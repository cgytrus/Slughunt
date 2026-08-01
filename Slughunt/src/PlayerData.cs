using System.IO;
using RainMeadow;

namespace Slughunt;

public sealed record PlayerData {
    public bool ready { get; set; }

    public bool pendingCatch { get; set; }

    public bool participating => participant.participating;
    public Participant participant => new(role, 0, dead);

    private PlayerRole _role;
    public PlayerRole role {
        get => _role;
        set {
            if (_role == value)
                return;
            SaveTime();
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
            _dead = value;
            if (participating) {
                // pretend we started later to account for time not participating
                _changedRoleAt += OnlineManager.lobby.owner.tick - _stoppedParticipatingAt;
            }
            else {
                _stoppedParticipatingAt = OnlineManager.lobby.owner.tick;
            }
        }
    }

    private uint _changedRoleAt;
    private uint _stoppedParticipatingAt;

    private uint lastParticipatingAt => participating ? OnlineManager.lobby.owner.tick : _stoppedParticipatingAt;

    public uint unsavedTime => lastParticipatingAt - _changedRoleAt;

    public long currentTotalTime => role switch {
        PlayerRole.Hunter => totalTime - unsavedTime,
        PlayerRole.Hider => totalTime + unsavedTime,
        _ => totalTime
    };

    public uint timeAsHunter { get; private set; }
    public uint timeAsHider { get; private set; }
    public uint caughtAsHunter { get; set; }
    public uint caughtAsHider { get; set; }

    public long totalScore => (long)caughtAsHunter - caughtAsHider;
    public long totalTime => (long)timeAsHider - timeAsHunter;

    public void ResetUnsavedTime() {
        _changedRoleAt = OnlineManager.lobby.owner.tick;
        _stoppedParticipatingAt = OnlineManager.lobby.owner.tick;
    }

    private void SaveTime() {
        if (role == PlayerRole.Hunter)
            timeAsHunter += unsavedTime;
        else if (role == PlayerRole.Hider)
            timeAsHider += unsavedTime;
        ResetUnsavedTime();
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
        writer.Write(pendingCatch);
        writer.Write((byte)role);
        writer.Write(dead);
        writer.Write(_changedRoleAt);
        writer.Write(_stoppedParticipatingAt);
        writer.Write(timeAsHunter);
        writer.Write(timeAsHider);
        writer.Write(caughtAsHunter);
        writer.Write(caughtAsHider);
    }

    public static PlayerData Read(BinaryReader reader) => new() {
        ready = reader.ReadBoolean(),
        pendingCatch = reader.ReadBoolean(),
        _role = (PlayerRole)reader.ReadByte(),
        _dead = reader.ReadBoolean(),
        _changedRoleAt = reader.ReadUInt32(),
        _stoppedParticipatingAt = reader.ReadUInt32(),
        timeAsHunter = reader.ReadUInt32(),
        timeAsHider = reader.ReadUInt32(),
        caughtAsHunter = reader.ReadUInt32(),
        caughtAsHider = reader.ReadUInt32()
    };
}
