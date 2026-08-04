using System.IO;

namespace Slughunt;

public static partial class Rules {
    public sealed record Score {
        public uint time { get; set; }
        public uint caught { get; set; }

        public uint total => caught;

        public void Write(BinaryWriter writer) {
            writer.Write(time);
            writer.Write(caught);
        }

        public void Read(BinaryReader reader) {
            time = reader.ReadUInt32();
            caught = reader.ReadUInt32();
        }
    }
}
