using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using RainMeadow;

namespace Slughunt;

public static partial class Rules {
    private static LobbyData lobbyData => OnlineManager.lobby.GetData<LobbyData>();
    private static PlayerData playerData => lobbyData.GetPlayerData(OnlineManager.mePlayer);

    public enum Catch {
        Nothing,
        Death,
        [Description("Switch Side")] SwitchSide
    }

    public enum Death {
        Nothing,
        [Description("No Respawn")] NoRespawn,
        [Description("Switch Side")] SwitchSide
    }

    public enum NextRoundRole {
        Random,
        [Description("No Repeats")] NoRepeats
    }

    public enum CompassMode { Off, Radar, Room, Position } // TODO: i dont like the name radar
    public enum TauntMode { Off, Sound, Radar, Room, Position }

    public static void OnCatch(PlayerData player, out int stun) {
        Role.Participant role = (Role.Participant)player.role;
        stun = 0;

        switch (role.catchRule) {
            case Catch.Nothing:
                break;
            case Catch.Death:
                player.dead = true;
                break;
            case Catch.SwitchSide:
                player.role = role.oppositeRole;
                if (player.role is Role.Hunter)
                    stun = (int)(40 * lobbyData.hideTime.TotalSeconds);
                break;
            default:
                Plugin.logger.LogError($"unknown rule? {role.catchRule}");
                break;
        }
    }

    public static void OnDeath(PlayerData player) {
        player.dead = true;

        if (player.role is not Role.Participant role)
            return;

        switch (role.deathRule) {
            case Death.Nothing:
            case Death.NoRespawn:
                break;
            case Death.SwitchSide:
                player.role = role.oppositeRole;
                break;
            default:
                Plugin.logger.LogError($"unknown rule? {role.deathRule}");
                break;
        }
    }

    public static void OnRespawn(PlayerData player) {
        player.dead = false;
    }

    public static void OnNextRound() {
        IEnumerable<PlayerData> players = OnlineManager.players
            .Select(x => lobbyData.GetPlayerData(x))
            .Where(x => x.ready);
        switch (lobbyData.ruleset.nextRoundRole) {
            case NextRoundRole.Random:
                foreach (PlayerData data in players) {
                    data.dead = false;
                    data.role = Role.none;
                }
                break;
            case NextRoundRole.NoRepeats:
                foreach (PlayerData data in players) {
                    data.dead = false;
                    data.role = data.role.AsParticipant()?.oppositeRole.AsPreference() ?? data.role;
                }
                break;
            default:
                Plugin.logger.LogError($"unknown rule? {lobbyData.ruleset.nextRoundRole}");
                break;
        }
    }
}
