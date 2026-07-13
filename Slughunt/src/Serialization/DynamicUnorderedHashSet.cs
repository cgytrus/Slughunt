using System.Collections.Generic;
using RainMeadow;
using RainMeadow.Generics;

namespace Slughunt.Serialization;

public abstract class DynamicUnorderedHashSet<T, TImp> : IDelta<TImp>, Serializer.ICustomSerializable
    where TImp : DynamicUnorderedHashSet<T, TImp>, new() {
    protected HashSet<T>? added;
    protected HashSet<T>? removed;

    protected DynamicUnorderedHashSet() { }

    protected DynamicUnorderedHashSet(HashSet<T> list) {
        added = [];
        foreach (T x in list)
            added.Add(x);
    }

    private static readonly TImp nullDelta = new();

    public TImp Delta(TImp? old) {
        if (old is null)
            return (TImp)this;

        HashSet<T>? deltaAdded = added is not null && added.Count != 0 ? added : null;
        HashSet<T>? deltaRemoved = old.added is not null && old.added.Count != 0 ? old.added : null;

        if (deltaAdded is not null && deltaRemoved is not null) {
            // added and other.added are guaranteed not to be null at this point as well
            deltaAdded = [];
            deltaRemoved = [];
            foreach (T x in added!) {
                if (!old.added!.Contains(x))
                    deltaAdded.Add(x);
            }
            foreach (T x in old.added!) {
                if (!added.Contains(x))
                    deltaRemoved.Add(x);
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
        foreach (T x in added!)
            next.added.Add(x);
        if (delta.added is not null) {
            foreach (T x in delta.added)
                next.added.Add(x);
        }
        if (delta.removed is not null) {
            foreach (T x in delta.removed)
                next.added.Remove(x);
        }
        return next;
    }

    public void ReadTo(HashSet<T> current) {
        current.Clear();
        if (added is null)
            return;
        foreach (T x in added)
            current.Add(x);
    }

    public abstract void CustomSerialize(Serializer serializer);
}
