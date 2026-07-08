using System.Collections.Generic;
using RainMeadow;

namespace Slughunt.Serialization;

public class DynamicUnorderedShortcuts : DynamicUnorderedHashSet<LobbyData.Shortcut, DynamicUnorderedShortcuts> {
    public DynamicUnorderedShortcuts() { }
    public DynamicUnorderedShortcuts(HashSet<LobbyData.Shortcut> list) : base(list) { }

    public override void CustomSerialize(Serializer serializer) {
        Serialize(serializer, ref added);
        if (serializer.IsDelta)
            Serialize(serializer, ref removed);
    }

    private static void Serialize(Serializer serializer, ref HashSet<LobbyData.Shortcut>? data) {
        if (serializer.IsWriting)
            Write(serializer, data);
        if (serializer.IsReading)
            Read(serializer, out data);
        // can a serializer be not reading and not writing or both reading and writing?????
    }

    private static void Write(Serializer serializer, HashSet<LobbyData.Shortcut>? data) {
        serializer.writer.Write((byte)(data?.Count ?? 0));
        if (data is null)
            return;
        foreach (LobbyData.Shortcut shortcut in data) {
            serializer.writer.Write(shortcut.a);
            serializer.writer.Write(shortcut.b);
        }
    }

    private static void Read(Serializer serializer, out HashSet<LobbyData.Shortcut>? data) {
        byte count = serializer.reader.ReadByte();
        if (count == 0) {
            data = null;
            return;
        }
        data = [];
        for (int i = 0; i < count; ++i)
            data.Add(new LobbyData.Shortcut(serializer.reader.ReadString(), serializer.reader.ReadString()));
    }
}
