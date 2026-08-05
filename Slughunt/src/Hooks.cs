using System.Reflection;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using RainMeadow;

namespace Slughunt;

public static partial class Hooks {
    // wanna use first when not calling and last when reimplementing
    public const int PriorityFirst = -1000000;
    public const int PriorityLast = 1000000;

    private static Lobby lobby => OnlineManager.lobby;
    private static LobbyData lobbyData => lobby.GetData<LobbyData>();
    private static PlayerData playerData => lobbyData.GetPlayerData(OnlineManager.mePlayer);

    public static void Apply() {
        try {
            Hook.OnDetour += AutoSlughuntCheck;

            Room.Apply();
            Permanent.Apply();
            Settings.Apply();
            Display.Apply();
            Rules.Apply();
        }
        finally {
            Hook.OnDetour -= AutoSlughuntCheck;
        }
    }

    // automatically add
    // if (OnlineManager.lobby?.gameMode is not SlughuntGameMode) {
    //     orig(...);
    //     return;
    // }
    // to every hook!
    private static bool AutoSlughuntCheck(Hook hook, MethodBase method, MethodBase target, object delegateTarget) {
        _ = new ILHook(target, il => {
            ILCursor cursor = new(il);
            MethodInfo origInvoke = target.GetParameters()[0].ParameterType.GetMethod("Invoke")!;
            ILLabel skipIfSlughunt = cursor.DefineLabel();

            // ReSharper disable once ConditionalAccessQualifierIsNonNullableAccordingToAPIContract
            cursor.EmitDelegate(() => OnlineManager.lobby?.gameMode is SlughuntGameMode);
            cursor.Emit(OpCodes.Brtrue, skipIfSlughunt);
            for (int i = 1; i < il.Method.Parameters.Count; i++)
                cursor.Emit(OpCodes.Ldarg, i);
            cursor.Emit(OpCodes.Callvirt, origInvoke);
            cursor.Emit(OpCodes.Ret);
            cursor.MarkLabel(skipIfSlughunt);
        });
        return true;
    }
}
