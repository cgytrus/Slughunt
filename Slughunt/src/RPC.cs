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
    public static void OnCatchOrKillAsAttacker(OnlinePlayer victim, bool isCatch, bool kill) =>
        OnCatchOrKill(from, victim, isCatch, kill);

    [RPCMethod]
    public static void OnCatchOrKillAsVictim(OnlinePlayer attacker, bool isCatch, bool kill) =>
        OnCatchOrKill(attacker, from, isCatch, kill);

    private static void OnCatchOrKill(OnlinePlayer attacker, OnlinePlayer victim, bool isCatch, bool kill) {
        PlayerData attackerData = lobbyData.GetPlayerData(attacker);
        PlayerData victimData = lobbyData.GetPlayerData(victim);
        if (attackerData.pendingCatch || victimData.pendingCatch)
            isCatch = false;
        if (attackerData.role is not Rules.Role.Hunter || victimData.role is not Rules.Role.Hider)
            isCatch = false;

        if (isCatch) {
            attackerData.pendingCatch = true;
            victimData.pendingCatch = true;

            victimData.dead = victimData.dead || kill;

            Rules.OnCatch(attackerData, out int attackerStun);
            Rules.OnCatch(victimData, out int victimStun);
            lobby.NewVersion();

            attacker.InvokeRPC(OnCatchOrKillConfirm, victim, attackerData.dead, attackerStun, true);
            victim.InvokeRPC(OnCatchOrKillConfirm, attacker, victimData.dead, victimStun, true);
        }
        else if (kill) {
            Rules.OnDeath(victimData);
            lobby.NewVersion();

            victim.InvokeRPC(OnCatchOrKillConfirm, attacker, victimData.dead, 0, false);
        }
    }

    [RPCMethod]
    private static void OnCatchOrKillConfirm(OnlinePlayer otherOnline, bool die, int stun, bool isCatch) {
        Player? player = (OnlineManager.instance.manager.currentMainLoop as RainWorldGame)?.FirstRealizedPlayer;
        if (player is null)
            return;

        if (die) {
            if (
                OnlineManager.lobby.playerAvatars
                    .FirstOrDefault(x => x.Key == otherOnline)
                    .Value?
                    .FindEntity(true) is OnlinePhysicalObject { apo: AbstractCreature other }
            ) {
                player.SetKillTag(other);
            }
            player.Die();
        }

        if (stun > 0) {
            player.Stun(stun);
        }

        if (!isCatch)
            return;

        // TODO: maybe play the sound for everyone in the room?
        player.room.PlaySound(SoundID.SS_AI_Give_The_Mark_Boom, 0f, 0.5f, 1f);

        from.InvokeRPC(OnCatchOrKillConfirm2);
    }

    [RPCMethod]
    private static void OnCatchOrKillConfirm2() {
        fromData.pendingCatch = false;
        lobby.NewVersion();
    }

    [RPCMethod]
    public static void OnDeath() {
        Rules.OnDeath(fromData);
        lobby.NewVersion();
    }

    [RPCMethod]
    public static void OnRespawn() {
        Rules.OnRespawn(fromData);
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
