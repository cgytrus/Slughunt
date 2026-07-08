using System.Collections.Generic;
using RainMeadow;

namespace Slughunt.Serialization;

public class DynamicUnorderedStrings : DynamicUnorderedHashSet<string, DynamicUnorderedStrings> {
    public DynamicUnorderedStrings() { }
    public DynamicUnorderedStrings(HashSet<string> list) : base(list) { }

    public override void CustomSerialize(Serializer serializer) {
        Serialize(serializer, ref added);
        if (serializer.IsDelta)
            Serialize(serializer, ref removed);
    }

    private static void Serialize(Serializer serializer, ref HashSet<string>? data) {
        if (serializer.IsWriting)
            Write(serializer, data);
        if (serializer.IsReading)
            Read(serializer, out data);
        // can a serializer be not reading and not writing or both reading and writing?????
    }

    private static void Write(Serializer serializer, HashSet<string>? data) {
        serializer.writer.Write((byte)(data?.Count ?? 0));
        if (data is null)
            return;
        foreach (string x in data)
            serializer.writer.Write(x);
    }

    private static void Read(Serializer serializer, out HashSet<string>? data) {
        byte count = serializer.reader.ReadByte();
        if (count == 0) {
            data = null;
            return;
        }
        data = [];
        for (int i = 0; i < count; ++i)
            data.Add(serializer.reader.ReadString());
    }
}
