using System.IO;
using RainMeadow;

namespace Slughunt;

public sealed record PlayerData {
    public bool ready { get; set; }

    public bool participating => participant.participating;
    public Participant participant {
        get {
            LobbyData lobbyData = OnlineManager.lobby.GetData<LobbyData>();
            int stun = role == PlayerRole.Hunter ? (int)((long)lobbyData.hideTimeFrames - currentStateFor) : 0;
            return new Participant(role, stun, dead, lobbyData);
        }
    }

    public PlayerRole role {
        get;
        set {
            if (field == value)
                return;
            changedStateAt = OnlineManager.lobby.owner.tick;
            field = value;
        }
    }

    public bool dead {
        get;
        set {
            if (field == value)
                return;
            changedStateAt = OnlineManager.lobby.owner.tick;
            field = value;
        }
    }

    public uint changedStateAt {
        get;
        private set {
            if (participating) {
                if (role == PlayerRole.Hunter)
                    timeAsHunter += value - field;
                else if (role == PlayerRole.Hider)
                    timeAsHider += value - field;
            }
            field = value;
        }
    }

    public uint currentStateFor => OnlineManager.lobby.owner.tick - changedStateAt;

    public uint timeAsHunter { get; private set; }
    public uint timeAsHider { get; private set; }
    public uint caughtAsHunter { get; set; }
    public uint caughtAsHider { get; set; }

    public long totalScore => (long)caughtAsHunter - caughtAsHider;
    public long totalTime => (long)timeAsHider - timeAsHunter;

    public void ResetCurrentTimers() {
        uint savedTimeAsHider = timeAsHider;
        uint savedTimeAsHunter = timeAsHunter;
        changedStateAt = OnlineManager.lobby.owner.tick;
        timeAsHider = savedTimeAsHider;
        timeAsHunter = savedTimeAsHunter;
    }

    public void SwitchSide() {
        switch (role) {
            case PlayerRole.None:
                role = PlayerRole.PreferHunter;
                break;
            case PlayerRole.PreferHunter:
                role = PlayerRole.PreferHider;
                break;
            case PlayerRole.PreferHider:
                role = PlayerRole.None;
                break;
            case PlayerRole.Hunter:
                role = PlayerRole.Hider;
                break;
            case PlayerRole.Hider:
                role = PlayerRole.Hunter;
                break;
            default:
                Plugin.logger.LogError($"unknown role? {role}");
                break;
        }
    }

    public void Write(BinaryWriter writer) {
        writer.Write(ready);
        writer.Write((byte)role);
        writer.Write(dead);
        writer.Write(changedStateAt);
        writer.Write(timeAsHunter);
        writer.Write(timeAsHider);
        writer.Write(caughtAsHunter);
        writer.Write(caughtAsHider);
    }

    public static PlayerData Read(BinaryReader reader) => new() {
        ready = reader.ReadBoolean(),
        role = (PlayerRole)reader.ReadByte(),
        dead = reader.ReadBoolean(),
        changedStateAt = reader.ReadUInt32(),
        timeAsHunter = reader.ReadUInt32(),
        timeAsHider = reader.ReadUInt32(),
        caughtAsHunter = reader.ReadUInt32(),
        caughtAsHider = reader.ReadUInt32()
    };
}
