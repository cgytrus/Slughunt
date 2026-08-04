using System;
using BepInEx;
using BepInEx.Logging;
using Slughunt.Menu;

namespace Slughunt;

[BepInAutoPlugin("cwonfig.slughunt")]
[BepInDependency("henpemaz.rainmeadow")]
public partial class Plugin : BaseUnityPlugin {
    private static Plugin? _instance;
    private Plugin() => _instance = this;

    public static ManualLogSource logger => _instance!.Logger;

    private void Awake() {
        SlughuntGameMode.Register();

        On.ProcessManager.PostSwitchMainProcess += (orig, self, id) => {
            if (id == SlughuntMenu.id)
                self.currentMainLoop = new SlughuntMenu(self);
            orig(self, id);
        };

        Hooks.Apply();
    }

    // seems to work fine?
    public static void Respawn(RainWorldGame game, string roomName) {
        if (game.Players[0].realizedCreature is Player realizedPlayer) {
            realizedPlayer.AllGraspsLetGoOfThisObject(true);
            realizedPlayer.LoseAllGrasps();
            realizedPlayer.Destroy();
        }
        game.Players[0].Destroy();
        game.Players.RemoveAt(0);

        string regionName = roomName.Split('_')[0];
        if (!string.Equals(regionName, game.world.region.name, StringComparison.OrdinalIgnoreCase))
            LoadWorld(game, regionName);

        AbstractRoom? room = game.world.GetAbstractRoom(roomName);
        if (room is null) {
            logger.LogError($"tried to spawn in room {roomName} that doesnt exist in region {game.world.name}");
            return;
        }

        game.SpawnPlayers(true, false, false, false, new WorldCoordinate(room.index, 0, 0, -1));

        game.cameras[0].followAbstractCreature = game.Players[0];

        if (game.roomRealizer is not null && game.roomRealizer.world != game.world)
            game.roomRealizer = new RoomRealizer(game.cameras[0].followAbstractCreature, game.world);

        if (room.realizedRoom is null)
            room.RealizeRoom(game.world, game);
        else if (room.realizedRoom.readyForAI)
            game.Players[0].RealizeInRoom();

        foreach (RoomCamera camera in game.cameras) {
            camera.virtualMicrophone.AllQuiet();
            camera.MoveCamera(room.realizedRoom, -1);
            if (camera.hud is null)
                continue;
            camera.hud.ClearAllSprites();
            camera.hud = null;
        }

        game.playedGameOverSound = false; // allow the game over sound to play
        game.manager.fadeToBlack = 1.0f; // fade from black on respawn
    }

    private static void LoadWorld(RainWorldGame game, string regionName) {
        OverWorld overWorld = game.overWorld;

        World oldWorld = overWorld.activeWorld;
        overWorld.activeWorld = null;
        overWorld.LoadWorld(regionName, overWorld.PlayerCharacterNumber, overWorld.PlayerTimelinePosition, false);
        World newWorld = overWorld.activeWorld!;

        game.shortcuts.transportVessels.Clear();
        game.shortcuts.betweenRoomsWaitingLobby.Clear();
        game.shortcuts.borderTravelVessels.Clear();

        oldWorld.regionState?.AdaptRegionStateToWorld(-1, -1);
        oldWorld.regionState?.world = null;

        newWorld.rainCycle.baseCycleLength = oldWorld.rainCycle.baseCycleLength;
        newWorld.rainCycle.cycleLength = oldWorld.rainCycle.cycleLength;
        newWorld.rainCycle.timer = oldWorld.rainCycle.timer;
        newWorld.rainCycle.duskPalette = oldWorld.rainCycle.duskPalette;
        newWorld.rainCycle.nightPalette = oldWorld.rainCycle.nightPalette;
        newWorld.rainCycle.dayNightCounter = oldWorld.rainCycle.dayNightCounter;

        foreach (Room room in oldWorld.activeRooms)
            room.Unloaded();
    }
}
