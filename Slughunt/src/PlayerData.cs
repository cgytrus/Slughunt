using System.IO;
using RainMeadow;

namespace Slughunt;

public sealed record PlayerData {
    public bool ready { get; set; }

    public PlayerRole role {
        get;
        set {
            if (field == value && !dead)
                return;
            uint tick = dead ? diedAt : OnlineManager.lobby.owner.tick;
            dead = false; // cant switch roles without respawning
            if (field == PlayerRole.Hunter)
                timeAsHunter += tick - switchedRolesAt;
            else if (field == PlayerRole.Hider)
                timeAsHider += tick - switchedRolesAt;
            field = value;
            switchedRolesAt = OnlineManager.lobby.owner.tick;
        }
    }
    public uint switchedRolesAt { get; private set; }

    public bool dead { get; private set; }
    public uint diedAt { get; private set; }

    public uint timeAsHunter { get; private set; }
    public uint timeAsHider { get; private set; }
    public uint caughtAsHunter { get; set; }
    public uint caughtAsHider { get; set; }

    public long totalScore => (long)caughtAsHunter - caughtAsHider;
    public long totalTime => (long)timeAsHider - timeAsHunter;

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

    public void Die() {
        dead = true;
        diedAt = OnlineManager.lobby.owner.tick;
    }

    public void Write(BinaryWriter writer) {
        writer.Write(ready);
        writer.Write((byte)role);
        writer.Write(switchedRolesAt);
        writer.Write(dead);
        writer.Write(diedAt);
        writer.Write(timeAsHunter);
        writer.Write(timeAsHider);
        writer.Write(caughtAsHunter);
        writer.Write(caughtAsHider);
    }

    public static PlayerData Read(BinaryReader reader) => new() {
        ready = reader.ReadBoolean(),
        role = (PlayerRole)reader.ReadByte(),
        switchedRolesAt = reader.ReadUInt32(),
        dead = reader.ReadBoolean(),
        diedAt = reader.ReadUInt32(),
        timeAsHunter = reader.ReadUInt32(),
        timeAsHider = reader.ReadUInt32(),
        caughtAsHunter = reader.ReadUInt32(),
        caughtAsHider = reader.ReadUInt32()
    };
}
