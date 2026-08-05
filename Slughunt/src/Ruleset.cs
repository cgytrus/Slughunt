using System.ComponentModel;
using System.IO;
using RainMeadow;

namespace Slughunt;

// TODO: figure out how to make this readonly again
public record struct Ruleset(
    Rules.Catch hiderCatch, Rules.Death hiderDeath,
    Rules.Catch hunterCatch, Rules.Death hunterDeath,
    Rules.NextRoundRole nextRoundRole
) : Serializer.ICustomSerializable {
    public static readonly Ruleset hideAndSeek = new(
        Rules.Catch.Death, Rules.Death.NoRespawn,
        Rules.Catch.Nothing, Rules.Death.Nothing,
        Rules.NextRoundRole.Random
    );

    public static readonly Ruleset infection = new(
        Rules.Catch.SwitchSide, Rules.Death.SwitchSide,
        Rules.Catch.Nothing, Rules.Death.Nothing,
        Rules.NextRoundRole.Random
    );

    public static readonly Ruleset tag = new(
        Rules.Catch.SwitchSide, Rules.Death.Nothing,
        Rules.Catch.SwitchSide, Rules.Death.Nothing,
        Rules.NextRoundRole.NoRepeats
    );

    public static readonly Ruleset tag2P = new(
        Rules.Catch.SwitchSide, Rules.Death.NoRespawn,
        Rules.Catch.SwitchSide, Rules.Death.Nothing,
        Rules.NextRoundRole.NoRepeats
    );

    public enum PresetName {
        [Description("Custom")] Custom,
        [Description("Hide and Seek")] HideAndSeek,
        [Description("Infection")] Infection,
        [Description("Tag")] Tag,
        [Description("Tag (for 2 players)")] Tag2P
    }

    public PresetName GetPresetName() =>
        this == hideAndSeek ? PresetName.HideAndSeek :
        this == infection ? PresetName.Infection :
        this == tag ? PresetName.Tag :
        this == tag2P ? PresetName.Tag2P :
        PresetName.Custom;

    public static Ruleset GetPreset(PresetName preset, Ruleset custom) => preset switch {
        PresetName.Custom => custom,
        PresetName.HideAndSeek => hideAndSeek,
        PresetName.Infection => infection,
        PresetName.Tag => tag,
        PresetName.Tag2P => tag2P,
        _ => default(Ruleset)
    };

    public void Write(BinaryWriter writer) {
        writer.Write((byte)hiderCatch);
        writer.Write((byte)hiderDeath);
        writer.Write((byte)hunterCatch);
        writer.Write((byte)hunterDeath);
        writer.Write((byte)nextRoundRole);
    }

    public void Read(BinaryReader reader) {
        hiderCatch = (Rules.Catch)reader.ReadByte();
        hiderDeath = (Rules.Death)reader.ReadByte();
        hunterCatch = (Rules.Catch)reader.ReadByte();
        hunterDeath = (Rules.Death)reader.ReadByte();
        nextRoundRole = (Rules.NextRoundRole)reader.ReadByte();
    }

    public void CustomSerialize(Serializer serializer) {
        if (serializer.IsWriting)
            Write(serializer.writer);
        else if (serializer.IsReading)
            Read(serializer.reader);
    }
}
