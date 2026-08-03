using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using RainMeadow;
using Slughunt.Menu;

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

    public override void PlayerLeftLobby(OnlinePlayer player) {
        lobbyData.RemovePlayerData(player);
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
        abstractCreature.Room.AddEntity(abstractCreature);
        game.session.AddPlayer(abstractCreature);
        return abstractCreature;
    }

    public override void ConfigureAvatar(OnlineCreature onlineCreature) {
        onlineCreature.AddData(avatarSettings);
        avatarSettings.playerIndex = 0;
        avatarSettings.playingAs = character;
        avatarSettings.nickname = OnlineManager.mePlayer.id.name;
        avatarSettings.fakePup = false;
        avatarSettings.overlaySkin = AvatarData.ConfigureOverlay(onlineCreature);
    }

    public override void Customize(Creature creature, OnlineCreature onlineCreature) {
        if (!onlineCreature.TryGetData(out SlugcatCustomization data))
            return;
        RainMeadow.RainMeadow.creatureCustomizations.GetValue(creature, _ => data);
    }

    public void NextStateIfReady() {
        if (!lobbyData.state.readyForNext)
            return;
        lobbyData.state = lobbyData.state.next;
        lobby.NewVersion();
    }

    private void EnterGame() {
        if (!lobbyData.state.canEnterGame)
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
        if (!TrySwitchToExpectedProcess())
            return;
        CleanupOldAvatars();
        if (lobbyData.state is not Rules.GameState.InGame state)
            return;
        state.Tick();
        NextStateIfReady();
    }

    private bool TrySwitchToExpectedProcess() {
        ProcessManager manager = OnlineManager.instance.manager;
        if (manager.currentMainLoop is null)
            return false;
        if (lobbyData.state is Rules.GameState.InGame == (manager.currentMainLoop.ID == ProcessManager.ProcessID.Game))
            return true;
        if (manager.IsSwitchingProcesses() || manager.IsRunningAnyDialog || manager._processSwitchQueue.Count > 0)
            return false;
        if (lobbyData.state is Rules.GameState.InLobby) {
            manager.RequestMainProcessSwitch(SlughuntMenu.id);
        }
        else {
            EnterGame();
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

    public override void GameShutDown(RainWorldGame game) {
        base.GameShutDown(game);
        if (lobby.isOwner) {
            lobbyData.state = Rules.GameState.inLobby;
            lobby.NewVersion();
        }
        else {
            // WantExit is specifically for when a player wants to exit while in-game
            if (lobbyData.state is not Rules.GameState.InGame)
                return;
            lobby.owner.InvokeOnceRPC(HostWantExit);
        }
    }

    [RPCMethod]
    private static void HostWantExit(RPCEvent rpcEvent) {
        LobbyData lobbyData = OnlineManager.lobby.GetData<LobbyData>();
        lobbyData.state.Leave(lobbyData.GetPlayerData(rpcEvent.from));
        OnlineManager.lobby.NewVersion();
    }
}
