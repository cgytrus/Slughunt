using System.IO;
using RainMeadow;

namespace Slughunt;

public record PlayerData {
    public bool ready { get; set; }
    public PlayerRole role { get; set; }
    public uint switchedRolesAt { get; private set; }
    public uint timeAsHunter { get; private set; }
    public uint timeAsHider { get; private set; }
    public uint caughtAsHunter { get; set; }
    public uint caughtAsHider { get; set; }

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
                timeAsHunter += OnlineManager.mePlayer.tick - switchedRolesAt;
                break;
            case PlayerRole.Hider:
                role = PlayerRole.Hunter;
                timeAsHider += OnlineManager.mePlayer.tick - switchedRolesAt;
                break;
            default:
                Plugin.logger.LogError($"unknown role? {role}");
                break;
        }
        switchedRolesAt = OnlineManager.mePlayer.tick;
    }

    public uint roleCaught {
        get => role switch {
            PlayerRole.Hunter => caughtAsHunter,
            PlayerRole.Hider => caughtAsHider,
            _ => 0
        };
        set {
            switch (role) {
                case PlayerRole.Hunter:
                    caughtAsHunter = value;
                    break;
                case PlayerRole.Hider:
                    caughtAsHider = value;
                    break;
                case PlayerRole.None:
                case PlayerRole.PreferHunter:
                default:
                    break;
            }
        }
    }

    public void Write(BinaryWriter writer) {
        writer.Write(ready);
        writer.Write((byte)role);
        writer.Write(switchedRolesAt);
        writer.Write(timeAsHunter);
        writer.Write(timeAsHider);
        writer.Write(caughtAsHunter);
        writer.Write(caughtAsHider);
    }

    public static PlayerData Read(BinaryReader reader) => new() {
        ready = reader.ReadBoolean(),
        role = (PlayerRole)reader.ReadByte(),
        switchedRolesAt = reader.ReadUInt32(),
        timeAsHunter = reader.ReadUInt32(),
        timeAsHider = reader.ReadUInt32(),
        caughtAsHunter = reader.ReadUInt32(),
        caughtAsHider = reader.ReadUInt32()
    };
}
