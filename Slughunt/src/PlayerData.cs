using System.IO;
using RainMeadow;

namespace Slughunt;

public sealed record PlayerData {
    private static uint currentTick => OnlineManager.lobby.owner.tick;

    public bool ready { get; set; }

    public bool pendingCatch { get; set; }

    private Rules.Role _role = Rules.Role.none;
    public Rules.Role role {
        get => _role;
        set {
            if (_role == value)
                return;
            SaveTime(); // unsaved time depends on role so it must be saved before role is changed
            _role = value;
            if (value is not Rules.Role.Participant)
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
            if (role.IsTimed(dead)) {
                // pretend we started later to compensate for the untimed period
                _changedRoleAt += currentTick - _stoppedTimingAt;
            }
            else {
                _stoppedTimingAt = currentTick;
            }
        }
    }

    private uint _changedRoleAt;
    private uint _stoppedTimingAt;

    public uint unsavedTime => (role.IsTimed(dead) ? currentTick : _stoppedTimingAt) - _changedRoleAt;

    public long currentTotalTime => role switch {
        Rules.Role.Hunter => totalTime - unsavedTime,
        Rules.Role.Hider => totalTime + unsavedTime,
        _ => totalTime
    };

    public uint timeAsHunter { get; private set; }
    public uint timeAsHider { get; private set; }
    public uint caughtAsHunter { get; set; }
    public uint caughtAsHider { get; set; }

    public long totalScore => (long)caughtAsHunter - caughtAsHider;
    public long totalTime => (long)timeAsHider - timeAsHunter;

    public void ResetUnsavedTime() {
        _changedRoleAt = currentTick;
        _stoppedTimingAt = currentTick;
    }

    private void SaveTime() {
        if (role is Rules.Role.Hunter)
            timeAsHunter += unsavedTime;
        else if (role is Rules.Role.Hider)
            timeAsHider += unsavedTime;
        ResetUnsavedTime();
    }

    public void Write(BinaryWriter writer) {
        writer.Write(ready);
        writer.Write(pendingCatch);
        writer.Write((byte)role);
        writer.Write(dead);
        writer.Write(_changedRoleAt);
        writer.Write(_stoppedTimingAt);
        writer.Write(timeAsHunter);
        writer.Write(timeAsHider);
        writer.Write(caughtAsHunter);
        writer.Write(caughtAsHider);
    }

    public static PlayerData Read(BinaryReader reader) => new() {
        ready = reader.ReadBoolean(),
        pendingCatch = reader.ReadBoolean(),
        _role = (Rules.Role)reader.ReadByte(),
        _dead = reader.ReadBoolean(),
        _changedRoleAt = reader.ReadUInt32(),
        _stoppedTimingAt = reader.ReadUInt32(),
        timeAsHunter = reader.ReadUInt32(),
        timeAsHider = reader.ReadUInt32(),
        caughtAsHunter = reader.ReadUInt32(),
        caughtAsHider = reader.ReadUInt32()
    };
}
