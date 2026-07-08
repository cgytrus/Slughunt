using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Menu;
using RainMeadow;

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

    public enum GameState : byte { Lobby, Game, Setup, Seek }
    public enum PlayerRole : byte { None, Hunter, Hider }

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
    }

    public LobbyData lobbyData => lobby.GetData<LobbyData>();
    public PlayerData playerData => clientSettings.GetData<PlayerData>();

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

    public override void AddClientData() {
        clientSettings.AddData(new PlayerData());
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
        if (!lobby.isOwner && !playerData.ready)
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
        CleanupOldAvatars();
        if (OnlineManager.instance.manager.currentMainLoop is not RainWorldGame game)
            return;
        // TODO
    }

    private void SetExpectedStateForProcess() {
        ProcessManager.ProcessID? process = OnlineManager.instance.manager.currentMainLoop?.ID;
        if (lobbyData.state != GameState.Lobby && process != ProcessManager.ProcessID.Game) {
            lobbyData.state = GameState.Lobby;
            lobby.NewVersion();
        }
        else if (lobbyData.state == GameState.Lobby && process == ProcessManager.ProcessID.Game) {
            lobbyData.state = GameState.Game;
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
    }

    public override void PostGameStart(RainWorldGame game) {
        base.PostGameStart(game);
    }

    public override void GameShutDown(RainWorldGame game) {
        base.GameShutDown(game);
    }
}
