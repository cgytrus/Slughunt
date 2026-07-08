using System;
using BepInEx;
using BepInEx.Logging;
using HUD;
using MonoMod.RuntimeDetour;
using MoreSlugcats;
using UnityEngine;

namespace Slughunt;

[BepInAutoPlugin("cwonfig.slughunt")]
[BepInDependency("henpemaz.rainmeadow")]
public partial class Plugin : BaseUnityPlugin {
    private static Plugin? _instance;

    public static ManualLogSource logger => _instance!.Logger;

    private Plugin() => _instance = this;

    private void Awake() {
        SlughuntGameMode.Register();

        On.ProcessManager.PostSwitchMainProcess += (orig, self, id) => {
            if (id == SlughuntMenu.id)
                self.currentMainLoop = new SlughuntMenu(self);
            orig(self, id);
        };

        Blacklist.ApplyForRoomEffects();
        Blacklist.ApplyForScripts();

        // LC_FINAL script gets replaced with LC_FINAL_Expedition, like in expedition
        On.MoreSlugcats.MSCRoomSpecificScript.AddRoomSpecificScript += (orig, room) => {
            orig(room);
            if (!SlughuntGameMode.IsIn())
                return;
            if (room.abstractRoom.name == "LC_FINAL" && room.abstractRoom.firstTimeRealized)
                room.AddObject(new MSCRoomSpecificScript.LC_FINAL_Expedition(room));
        };

        DisableOverseers();
        DisableGhosts();
        DisableRain();
        DisableShelters();
        DisableEating();
        DisableSleep();
        DisablePups();
        DisableOracles();
        DisableCreatures();
        LockShortcuts();
        UnlockGates();
        UnlockMap();
        CustomHud();
        CustomSpawn();
        CustomRespawn();
    }

    private static void DisableOverseers() {
        On.WorldLoader.OverseerSpawnConditions += (orig, self, character) =>
            !SlughuntGameMode.IsIn() && orig(self, character);
    }

    private static void DisableGhosts() {
        On.World.SpawnGhost += (orig, self) => {
            if (SlughuntGameMode.IsIn())
                return;
            orig(self);
        };
    }

    private static void DisableRain() {
        On.OverWorld.LoadFirstWorld += (orig, self) => {
            orig(self);
            if (SlughuntGameMode.IsIn())
                self.activeWorld.rainCycle.timer = 800;
        };
        On.RainWorldGame.AllowRainCounterToTick += (orig, self) => !SlughuntGameMode.IsIn() && orig(self);
    }

    private static void DisableShelters() {
        On.ShelterDoor.Close += (orig, self) => {
            if (SlughuntGameMode.IsIn())
                return;
            orig(self);
        };
    }

    // ? not sure whether i wanna keep this actually
    // upd: yes i do
    private static void DisableEating() {
        On.Player.CanEatMeat += (orig, self, crit) => !SlughuntGameMode.IsIn() && orig(self, crit);
        On.Player.BiteEdibleObject += (orig, self, eu) => {
            if (SlughuntGameMode.IsIn())
                return;
            orig(self, eu);
        };
        On.Player.AddFood += (orig, self, add) => {
            if (SlughuntGameMode.IsIn())
                return;
            orig(self, add);
        };
        On.Player.AddQuarterFood += (orig, self) => {
            if (SlughuntGameMode.IsIn())
                return;
            orig(self);
        };
        On.Player.SubtractFood += (orig, self, sub) => {
            if (SlughuntGameMode.IsIn())
                return;
            orig(self, sub);
        };
    }

    private static void DisableSleep() {
        On.Player.SleepUpdate += (orig, self) => {
            if (SlughuntGameMode.IsIn()) {
                self.sleepCounter = 0;
                self.forceSleepCounter = 0;
            }
            orig(self);
        };
    }

    private static void DisablePups() {
        On.World.SpawnPupNPCs += (orig, self) => SlughuntGameMode.IsIn() ? 0 : orig(self);
    }

    // TODO: maybe update iterators behavior so they properly react to players in slughunt
    private static void DisableOracles() {
        On.Room.AddObject += (orig, self, obj) => {
            if (SlughuntGameMode.IsIn() && obj is Oracle) {
                logger.LogInfo("blocking oracle");
                return;
            }
            orig(self, obj);
        };
    }

