using System;
using System.Collections.Generic;
using System.Linq;
using Expedition;
using MoreSlugcats;

namespace Slughunt;

public static class Blacklist {
    private static readonly HashSet<PlacedObject.Type> bannedPlacedObjects = [
        PlacedObject.Type.HangingPearls, // supposedly not synced
        PlacedObject.Type.StuckDaddy, // idk whats up with stuck daddies in rain meadow, ill just disable them for now
        PlacedObject.Type.KarmaFlower,
        PlacedObject.Type.VoidSpawnEgg,
        PlacedObject.Type.BlinkingFlower,
        MoreSlugcatsEnums.PlacedObjectType.MoonCloak,
        PlacedObject.Type.RippleSpawnEgg,
        Watcher.WatcherEnums.PlacedObjectType.CosmeticRipple
    ];
    public static bool HasPlacedObject(PlacedObject po) => bannedPlacedObjects.Contains(po.type) ||
        po.type.value.EndsWith("Token", StringComparison.Ordinal) ||
        po.type.value.EndsWith("Instruction", StringComparison.Ordinal);

    private static readonly HashSet<AbstractPhysicalObject.AbstractObjectType> unsyncedAbstractObjects = [
        AbstractPhysicalObject.AbstractObjectType.VoidSpawn,
        AbstractPhysicalObject.AbstractObjectType.BlinkingFlower,
        AbstractPhysicalObject.AbstractObjectType.AttachedBee,
        MoreSlugcatsEnums.AbstractObjectType.Bullet,
        Watcher.WatcherEnums.AbstractObjectType.RippleSpawn
    ];
    public static bool SyncAPO(AbstractPhysicalObject apo, bool room = false) =>
        !unsyncedAbstractObjects.Contains(apo.type) &&
        !room || apo.type != AbstractPhysicalObject.AbstractObjectType.SSOracleSwarmer;

    private static readonly HashSet<RoomSettings.RoomEffect.Type> bannedRoomEffects = [
        RoomSettings.RoomEffect.Type.VoidSea,
        RoomSettings.RoomEffect.Type.VoidSpawn
    ];
    public static void ApplyForRoomEffects() => On.RoomSettings.Load_Timeline += (orig, self, timelinePoint) => {
        if (!SlughuntGameMode.IsIn())
            return orig(self, timelinePoint);
        if (!orig(self, timelinePoint))
            return false;
        int count = self.effects.RemoveAll(effect => bannedRoomEffects.Contains(effect.type));
        if (count > 0)
            Plugin.logger.LogInfo($"removed {count} disallowed effects");
        return true;
    };

    private static bool HasRoomScript(UpdatableAndDeletable script) =>
        ExpeditionGame.IsUndesirableRoomScript(script) ||
        ExpeditionGame.IsMSCRoomScript(script) ||
        script is RoomSpecificScript.SB_D03ShortcutLock ||
        script is MSCRoomSpecificScript.GW_C05ArtificerMessage || // they probably forgot to remove this in expedition?
        script is MSCRoomSpecificScript.SU_A42Message ||
        script is MSCRoomSpecificScript.SpearmasterEnding ||
        script is MSCRoomSpecificScript.SL_AI_Behavior || // rivulet alt ending
        script is MSCRoomSpecificScript.SI_A07_RivEnding || // rivulet alt ending
        script is MSCRoomSpecificScript.RifleTutorial;
    public static void ApplyForScripts() => On.Room.Loaded += (orig, self) => {
        orig(self);
        if (!SlughuntGameMode.IsIn())
            return;
        foreach (UpdatableAndDeletable script in self.updateList.Where(HasRoomScript)) {
            string roomName = self.abstractRoom.name;
            string? scriptName = script.GetType().FullName;
            Plugin.logger.LogInfo($"blocking disallowed room specific script: {roomName} {scriptName}");
            script.Destroy();
        }
    };
}
