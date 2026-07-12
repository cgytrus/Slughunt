using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using Menu;
using RainMeadow;
using UnityEngine;

namespace Slughunt;

public class SlughuntGameMode(Lobby lobby) : OnlineGameMode(lobby) {
    public static OnlineGameModeType type { get; } = new("Slughunt", true);
    public static SlugcatStats.Name save { get; } = new("Slughunt", true);

    public static void Register() {
        // why do i have to be translated so i have to do this super late but rain meadow itself doesnt
        // unfair......
        gamemodes[type] = typeof(SlughuntGameMode);
        OnlineGameModeType.descriptions[type] = "slughunt";
        //RegisterType(type, typeof(SlughuntGameMode), "slughunt");
    }

    // ReSharper disable once ConditionalAccessQualifierIsNonNullableAccordingToAPIContract
    public static bool TryGet([NotNullWhen(true)] out SlughuntGameMode? gameMode) {
        if (OnlineManager.lobby?.gameMode is not SlughuntGameMode self) {
            gameMode = null;
            return false;
        }
        gameMode = self;
        return true;
    }

    // ReSharper disable once ConditionalAccessQualifierIsNonNullableAccordingToAPIContract
    public static bool IsIn() => OnlineManager.lobby?.gameMode is SlughuntGameMode;

    public override ProcessManager.ProcessID MenuProcessId() => SlughuntMenu.id;
    public override PauseMenu CustomPauseMenu(ProcessManager manager, RainWorldGame game) => base.CustomPauseMenu(manager, game); // TODO?

    public override bool AllowedInMode(PlacedObject item) => !Blacklist.HasPlacedObject(item);

    public override bool ShouldLoadCreatures(RainWorldGame game, WorldSession ws) =>
        lobbyData.spawnCreatures && ShouldLoadObjects(ws);

    public override bool ShouldSpawnRoomItems(RainWorldGame game, RoomSession rs) => ShouldLoadObjects(rs);

    private static bool ShouldLoadObjects(OnlineResource resource) =>
        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        !OnlineManager.mePlayer.isActuallySpectating && (resource.owner is null || resource.isOwner);

    public override bool ShouldSyncAPOInWorld(WorldSession _, AbstractPhysicalObject apo) => Blacklist.SyncAPO(apo);
    public override bool ShouldSyncAPOInRoom(RoomSession _, AbstractPhysicalObject apo) => Blacklist.SyncAPO(apo, true);
    public override bool ShouldRegisterAPO(OnlineResource _, AbstractPhysicalObject apo) => Blacklist.SyncAPO(apo);
    public override bool ShouldSpawnFly(FliesWorldAI flies, int spawnRoom) => lobbyData.spawnCreatures;

    public override void NewResourceOwner(OnlineResource resource, OnlinePlayer? oldOwner, OnlinePlayer? newOwner) {
        if (resource is not Lobby)
            return;
        if (OnlineManager.instance.manager.currentMainLoop is not RainWorldGame)
            return;
        OnlineManager.instance.manager.RequestMainProcessSwitch(SlughuntMenu.id);
        if (!lobby.isOwner)
            return;
        lobbyData.state = GameState.Lobby;
        lobby.NewVersion();
    }

    public LobbyData lobbyData => lobby.GetData<LobbyData>();
    public PlayerData playerData => lobbyData.GetPlayerData(OnlineManager.mePlayer);

    public SlugcatStats.Name character => lobbyData.campaign;
    public SlugcatStats.Timeline timeline => SlugcatStats.SlugcatToTimeline(lobbyData.campaign);

    public SlugcatCustomization avatarSettings { get; } = new();

    public override SlugcatStats.Name GetStorySessionPlayer(RainWorldGame game) => save;
    public override SlugcatStats.Name LoadWorldAs(RainWorldGame game) => character;
    public override SlugcatStats.Timeline LoadWorldIn(RainWorldGame game) => timeline;

    public override void ResourceAvailable(OnlineResource resource) {
        base.ResourceAvailable(resource);
        if (resource is Lobby lob) {
            lob.AddData(new LobbyData());
        }
    }

    public override AbstractCreature SpawnAvatar(RainWorldGame game, WorldCoordinate location) {
        AbstractCreature abstractCreature = new(game.world, StaticWorld.GetCreatureTemplate("Slugcat"), null, location, new EntityID(-1, 0));
        abstractCreature.state = new PlayerState(abstractCreature, 0, character, false);
        game.world.GetAbstractRoom(abstractCreature.pos.room).AddEntity(abstractCreature);
        game.session.AddPlayer(abstractCreature);
        return abstractCreature;
    }

    public override void ConfigureAvatar(OnlineCreature onlineCreature) {
        onlineCreature.AddData(avatarSettings);
        avatarSettings.playerIndex = 0;
        avatarSettings.playingAs = character;
        avatarSettings.nickname = OnlineManager.mePlayer.id.name;
        avatarSettings.wearingCape = false; // TODO
        avatarSettings.eventCape = null;
        avatarSettings.fakePup = false;
        avatarSettings.overlaySkin = AvatarData.ConfigureOverlay(onlineCreature);
    }

