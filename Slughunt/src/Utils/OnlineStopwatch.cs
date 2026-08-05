using System.IO;
using RainMeadow;

namespace Slughunt.Utils;

public class OnlineStopwatch : Serializer.ICustomSerializable {
    public OnlineTimeSpan time => OnlineTime.now - _startedAt;

    private OnlineTime _startedAt;

    // reset time, no unpause!
    public void Reset() {
        _startedAt = OnlineTime.now;
    }

    public void Write(BinaryWriter writer) {
        _startedAt.Write(writer);
    }

    public void Read(BinaryReader reader) {
        _startedAt = OnlineTime.Read(reader);
    }

    public void CustomSerialize(Serializer serializer) {
        if (serializer.IsWriting)
            Write(serializer.writer);
        else if (serializer.IsReading)
            Read(serializer.reader);
    }

    public void ReadTo(OnlineStopwatch other) {
        other._startedAt = _startedAt;
    }
}
