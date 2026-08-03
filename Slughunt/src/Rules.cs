using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using RainMeadow;

namespace Slughunt;

public static partial class Rules {
    private static LobbyData lobbyData => OnlineManager.lobby.GetData<LobbyData>();
    private static PlayerData playerData => lobbyData.GetPlayerData(OnlineManager.mePlayer);

    public enum OnCatch {
        Nothing,
        Death,
        [Description("Switch Side")] SwitchSide
    }

    public enum OnRespawn {
        Nothing,
        Block,
        [Description("Switch Side")] SwitchSide
    }

    public enum OnNextRound {
        [Description("Random Side")] RandomSide,
        [Description("Switch Side")] SwitchSide
    }

    public enum CompassMode { Off, Radar, Room, Position } // TODO: i dont like the name radar
    public enum TauntMode { Off, Sound, Radar, Room, Position }

    public static void ApplyCatch(PlayerData player, out int stun) {
        Role.Participant role = (Role.Participant)player.role;
        stun = 0;

        switch (role) {
            case Role.Hunter:
                player.caughtAsHunter++;
                break;
            case Role.Hider:
                player.caughtAsHider++;
                break;
        }

        switch (role.onCatch) {
            case OnCatch.Nothing:
                break;
            case OnCatch.Death:
                player.dead = true;
                break;
            case OnCatch.SwitchSide:
                player.role = role.oppositeRole;
                if (player.role is Role.Hunter)
                    stun = (int)(40 * lobbyData.hideTime.TotalSeconds);
                break;
            default:
                Plugin.logger.LogError($"unknown rule? {role.onCatch}");
                break;
        }
    }

    public static void ApplyRespawn(PlayerData player) {
        player.dead = false;

        if (player.role is not Role.Participant role)
            return;

        switch (role.onRespawn) {
            case OnRespawn.Nothing:
                break;
            case OnRespawn.Block:
                Plugin.logger.LogError("tried applying block respawn rule");
                break;
            case OnRespawn.SwitchSide:
                player.role = role.oppositeRole;
                break;
            default:
                Plugin.logger.LogError($"unknown rule? {role.onRespawn}");
                break;
        }
    }

    public static void ApplyNextRound() {
        IEnumerable<PlayerData> players = OnlineManager.players
            .Select(x => lobbyData.GetPlayerData(x))
            .Where(x => x.ready);
        switch (lobbyData.ruleset.nextRound) {
            case OnNextRound.RandomSide:
                foreach (PlayerData data in players) {
                    data.dead = false;
                    data.role = Role.none;
                }
                break;
            case OnNextRound.SwitchSide:
                foreach (PlayerData data in players) {
                    data.dead = false;
                    data.role = data.role.AsPreference();
                }
                break;
            default:
                Plugin.logger.LogError($"unknown rule? {lobbyData.ruleset.nextRound}");
                break;
        }
    }
}