    private static void DisableCreatures() {
        On.WorldLoader.FindingCreatures += (orig, self) => {
            if (SlughuntGameMode.TryGet(out SlughuntGameMode? gameMode) && !gameMode.lobbyData.spawnCreatures)
                return;
            orig(self);
        };
    }

    private static void LockShortcuts() {
        On.WorldLoader.ctor_RainWorldGame_Name_Timeline_bool_string_Region_SetupValues +=
            (orig, self, game, playerCharacter, timelinePosition, singleRoomWorld, worldName, region, setupValues) => {
                orig(self, game, playerCharacter, timelinePosition, singleRoomWorld, worldName, region, setupValues);
                if (game is null || !SlughuntGameMode.TryGet(out SlughuntGameMode? gameMode))
                    return;
                foreach ((string a, string b) in gameMode.lobbyData.lockedShortcuts) {
                    self.ConditionalLinkList.Add(new WorldLoader.ConditionalLink(a, b, "DISCONNECTED"));
                    self.ConditionalLinkList.Add(new WorldLoader.ConditionalLink(b, a, "DISCONNECTED"));
                }
            };
    }

    private static void UnlockGates() {
        On.RegionGate.customKarmaGateRequirements += (orig, self) => {
            orig(self);
            if (!SlughuntGameMode.TryGet(out SlughuntGameMode? gameMode))
                return;
            self.room.world.regionState.gatesPassedThrough[self.room.abstractRoom.gateIndex] = false;
            self.unlocked = !gameMode.lobbyData.lockedGates.Contains(self.room.abstractRoom.name);
            self.karmaRequirements[0] = self.unlocked ?
                RegionGate.GateRequirement.OneKarma :
                RegionGate.GateRequirement.DemoLock;
            self.karmaRequirements[1] = self.karmaRequirements[0];
        };
        On.RegionGate.Update += (orig, self, eu) => {
            orig(self, eu);
            if (self.mode == RegionGate.Mode.Closed)
                self.Reset();
        };
        On.WaterGate.WaterRunning += (orig, self, flow) => {
            if (SlughuntGameMode.IsIn())
                return;
            orig(self, flow);
        };
        On.ElectricGate.BatteryRunning += (orig, self, flow) => {
            if (SlughuntGameMode.IsIn())
                return;
            orig(self, flow);
        };
    }

    private static Texture2D? _tempDiscoverTexture;
    private static void UnlockMap() {
        _ = new Hook(
            typeof(Map).GetProperty(nameof(Map.discoverTexture))!.GetGetMethod(),
            (Func<Map, Texture2D> orig, Map self) => SlughuntGameMode.IsIn() ? _tempDiscoverTexture : orig(self)
        );
        On.HUD.Map.CreateDiscoveryTextureFromVisitedRooms += (orig, self) => {
            if (!SlughuntGameMode.IsIn()) {
                orig(self);
                return;
            }
            int width = (int)(self.mapTexture.width / self.DiscoverResolution);
            int height = (int)(self.mapTexture.height / self.DiscoverResolution);
            if (_tempDiscoverTexture is null)
                _tempDiscoverTexture = new Texture2D(width, height);
            else
                _tempDiscoverTexture.Resize(width, height);
            Color32[] pixels = _tempDiscoverTexture.GetPixels32();
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = new Color32(255, 0, 0, 255);
            _tempDiscoverTexture.SetPixels32(pixels);
            _tempDiscoverTexture.Apply();
        };
        On.HUD.Map.DiscoverMap += (orig, self, texturePos) => {
            if (SlughuntGameMode.IsIn())
                return;
            orig(self, texturePos);
        };
        On.HUD.Map.OnePixelDiscoverMap += (orig, self, texturePos) => {
            if (SlughuntGameMode.IsIn())
                return;
            orig(self, texturePos);
        };
        On.HUD.Map.SmallDiscoverMap += (orig, self, texturePos) => {
            if (SlughuntGameMode.IsIn())
                return;
            orig(self, texturePos);
        };
        On.PlayerProgression.TempDiscoverShelter += (orig, self, shelterName) => {
            if (SlughuntGameMode.IsIn())
                return;
            orig(self, shelterName);
        };
    }