    public override void Customize(Creature creature, OnlineCreature onlineCreature) {
        if (!onlineCreature.TryGetData(out SlugcatCustomization data))
            return;
        RainMeadow.RainMeadow.creatureCustomizations.GetValue(creature, _ => data);
    }

    public void StartGame() {
        // TODO: count ready players
        if (!playerData.ready ||
            !lobby.isOwner && lobbyData.state == GameState.Lobby ||
            OnlineManager.players.Count < 2)
            return;
        ProcessManager manager = OnlineManager.instance.manager;
        if (ModManager.CoopAvailable)
            manager.rainWorld.DeactivateAllPlayers();
        manager.arenaSitting = null;
        manager.rainWorld.progression.ClearOutSaveStateFromMemory();
        manager.rainWorld.progression.miscProgressionData.currentlySelectedSinglePlayerSlugcat = save;
        manager.rainWorld.progression.WipeSaveState(save);
        manager.menuSetup.startGameCondition = ProcessManager.MenuSetup.StoryGameInitCondition.New;
        manager.RequestMainProcessSwitch(ProcessManager.ProcessID.Game);
    }

    public override void LobbyTick(uint tick) {
        base.LobbyTick(tick);
        if (lobby.isOwner) {
            SetExpectedStateForProcess();
        }
        else if (!TrySwitchToExpectedProcess()) {
            return;
        }
        lobbyData.CleanupOldPlayerData();
        CleanupOldAvatars();
        if (OnlineManager.instance.manager.currentMainLoop is RainWorldGame game)
            GameTick(game);
    }

    private void SetExpectedStateForProcess() {
        ProcessManager.ProcessID? process = OnlineManager.instance.manager.currentMainLoop?.ID;
        if (lobbyData.state != GameState.Lobby && process != ProcessManager.ProcessID.Game) {
            lobbyData.state = GameState.Lobby;
            lobby.NewVersion();
        }
        else if (lobbyData.state == GameState.Lobby && process == ProcessManager.ProcessID.Game) {
            lobbyData.state = GameState.Setup;
            lobby.NewVersion();
        }
        // once we are in game we control the state based on the gameplay in LobbyTick
    }

    private bool TrySwitchToExpectedProcess() {
        ProcessManager manager = OnlineManager.instance.manager;
        if (manager.currentMainLoop is null)
            return false;
        if (lobbyData.state == GameState.Lobby && manager.currentMainLoop.ID != ProcessManager.ProcessID.Game)
            return true;
        if (lobbyData.state != GameState.Lobby && manager.currentMainLoop.ID == ProcessManager.ProcessID.Game)
            return true;
        if (manager.IsSwitchingProcesses() || manager.IsRunningAnyDialog || manager._processSwitchQueue.Count > 0)
            return false;
        if (lobbyData.state == GameState.Lobby) {
            manager.RequestMainProcessSwitch(SlughuntMenu.id);
        }
        else {
            StartGame();
        }
        return false;
    }

    private void CleanupOldAvatars() {
        ConditionalWeakTable<AbstractPhysicalObject, OnlinePhysicalObject> opoMap = OnlinePhysicalObject.map;
        for (int i = avatars.Count - 1; i >= 0; i--) {
            if (opoMap.TryGetValue(avatars[i].abstractCreature, out OnlinePhysicalObject? opo) && avatars[i] == opo)
                continue;
            Plugin.logger.LogInfo($"cleaning up avatar {i} {avatars[i]}");
            avatars.RemoveAt(i);
        }
    }

    public override void PreGameStart() {
        base.PreGameStart();
        PrepareRound();
    }

    public override void PostGameStart(RainWorldGame game) {
        base.PostGameStart(game);
    }

    private void PrepareRound() {
        if (!lobby.isOwner)
            return;
        lobbyData.startingShelter = lobbyData.RandomShelter();
        AssignRoles();
        lobbyData.state = GameState.Setup;
        lobby.NewVersion();
    }

    private PlayerRole PickRole(PlayerRole role, int currentHunters) {
        int maxHunters = lobby.clientSettings.Count - 1;
        if (currentHunters >= maxHunters)
            return PlayerRole.Hider;
        if (currentHunters < 1)
            return PlayerRole.Hunter;
        if (role == PlayerRole.PreferHunter && !lobbyData.allowHunterPreference)
            role = PlayerRole.None;
        if (role == PlayerRole.PreferHider && !lobbyData.allowHiderPreference)
            role = PlayerRole.None;
        if (lobbyData.targetHunterCount == 0) {
            return role == PlayerRole.PreferHunter ? PlayerRole.Hunter :
                role == PlayerRole.PreferHider ? PlayerRole.Hider :
                RXRandom.Bool() ? PlayerRole.Hunter : PlayerRole.Hider;
        }
        int targetHunters = Mathf.Clamp(lobbyData.targetHunterCount, 1, maxHunters);
        return currentHunters < targetHunters ? PlayerRole.Hunter : PlayerRole.Hider;
    }

