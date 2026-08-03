using System;
using BepInEx;
using BepInEx.Logging;
using HUD;
using MonoMod.RuntimeDetour;
using MoreSlugcats;
using RainMeadow;
using Slughunt.HUD;
using Slughunt.Menu;
using UnityEngine;

namespace Slughunt;

[BepInAutoPlugin("cwonfig.slughunt")]
[BepInDependency("henpemaz.rainmeadow")]
public partial class Plugin : BaseUnityPlugin {
    private static Plugin? _instance;
    private Plugin() => _instance = this;

    public static ManualLogSource logger => _instance!.Logger;

    private static Lobby lobby => OnlineManager.lobby;
    private static LobbyData lobbyData => lobby.GetData<LobbyData>();
    private static PlayerData playerData => lobbyData.GetPlayerData(OnlineManager.mePlayer);

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
        RoleColors();
        CustomHud();
        CustomSpawn();
        CatchRule();
        RespawnRule();
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
            if (SlughuntGameMode.IsIn() && !lobbyData.spawnCreatures)
                return;
            orig(self);
        };
    }

    private static void LockShortcuts() {
        On.WorldLoader.ctor_RainWorldGame_Name_Timeline_bool_string_Region_SetupValues +=
            (orig, self, game, playerCharacter, timelinePosition, singleRoomWorld, worldName, region, setupValues) => {
                orig(self, game, playerCharacter, timelinePosition, singleRoomWorld, worldName, region, setupValues);
                if (game is null || !SlughuntGameMode.IsIn())
                    return;
                foreach ((string a, string b) in lobbyData.lockedShortcuts) {
                    self.ConditionalLinkList.Add(new WorldLoader.ConditionalLink(a, b, "DISCONNECTED"));
                    self.ConditionalLinkList.Add(new WorldLoader.ConditionalLink(b, a, "DISCONNECTED"));
                }
            };
    }

    private static void UnlockGates() {
        On.RegionGate.customKarmaGateRequirements += (orig, self) => {
            orig(self);
            if (!SlughuntGameMode.IsIn())
                return;
            self.room.world.regionState.gatesPassedThrough[self.room.abstractRoom.gateIndex] = false;
            self.unlocked = !lobbyData.lockedGates.Contains(self.room.abstractRoom.name);
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

    private static void UnlockMap() {
        Texture2D? temp = null;
        _ = new Hook(
            typeof(Map).GetProperty(nameof(Map.discoverTexture))!.GetGetMethod(),
            (Func<Map, Texture2D> orig, Map self) => SlughuntGameMode.IsIn() ? temp : orig(self)
        );
        On.HUD.Map.CreateDiscoveryTextureFromVisitedRooms += (orig, self) => {
            if (!SlughuntGameMode.IsIn()) {
                orig(self);
                return;
            }
            int width = (int)(self.mapTexture.width / self.DiscoverResolution);
            int height = (int)(self.mapTexture.height / self.DiscoverResolution);
            if (temp is null)
                temp = new Texture2D(width, height);
            else
                temp.Resize(width, height);
            Color32[] pixels = temp.GetPixels32();
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = new Color32(255, 0, 0, 255);
            temp.SetPixels32(pixels);
            temp.Apply();
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

    private static void RoleColors() {
        On.PlayerGraphics.DrawSprites += (orig, self, sLeaser, rCam, timeStacker, camPos) => {
            if (
                !SlughuntGameMode.IsIn() ||
                !self.player.abstractPhysicalObject.GetOnlineObject(out OnlinePhysicalObject? opo) ||
                opo?.owner is null
            ) {
                orig(self, sLeaser, rCam, timeStacker, camPos);
                return;
            }
            PlayerData data = lobbyData.GetPlayerData(opo.owner);
            if (data.role is not Rules.Role.Hunter) {
                self.markAlpha = 0.0f;
                orig(self, sLeaser, rCam, timeStacker, camPos);
                return;
            }
            self.markAlpha = 1.0f;
            sLeaser.sprites[10].color = new HSLColor(12f / 360f, 1.0f, 0.55f).rgb;
            sLeaser.sprites[11].color = Color.Lerp(sLeaser.sprites[10].color, Color.white, 0.3f);
            orig(self, sLeaser, rCam, timeStacker, camPos);
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
            if (!SlughuntGameMode.IsIn())
                return;
            self.UpdateGraphic(
                playerData.role switch {
                    Rules.Role.Hunter => 0,
                    Rules.Role.Hider => 4,
                    _ => 9
                }, 9
            );
        };
        On.HUD.HUD.InitSinglePlayerHud += (orig, self, cam) => {
            orig(self, cam);
            if (!SlughuntGameMode.IsIn())
                return;
            self.AddPart(new SlughuntInfo(self, self.fContainers[0]));
        };
    }

    private static void CustomSpawn() {
        On.SaveState.setDenPosition += (orig, self) => {
            if (
                self.saveStateNumber != SlughuntGameMode.save ||
                !SlughuntGameMode.IsIn()
            ) {
                orig(self);
                return;
            }
            self.denPosition = lobbyData.startingShelter;
        };
    }

    private static void CatchRule() {
        On.Player.Collide += (orig, self, otherObject, myChunk, otherChunk) => {
            orig(self, otherObject, myChunk, otherChunk);
            if (!SlughuntGameMode.IsIn())
                return;
            OnCatch(self, otherObject);
            OnCatch(otherObject, self);
        };
        // TODO: maybe could make it so that its not only rocks?
        On.Rock.HitSomething += (orig, self, result, eu) => {
            if (!orig(self, result, eu))
                return false;
            if (!SlughuntGameMode.IsIn())
                return true;
            OnCatch(self.thrownBy, result.obj);
            return true;
        };
    }

    private static void OnCatch(PhysicalObject hunterObj, PhysicalObject hiderObj) {
        if (hunterObj is not Player hunter)
            return;
        if (hiderObj is not Player hider)
            return;

        if (!lobbyData.state.CanCatch(hunter, hider))
            return;

        Player? self = hunter.room.game.FirstRealizedPlayer;
        Rules.Role selfRole;
        OnlinePlayer? otherOnline;
        Rules.Role otherRole;
        Delegate rpc;

        if (self == hunter) {
            selfRole = Rules.Role.hunter;
            otherOnline = hider.abstractPhysicalObject.GetOnlineObject()?.owner;
            otherRole = Rules.Role.hider;
            rpc = RPC.OnCatchAsHunter;
        }
        else if (self == hider) {
            selfRole = Rules.Role.hider;
            otherOnline = hunter.abstractPhysicalObject.GetOnlineObject()?.owner;
            otherRole = Rules.Role.hunter;
            rpc = RPC.OnCatchAsHider;
        }
        else {
            return;
        }

        if (otherOnline is null)
            return;

        if (playerData.role != selfRole)
            return;
        if (lobbyData.GetPlayerData(otherOnline).role != otherRole)
            return;

        lobby.owner.InvokeOnceRPC(rpc, otherOnline);
    }

    private static void RespawnRule() {
        On.Player.Die += (orig, self) => {
            bool alreadyWasDead = self.dead;
            orig(self);
            if (self != self.room.game.FirstRealizedPlayer)
                return;
            if (alreadyWasDead)
                return;
            if (!SlughuntGameMode.IsIn())
                return;
            lobby.owner.InvokeRPC(RPC.OnDeath);
        };
        On.RainWorldGame.GoToDeathScreen += (orig, self) => {
            if (!SlughuntGameMode.IsIn()) {
                orig(self);
                return;
            }

            // TODO: update the game over text
            // TODO: spectate
            if (!playerData.role.canRespawn)
                return;

            Respawn(self, lobbyData.RandomShelter());
            self.lastPauseButton = true; // prevent pause

            lobby.owner.InvokeRPC(RPC.OnRespawn);
        };
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
