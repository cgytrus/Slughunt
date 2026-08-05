using System;
using System.IO;
using RainMeadow;

namespace Slughunt.Utils;

public readonly record struct OnlineTime(uint tick) : IComparable<OnlineTime> {
    public static OnlineTime now => new(OnlineManager.lobby.owner.tick);
    public static OnlineTime never => new(0);

    public TimeSpan time => TimeSpan.FromSeconds((double)tick / OnlineManager.instance.framesPerSecond);

    public bool isNever => tick == 0;

    public int CompareTo(OnlineTime other) => tick.CompareTo(other.tick);

    public static bool operator >(OnlineTime a, OnlineTime b) => a.tick > b.tick;

    public static bool operator >=(OnlineTime a, OnlineTime b) => a.tick >= b.tick;

    public static bool operator <(OnlineTime a, OnlineTime b) => a.tick < b.tick;

    public static bool operator <=(OnlineTime a, OnlineTime b) => a.tick <= b.tick;

    public static OnlineTime operator +(OnlineTime a, OnlineTimeSpan b) => new((uint)(a.tick + b.tick));
    public static OnlineTimeSpan operator -(OnlineTime a, OnlineTime b) => new((long)a.tick - b.tick);

    public void Write(BinaryWriter writer) => writer.Write(tick);
    public static OnlineTime Read(BinaryReader reader) => new(reader.ReadUInt32());
}
