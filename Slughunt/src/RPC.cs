using System;
using System.Linq;
using RainMeadow;

namespace Slughunt;

public static class RPC {
    private static Lobby lobby => OnlineManager.lobby;
    private static LobbyData lobbyData => lobby.GetData<LobbyData>();
    private static OnlinePlayer from => RPCEvent.currentRPCEvent!.from;
    private static PlayerData fromData => lobbyData.GetPlayerData(from);

    [RPCMethod]
    public static void OnCatchAsHunter(OnlinePlayer hider) => OnCatch(from, hider);

    [RPCMethod]
    public static void OnCatchAsHider(OnlinePlayer hunter) => OnCatch(hunter, from);

    private static void OnCatch(OnlinePlayer hunter, OnlinePlayer hider) {
        if (OnCatchConfirmPending(hunter) || OnCatchConfirmPending(hider))
            return;

        PlayerData hunterData = lobbyData.GetPlayerData(hunter);
        PlayerData hiderData = lobbyData.GetPlayerData(hider);
        if (hunterData.role is not Rules.Role.Hunter || hiderData.role is not Rules.Role.Hider)
            return;

        Rules.ApplyCatch(hunterData, out int hunterStun);
        Rules.ApplyCatch(hiderData, out int hiderStun);
        lobby.NewVersion();

        hunter.InvokeRPC(OnCatchConfirm, hunterData.dead, hunterStun);
        hider.InvokeRPC(OnCatchConfirm, hiderData.dead, hiderStun);
    }

    [RPCMethod(runDeferred = true)]
    private static void OnCatchConfirm(bool die, int stun) {
        Player? player = (OnlineManager.instance.manager.currentMainLoop as RainWorldGame)?.FirstRealizedPlayer;
        if (player is null)
            return;
        if (die)
            player.Die();
        if (stun > 0)
            player.Stun(stun);
        // TODO: maybe play the sound for everyone in the room?
        player.room.PlaySound(SoundID.SS_AI_Give_The_Mark_Boom, 0f, 0.5f, 1f);
    }

    private static bool OnCatchConfirmPending(OnlinePlayer player) => player.OutgoingEvents.Any(x =>
        x is RPCEvent rpc && rpc.handler.method == ((Delegate)OnCatchConfirm).Method
    );

    [RPCMethod]
    public static void OnDeath() {
        fromData.dead = true;
        lobby.NewVersion();
    }

    [RPCMethod]
    public static void OnRespawn() {
        Rules.ApplyRespawn(fromData);
        lobby.NewVersion();
    }

    [RPCMethod]
    public static void SwitchReady() {
        if (fromData.ready)
            lobbyData.state.Leave(fromData);
        else
            lobbyData.state.Join(fromData);
        lobby.NewVersion();
    }

    [RPCMethod]
    public static void SwitchPreference() {
        fromData.role = fromData.role.AsPreference().nextAllowed;
        lobby.NewVersion();
    }

    [RPCMethod]
    public static void WantExit() {
        lobbyData.state.Leave(fromData);
        lobby.NewVersion();
    }
}
