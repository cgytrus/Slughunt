using System.IO;
using RainMeadow;

namespace Slughunt;

public class NetTimer {
    private static uint currentTick => OnlineManager.lobby.owner.tick;

    public uint time => (running ? currentTick : _pausedAt) - _startedAt;

    public bool running {
        get => _pausedAt == 0;
        set {
            if (running == value)
                return;
            if (value) {
                // pretend we started later to compensate for the paused period
                _startedAt += currentTick - _pausedAt;
                _pausedAt = 0;
            }
            else {
                _pausedAt = currentTick;
            }
        }
    }

    private uint _startedAt;
    private uint _pausedAt;

    // reset time, no unpause!
    public void Reset() {
        _startedAt = currentTick;
        _pausedAt = running ? 0 : currentTick;
    }

    public uint Save() {
        uint saved = time;
        _startedAt = currentTick;
        return saved;
    }

    public void Write(BinaryWriter writer) {
        writer.Write(_startedAt);
        writer.Write(_pausedAt);
    }

    public void Read(BinaryReader reader) {
        _startedAt = reader.ReadUInt32();
        _pausedAt = reader.ReadUInt32();
    }
}
