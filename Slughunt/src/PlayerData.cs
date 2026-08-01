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
            SavePausedUnsavedTime();
            _dead = value;
        }
    }

    private uint _changedRoleAt;
    private uint _changedParticipatingAt;
    private uint _pausedUnsavedTimeAsCurrentRole;

    private uint unsavedPausedUnsavedTimeAsCurrentRole =>
        participating ? 0 : OnlineManager.lobby.owner.tick - _changedParticipatingAt;

    public uint unsavedTimeAsCurrentRole => OnlineManager.lobby.owner.tick - _changedRoleAt -
        _pausedUnsavedTimeAsCurrentRole - unsavedPausedUnsavedTimeAsCurrentRole;

    public long currentTotalTime => role switch {
        PlayerRole.Hunter => totalTime - unsavedTimeAsCurrentRole,
        PlayerRole.Hider => totalTime + unsavedTimeAsCurrentRole,
        _ => totalTime
    };

    public uint timeAsHunter { get; private set; }
    public uint timeAsHider { get; private set; }
    public uint caughtAsHunter { get; set; }
    public uint caughtAsHider { get; set; }

    public bool pendingCatch { get; set; }

    public long totalScore => (long)caughtAsHunter - caughtAsHider;
    public long totalTime => (long)timeAsHider - timeAsHunter;

    public void ResetUnsavedTime() {
        _changedRoleAt = OnlineManager.lobby.owner.tick;
        _changedParticipatingAt = OnlineManager.lobby.owner.tick;
        _pausedUnsavedTimeAsCurrentRole = 0;
    }

    private void SavePausedUnsavedTime() {
        _pausedUnsavedTimeAsCurrentRole += unsavedPausedUnsavedTimeAsCurrentRole;
        _changedParticipatingAt = OnlineManager.lobby.owner.tick;
        _pausedUnsavedTimeAsCurrentRole = 0;
    }

    private void SaveTime() {
        if (role == PlayerRole.Hunter)
            timeAsHunter += unsavedTimeAsCurrentRole;
        else if (role == PlayerRole.Hider)
            timeAsHider += unsavedTimeAsCurrentRole;
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
        writer.Write((byte)role);
        writer.Write(dead);
        writer.Write(_changedRoleAt);
        writer.Write(_changedParticipatingAt);
        writer.Write(_pausedUnsavedTimeAsCurrentRole);
        writer.Write(timeAsHunter);
        writer.Write(timeAsHider);
        writer.Write(caughtAsHunter);
        writer.Write(caughtAsHider);
        writer.Write(pendingCatch);
    }

    public static PlayerData Read(BinaryReader reader) => new() {
        ready = reader.ReadBoolean(),
        _role = (PlayerRole)reader.ReadByte(),
        _dead = reader.ReadBoolean(),
        _changedRoleAt = reader.ReadUInt32(),
        _changedParticipatingAt = reader.ReadUInt32(),
        _pausedUnsavedTimeAsCurrentRole = reader.ReadUInt32(),
        timeAsHunter = reader.ReadUInt32(),
        timeAsHider = reader.ReadUInt32(),
        caughtAsHunter = reader.ReadUInt32(),
        caughtAsHider = reader.ReadUInt32(),
        pendingCatch = reader.ReadBoolean()
    };
}
