using System.IO;
using RainMeadow;
using Slughunt.Utils;

namespace Slughunt;

public sealed record PlayerData {
    public bool ready { get; set; }

    public bool pendingCatch { get; set; }

    private Rules.Role _role = Rules.Role.none;
    public Rules.Role role {
        get => _role;
        set {
            if (_role == value)
                return;
            _role = value;
            unsavedTime.isRunning = role.IsTimed(dead);
            score.time += unsavedTime.time;
            unsavedTime.Reset();
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
            unsavedTime.isRunning = role.IsTimed(dead);
        }
    }

    public OnlinePausableStopwatch unsavedTime { get; } = new();

    public Rules.Score hunterScore { get; } = new(Rules.Role.hunter);
    public Rules.Score hiderScore { get; } = new(Rules.Role.hider);

    public OnlineTimeSpan currentTotalTime => role switch {
        Rules.Role.Hunter => totalTime - unsavedTime.time,
        Rules.Role.Hider => totalTime + unsavedTime.time,
        _ => totalTime
    };

    public Rules.Score score => role switch {
        Rules.Role.Hunter => hunterScore,
        Rules.Role.Hider => hiderScore,
        _ => new Rules.Score(Rules.Role.hunter) // placeholder
    };

    public long totalScore => hunterScore.total + hiderScore.total;
    public OnlineTimeSpan totalTime => hiderScore.time - hunterScore.time;

    public void Write(BinaryWriter writer) {
        writer.Write(ready);
        writer.Write(pendingCatch);
        writer.Write((byte)_role);
        writer.Write(_dead);
        unsavedTime.Write(writer);
        hunterScore.Write(writer);
        hiderScore.Write(writer);
    }

    public void Read(BinaryReader reader) {
        ready = reader.ReadBoolean();
        pendingCatch = reader.ReadBoolean();
        _role = (Rules.Role)reader.ReadByte();
        _dead = reader.ReadBoolean();
        unsavedTime.Read(reader);
        hunterScore.Read(reader);
        hiderScore.Read(reader);
    }

    public void ReadTo(PlayerData other) {
        other.ready = ready;
        other.pendingCatch = pendingCatch;
        other._role = _role;
        other._dead = _dead;
        unsavedTime.ReadTo(other.unsavedTime);
        hunterScore.ReadTo(other.hunterScore);
        hiderScore.ReadTo(other.hiderScore);
    }
}
