using System.Collections.Generic;

namespace Slughunt;

public abstract class EnumClass<TSelf> where TSelf : EnumClass<TSelf> {
    private static readonly List<TSelf> instances = [];

    private readonly int _value = instances.Count;
    protected EnumClass() => instances.Add((TSelf)this);

    public static explicit operator int(EnumClass<TSelf> role) => role._value;
    public static explicit operator EnumClass<TSelf>(int value) => instances[value];
}
