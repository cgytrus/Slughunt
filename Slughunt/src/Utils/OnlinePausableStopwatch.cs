using System.IO;
using RainMeadow;

namespace Slughunt.Utils;

public class OnlinePausableStopwatch : Serializer.ICustomSerializable {
    public OnlineTimeSpan time => (isRunning ? OnlineTime.now : _pausedAt) - _startedAt;

    public bool isRunning {
        get => _pausedAt.isNever;
        set {
            if (isRunning == value)
                return;
            if (value) {
                // pretend we started later to compensate for the paused period
                _startedAt += OnlineTime.now - _pausedAt;
                _pausedAt = OnlineTime.never;
            }
            else {
                _pausedAt = OnlineTime.now;
            }
        }
    }

    private OnlineTime _startedAt;
    private OnlineTime _pausedAt;

    // reset time, no unpause!
    public void Reset() {
        _startedAt = OnlineTime.now;
        _pausedAt = isRunning ? OnlineTime.never : OnlineTime.now;
    }

    public void Write(BinaryWriter writer) {
        _startedAt.Write(writer);
        _pausedAt.Write(writer);
    }

    public void Read(BinaryReader reader) {
        _startedAt = OnlineTime.Read(reader);
        _pausedAt = OnlineTime.Read(reader);
    }

    public void CustomSerialize(Serializer serializer) {
        if (serializer.IsWriting)
            Write(serializer.writer);
        else if (serializer.IsReading)
            Read(serializer.reader);
    }

    public void ReadTo(OnlinePausableStopwatch other) {
        other._startedAt = _startedAt;
        other._pausedAt = _pausedAt;
    }
}
