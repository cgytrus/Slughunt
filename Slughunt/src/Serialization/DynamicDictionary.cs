using System;
using System.Collections.Generic;
using RainMeadow;
using RainMeadow.Generics;

namespace Slughunt.Serialization;

public abstract class DynamicDictionary<TKey, TValue, TImp> : IDelta<TImp>, Serializer.ICustomSerializable
    where TValue : IEquatable<TValue>
    where TImp : DynamicDictionary<TKey, TValue, TImp>, new() {
    protected Dictionary<TKey, TValue>? added;
    protected Dictionary<TKey, TValue>? removed;

    private static readonly TImp nullDelta = new();

    public TImp Delta(TImp? old) {
        if (old is null)
            return (TImp)this;

        Dictionary<TKey, TValue>? deltaAdded = added is not null && added.Count != 0 ? added : null;
        Dictionary<TKey, TValue>? deltaRemoved = old.added is not null && old.added.Count != 0 ? old.added : null;

        if (deltaAdded is not null && deltaRemoved is not null) {
            // added and other.added are guaranteed not to be null at this point as well
            deltaAdded = [];
            deltaRemoved = [];
            foreach (KeyValuePair<TKey, TValue> x in added!) {
                if (!old.added!.ContainsKey(x.Key) || !old.added[x.Key].Equals(x.Value))
                    deltaAdded.Add(x.Key, x.Value);
            }
            foreach (KeyValuePair<TKey, TValue> x in old.added!) {
                if (!added.ContainsKey(x.Key))
                    deltaRemoved.Add(x.Key, x.Value);
            }
        }

        bool hasAdded = deltaAdded is not null && deltaAdded.Count != 0;
        bool hasRemoved = deltaRemoved is not null && deltaRemoved.Count != 0;

        return hasAdded || hasRemoved ? new TImp {
            added = hasAdded ? deltaAdded : null,
            removed = hasRemoved ? deltaRemoved : null
        } : nullDelta;
    }

    public TImp ApplyDelta(TImp? delta) {
        if (delta is null)
            return new TImp { added = added };
        TImp next = new() {
            added = []
        };
        foreach (KeyValuePair<TKey, TValue> x in added!)
            next.added.Add(x.Key, x.Value);
        if (delta.added is not null) {
            foreach (KeyValuePair<TKey, TValue> x in delta.added)
                next.added.Add(x.Key, x.Value);
        }
        if (delta.removed is not null) {
            foreach (TKey x in delta.removed.Keys)
                next.added.Remove(x);
        }
        return next;
    }

    public void ReadTo(Dictionary<TKey, TValue> current) {
        current.Clear();
        if (added is null)
            return;
        foreach (KeyValuePair<TKey, TValue> x in added)
            current[x.Key] = x.Value;
    }

    public abstract void CustomSerialize(Serializer serializer);
}
