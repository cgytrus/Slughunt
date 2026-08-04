using System.Linq;
using MoreSlugcats;

namespace Slughunt;

public static partial class Hooks {
    // room-specific gameplay changes
    public static class Room {
        // ReSharper disable once MemberHidesStaticFromOuterClass
        public static void Apply() {
            EffectBlacklist();
            ScriptBlacklist();
            LcFinalExpedition();
        }

        private static void EffectBlacklist() {
            On.RoomSettings.Load_Timeline += (orig, self, timelinePoint) => {
                if (!orig(self, timelinePoint))
                    return false;
                int count = self.effects.RemoveAll(Blacklist.HasRoomEffect);
                if (count > 0)
                    Plugin.logger.LogInfo($"removed {count} disallowed effects");
                return true;
            };
        }

        private static void ScriptBlacklist() {
            On.Room.Loaded += (orig, self) => {
                orig(self);
                foreach (UpdatableAndDeletable script in self.updateList.Where(Blacklist.HasRoomScript)) {
                    string roomName = self.abstractRoom.name;
                    string? scriptName = script.GetType().FullName;
                    Plugin.logger.LogInfo($"blocking disallowed room specific script: {roomName} {scriptName}");
                    script.Destroy();
                }
            };
        }

        private static void LcFinalExpedition() {
            // LC_FINAL script gets replaced with LC_FINAL_Expedition, like in expedition
            On.MoreSlugcats.MSCRoomSpecificScript.AddRoomSpecificScript += (orig, room) => {
                orig(room);
                if (room.abstractRoom.name == "LC_FINAL" && room.abstractRoom.firstTimeRealized)
                    room.AddObject(new MSCRoomSpecificScript.LC_FINAL_Expedition(room));
            };
        }
    }
}