    private static void CustomHud() {
        On.HUD.FoodMeter.Draw += (orig, self, timeStacker) => {
            if (SlughuntGameMode.IsIn())
                return;
            orig(self, timeStacker);
        };
        On.HUD.KarmaMeter.Update += (orig, self) => {
            orig(self);
            if (!SlughuntGameMode.TryGet(out SlughuntGameMode? gameMode))
                return;
            if (!gameMode.clientSettings.TryGetData(out PlayerData playerData))
                return;
            self.UpdateGraphic(playerData.role switch {
                SlughuntGameMode.PlayerRole.Hunter => 0,
                SlughuntGameMode.PlayerRole.Hider => 4,
                _ => 9
            }, 9);
        };
    }

    private static void CustomSpawn() {
        On.SaveState.setDenPosition += (orig, self) => {
            if (self.saveStateNumber != SlughuntGameMode.save ||
                !SlughuntGameMode.TryGet(out SlughuntGameMode? gameMode)) {
                orig(self);
                return;
            }
            self.denPosition = gameMode.lobbyData.startingShelter;
        };
    }

    // seems to work fine?
    private static void CustomRespawn() {
        On.RainWorldGame.GoToDeathScreen += (orig, self) => {
            if (!SlughuntGameMode.TryGet(out SlughuntGameMode? gameMode)) {
                orig(self);
                return;
            }

            // fade from black on respawn
            // TODO: maybe figure out fade to black too
            self.manager.fadeToBlack = 1.0f;

            AbstractCreature player = self.Players[0];

            if (player.realizedCreature is Player realizedPlayer) {
                realizedPlayer.AllGraspsLetGoOfThisObject(true);
                realizedPlayer.LoseAllGrasps();
                realizedPlayer.Destroy();
            }
            player.Destroy();

            string shelter = gameMode.lobbyData.RandomShelter().ToUpperInvariant();

            string shelterRegion = shelter.Split('_')[0];
            if (shelterRegion != self.world.region.name.ToUpperInvariant()) {
                LoadWorld(self, shelterRegion);
            }

            SpawnRoom(self, shelter);

            // exit game over mode
            self.cameras[0].hud.textPrompt.gameOverMode = false;

            // prevent pause
            self.lastPauseButton = true;
        };
    }

    private static void SpawnRoom(RainWorldGame game, string roomName) {
        AbstractRoom? room = game.world.GetAbstractRoom(roomName);
        if (room is null)
            return;

        game.SpawnPlayers(true, false, false, false, new WorldCoordinate(room.index, 0, 0, -1));

        // the game assumes players[0] is the main/only player in a loooot of places
        game.Players[0] = game.Players[game.Players.Count - 1];
        game.Players.RemoveAt(game.Players.Count - 1);
        AbstractCreature player = game.Players[0];

        game.cameras[0].followAbstractCreature = player;

        if (game.roomRealizer is not null && game.roomRealizer.world != game.world) {
            game.roomRealizer = new RoomRealizer(game.cameras[0].followAbstractCreature, game.world);
        }

        if (room.realizedRoom is null)
            room.RealizeRoom(game.world, game);
        else if (room.realizedRoom.readyForAI)
            player.RealizeInRoom();

        foreach (RoomCamera camera in game.cameras) {
            camera.virtualMicrophone.AllQuiet();
            camera.MoveCamera(room.realizedRoom, -1);
        }
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
        //for (int i = game.shortcuts.transportVessels.Count - 1; i >= 0; i--) {
        //    if (!newWorld.region.IsRoomInRegion(game.shortcuts.transportVessels[i].room.index))
        //        game.shortcuts.transportVessels.RemoveAt(i);
        //}
        //for (int i = game.shortcuts.betweenRoomsWaitingLobby.Count - 1; i >= 0; i--) {
        //    if (!newWorld.region.IsRoomInRegion(game.shortcuts.betweenRoomsWaitingLobby[i].room.index))
        //        game.shortcuts.betweenRoomsWaitingLobby.RemoveAt(i);
        //}
        //for (int i = game.shortcuts.borderTravelVessels.Count - 1; i >= 0; i--) {
        //    if (!newWorld.region.IsRoomInRegion(game.shortcuts.borderTravelVessels[i].room.index))
        //        game.shortcuts.borderTravelVessels.RemoveAt(i);
        //}

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
