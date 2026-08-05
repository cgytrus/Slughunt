using System;
using System.IO;
using RainMeadow;

namespace Slughunt.Utils;

public readonly record struct OnlineTimeSpan(long tick) : IEquatable<TimeSpan>, IComparable<TimeSpan>,
    IComparable<OnlineTimeSpan> {
    public TimeSpan time => TimeSpan.FromSeconds((double)tick / OnlineManager.instance.framesPerSecond);

    public int CompareTo(OnlineTimeSpan other) => tick.CompareTo(other.tick);

    public bool Equals(TimeSpan other) => time == other;
    public int CompareTo(TimeSpan other) => time.CompareTo(other);

    public static bool operator >(OnlineTimeSpan a, OnlineTimeSpan b) => a.tick > b.tick;
    public static bool operator >=(OnlineTimeSpan a, OnlineTimeSpan b) => a.tick >= b.tick;
    public static bool operator <(OnlineTimeSpan a, OnlineTimeSpan b) => a.tick < b.tick;
    public static bool operator <=(OnlineTimeSpan a, OnlineTimeSpan b) => a.tick <= b.tick;

    public static OnlineTimeSpan operator +(OnlineTimeSpan a, OnlineTimeSpan b) => new(a.tick + b.tick);
    public static OnlineTimeSpan operator -(OnlineTimeSpan a, OnlineTimeSpan b) => new(a.tick - b.tick);

    public static implicit operator TimeSpan(OnlineTimeSpan x) => x.time;

    public void Write(BinaryWriter writer) => writer.Write(tick);
    public static OnlineTimeSpan Read(BinaryReader reader) => new(reader.ReadInt64());
}
