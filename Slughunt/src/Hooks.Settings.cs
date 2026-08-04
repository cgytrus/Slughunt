namespace Slughunt;

public static partial class Hooks {
    // toggleable gameplay features
    public static class Settings {
        // ReSharper disable once MemberHidesStaticFromOuterClass
        public static void Apply() {
            ToggleCreatures();
            ShortcutLocks();
            GateLocks();
            CustomSpawn();
        }

        private static void ToggleCreatures() {
            On.WorldLoader.FindingCreatures += (orig, self) => {
                if (!lobbyData.spawnCreatures)
                    return;
                orig(self);
            };
        }

        private static void ShortcutLocks() {
            // what the fuck
            On.WorldLoader.ctor_RainWorldGame_Name_Timeline_bool_string_Region_SetupValues +=
            (
                orig, self, game, playerCharacter, timelinePosition, singleRoomWorld, worldName, region, setupValues
            ) => {
                orig(self, game, playerCharacter, timelinePosition, singleRoomWorld, worldName, region, setupValues);
                if (game is null)
                    return;
                foreach ((string a, string b) in lobbyData.lockedShortcuts) {
                    self.ConditionalLinkList.Add(new WorldLoader.ConditionalLink(a, b, "DISCONNECTED"));
                    self.ConditionalLinkList.Add(new WorldLoader.ConditionalLink(b, a, "DISCONNECTED"));
                }
            };
        }

        private static void GateLocks() {
            On.RegionGate.customKarmaGateRequirements += (orig, self) => {
                orig(self);
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
            On.WaterGate.WaterRunning += (_, _, _) => { };
            On.ElectricGate.BatteryRunning += (_, _, _) => { };
        }

        private static void CustomSpawn() {
            On.SaveState.setDenPosition += (orig, self) => {
                if (self.saveStateNumber == SlughuntGameMode.save) {
                    self.denPosition = lobbyData.startingShelter;
                }
                else {
                    orig(self);
                }
            };
        }
    }
}
