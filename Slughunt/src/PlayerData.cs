using System.IO;
using RainMeadow;

namespace Slughunt;

public sealed record PlayerData {
    public bool ready { get; set; }

    public bool participating => participant.participating;
    public Participant participant => new(role, 0, dead);

    private PlayerRole _role;
    public PlayerRole role {
        get => _role;
        set {
            if (_role == value)
                return;
            changedStateAt = OnlineManager.lobby.owner.tick;
            _role = value;
            if (value is not PlayerRole.Hunter and not PlayerRole.Hider)
                pendingCatch = false;
        }
    }

    private bool _dead;
    public bool dead {
        get => _dead;
        set {
            if (_dead == value)
                return;
            changedStateAt = OnlineManager.lobby.owner.tick;
            _dead = value;
        }
    }

    private uint _changedStateAt;
    public uint changedStateAt {
        get => _changedStateAt;
        private set {
            if (participating) {
                if (role == PlayerRole.Hunter)
                    timeAsHunter += value - _changedStateAt;
                else if (role == PlayerRole.Hider)
                    timeAsHider += value - _changedStateAt;
            }
            _changedStateAt = value;
        }
    }

    public uint currentStateFor => OnlineManager.lobby.owner.tick - changedStateAt;

    public bool pendingCatch { get; set; }

    public uint timeAsHunter { get; private set; }
    public uint timeAsHider { get; private set; }
    public uint caughtAsHunter { get; set; }
    public uint caughtAsHider { get; set; }

    public long totalScore => (long)caughtAsHunter - caughtAsHider;
    public long totalTime => (long)timeAsHider - timeAsHunter;

    public void ResetCurrentTimers() {
        _changedStateAt = OnlineManager.lobby.owner.tick;
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
        writer.Write(pendingCatch);
        writer.Write(timeAsHunter);
        writer.Write(timeAsHider);
        writer.Write(caughtAsHunter);
        writer.Write(caughtAsHider);
    }

    public static PlayerData Read(BinaryReader reader) => new() {
        ready = reader.ReadBoolean(),
        _role = (PlayerRole)reader.ReadByte(),
        _dead = reader.ReadBoolean(),
        _changedStateAt = reader.ReadUInt32(),
        pendingCatch = reader.ReadBoolean(),
        timeAsHunter = reader.ReadUInt32(),
        timeAsHider = reader.ReadUInt32(),
        caughtAsHunter = reader.ReadUInt32(),
        caughtAsHider = reader.ReadUInt32()
    };
}
