using System.Collections.Generic;

namespace Slughunt.Utils;

public abstract class DiscriminatedUnion<TSelf> where TSelf : DiscriminatedUnion<TSelf> {
    private static readonly List<TSelf> instances = [];

    private readonly int _value = instances.Count;
    protected DiscriminatedUnion() => instances.Add((TSelf)this);

    public static explicit operator int(DiscriminatedUnion<TSelf> role) => role._value;
    public static explicit operator DiscriminatedUnion<TSelf>(int value) => instances[value];
}
