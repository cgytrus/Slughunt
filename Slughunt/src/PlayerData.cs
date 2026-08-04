using System.IO;

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
            unsavedTime.running = role.IsTimed(dead);
            score.time += unsavedTime.Save();
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
            unsavedTime.running = role.IsTimed(dead);
        }
    }

    public NetTimer unsavedTime { get; } = new();

    public Rules.Score hunterScore { get; } = new();
    public Rules.Score hiderScore { get; } = new();

    public long currentTotalTime => role switch {
        Rules.Role.Hunter => totalTime - unsavedTime.time,
        Rules.Role.Hider => totalTime + unsavedTime.time,
        _ => totalTime
    };

    public Rules.Score score => role switch {
        Rules.Role.Hunter => hunterScore,
        Rules.Role.Hider => hiderScore,
        _ => new Rules.Score()
    };

    public long totalScore => (long)hunterScore.total - hiderScore.total;
    public long totalTime => (long)hiderScore.time - hunterScore.time;

    public void Write(BinaryWriter writer) {
        writer.Write(ready);
        writer.Write(pendingCatch);
        writer.Write((byte)role);
        writer.Write(dead);
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
}
