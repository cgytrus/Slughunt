using System;
using HUD;
using MonoMod.RuntimeDetour;
using UnityEngine;

namespace Slughunt;

public static partial class Hooks {
    // permanent gameplay changes
    public static class Permanent {
        // ReSharper disable once MemberHidesStaticFromOuterClass
        public static void Apply() {
            DisableOverseers();
            DisableGhosts();
            DisableRain();
            DisableShelters();
            DisableEating();
            DisableSleep();
            DisablePups();
            DisableOracles();
            UnlockMap();
        }

        private static void DisableOverseers() {
            using DetourContext ctx = new(PriorityFirst);
            On.WorldLoader.OverseerSpawnConditions += (_, _, _) => false;
        }

        private static void DisableGhosts() {
            using DetourContext ctx = new(PriorityFirst);
            On.World.SpawnGhost += (_, _) => { };
        }

        private static void DisableRain() {
            On.OverWorld.LoadFirstWorld += (orig, self) => {
                orig(self);
                self.activeWorld.rainCycle.timer = 800;
            };

            using DetourContext ctx = new(PriorityFirst);
            On.RainWorldGame.AllowRainCounterToTick += (_, _) => false;
        }

        private static void DisableShelters() {
            using DetourContext ctx = new(PriorityFirst);
            On.ShelterDoor.Close += (_, _) => { };
        }

        private static void DisableEating() {
            using DetourContext ctx = new(PriorityFirst);
            On.Player.CanEatMeat += (_, _, _) => false;
            On.Player.BiteEdibleObject += (_, _, _) => { };
            On.Player.AddFood += (_, _, _) => { };
            On.Player.AddQuarterFood += (_, _) => { };
            On.Player.SubtractFood += (_, _, _) => { };
        }

        private static void DisableSleep() {
            On.Player.SleepUpdate += (orig, self) => {
                self.sleepCounter = 0;
                self.forceSleepCounter = 0;
                orig(self);
            };
        }

        private static void DisablePups() {
            using DetourContext ctx = new(PriorityFirst);
            On.World.SpawnPupNPCs += (_, _) => 0;
        }

        // TODO: maybe update iterators behavior so they properly react to players in slughunt
        private static void DisableOracles() {
            using DetourContext ctx = new(PriorityFirst);
            On.Room.AddObject += (orig, self, obj) => {
                if (obj is Oracle) {
                    Plugin.logger.LogInfo("blocking oracle");
                    return;
                }
                orig(self, obj);
            };
        }

        private static void UnlockMap() {
            using DetourContext ctx = new(PriorityFirst);
            Texture2D? temp = null;
            _ = new Hook(
                typeof(Map).GetProperty(nameof(Map.discoverTexture))!.GetGetMethod(),
                (Func<Map, Texture2D> _, Map _) => temp
            );
            On.HUD.Map.CreateDiscoveryTextureFromVisitedRooms += (_, self) => {
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
            On.HUD.Map.DiscoverMap += (_, _, _) => { };
            On.HUD.Map.OnePixelDiscoverMap += (_, _, _) => { };
            On.HUD.Map.SmallDiscoverMap += (_, _, _) => { };
            On.PlayerProgression.TempDiscoverShelter += (_, _, _) => { };
        }
    }
}