    private void AssignRoles() {
        List<PlayerData> players = OnlineManager.players
            .Select(x => lobbyData.GetPlayerData(x))
            .OrderBy(_ => RXRandom.Int())
            .ToList();
        int hunterCount = 0;
        foreach (PlayerData data in players.Where(x => x.role == PlayerRole.PreferHunter)) {
            data.role = PickRole(data.role, hunterCount);
            if (data.role == PlayerRole.Hunter)
                hunterCount++;
        }
        foreach (PlayerData data in players.Where(x => x.role == PlayerRole.None)) {
            data.role = PickRole(data.role, hunterCount);
            if (data.role == PlayerRole.Hunter)
                hunterCount++;
        }
        foreach (PlayerData data in players.Where(x => x.role == PlayerRole.PreferHider)) {
            data.role = PickRole(data.role, hunterCount);
            if (data.role == PlayerRole.Hunter)
                hunterCount++;
        }
    }

    public void AssignLateRole(PlayerData data) {
        int hunterCount = OnlineManager.players.Count(x => lobbyData.GetPlayerData(x).role == PlayerRole.Hunter);
        data.role = PickRole(data.role, hunterCount);
    }

    private void GameTick(RainWorldGame game) {
        switch (lobbyData.state) {
            case GameState.Setup:
                SetupTick(game);
                break;
            case GameState.Hide:
                HideTick(game);
                break;
            case GameState.Hunt:
                HuntTick(game);
                break;
            case GameState.Lobby:
            default:
                Plugin.logger.LogError($"invalid game state {lobbyData.state}");
                break;
        }
    }

    private void SetupTick(RainWorldGame game) {
        // TODO: this is still counting hunter/hider time
        StunOrManageShortcut(game, false);
        if (!lobby.isOwner)
            return;
        bool allHidersInShortcuts = true;
        foreach (OnlinePlayer player in OnlineManager.players) {
            PlayerData data = lobbyData.GetPlayerData(player);
            if (!data.ready)
                continue;
            if (data.role != PlayerRole.Hider)
                continue;
            allHidersInShortcuts = allHidersInShortcuts &&
                lobby.clientSettings.TryGetValue(player, out ClientSettings settings) &&
                settings.inGame &&
                settings.avatars.All(x => x.FindEntity(true) is OnlineCreature {
                    realized: true, realizedCreature.inShortcut: true
                });
        }
        if (!allHidersInShortcuts)
            return;
        lobbyData.state = GameState.Hide;
        lobby.NewVersion();
    }

    private void HideTick(RainWorldGame game) {
        StunOrManageShortcut(game, true);
        if (!lobby.isOwner)
            return;
        if (lobby.owner.tick - lobbyData.switchedStateAt < (long)lobbyData.hideTimeFrames)
            return;
        lobbyData.state = GameState.Hunt;
        lobby.NewVersion();
    }

    private void StunOrManageShortcut(RainWorldGame game, bool hide) {
        Player? player = game.FirstRealizedPlayer;
        if (player is null)
            return;
        switch (playerData.role) {
            case PlayerRole.Hunter:
                if (!hide)
                    player.stun = (int)(40 * lobbyData.hideTime.TotalSeconds);
                break;
            case PlayerRole.Hider when player.inShortcut:
                player.inShortcutVessel?.wait = hide ? 0 : 2;
                break;
            case PlayerRole.None:
            case PlayerRole.PreferHunter:
            default:
                break;
        }
    }

    private void HuntTick(RainWorldGame game) {
        if (!lobby.isOwner)
            return;
        bool anyHunters = false;
        bool anyHiders = false;
        foreach (ClientSettings settings in lobby.clientSettings.Values) {
            PlayerData data = lobbyData.GetPlayerData(settings.owner);
            anyHunters = anyHunters || data.role == PlayerRole.Hunter && (lobbyData.endless || settings.avatars
                .Any(x => x.FindEntity(true) is OnlineCreature { abstractCreature.state.alive: true }));
            anyHiders = anyHiders || data.role == PlayerRole.Hider && (lobbyData.endless || settings.avatars
                .Any(x => x.FindEntity(true) is OnlineCreature { abstractCreature.state.alive: true }));
            if (anyHiders && anyHunters)
                break;
        }
        if (!anyHunters || !anyHiders)
            EndRound(game);
    }

    private void EndRound(RainWorldGame game) {
        if (!lobbyData.endless) {
            OnlineManager.instance.manager.RequestMainProcessSwitch(SlughuntMenu.id);
            return;
        }
        PrepareRound();
        Plugin.Respawn(game, lobbyData.startingShelter);
    }

    public override void GameShutDown(RainWorldGame game) {
        base.GameShutDown(game);
        if (!lobby.isOwner)
            return;
        foreach (PlayerData data in OnlineManager.players.Select(x => lobbyData.GetPlayerData(x))) {
            data.ready = false;
            data.role = PlayerRole.None;
            data.dead = false;
        }
        lobby.NewVersion();
    }
}
