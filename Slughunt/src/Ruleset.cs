using System;
using System.ComponentModel;

namespace Slughunt;

public readonly record struct Ruleset(
    Rules.Catch hiderCatch, Rules.Death hiderDeath,
    Rules.Catch hunterCatch, Rules.Death hunterDeath,
    Rules.NextRoundRole nextRoundRole
) {
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
        Rules.Catch.SwitchSide, Rules.Death.NoRespawn,
        Rules.Catch.SwitchSide, Rules.Death.Nothing,
        Rules.NextRoundRole.NoRepeats
    );

    public enum PresetName {
        [Description("Custom")] Custom,
        [Description("Hide and Seek")] HideAndSeek,
        [Description("Infection")] Infection,
        [Description("Tag")] Tag
    }

    public PresetName GetPresetName() =>
        this == hideAndSeek ? PresetName.HideAndSeek :
        this == infection ? PresetName.Infection :
        this == tag ? PresetName.Tag :
        PresetName.Custom;

    public static Ruleset GetPreset(PresetName preset, Ruleset custom) => preset switch {
        PresetName.Custom => custom,
        PresetName.HideAndSeek => hideAndSeek,
        PresetName.Infection => infection,
        PresetName.Tag => tag,
        _ => default(Ruleset)
    };

    private static readonly int catchCount = Enum.GetValues(typeof(Rules.Catch)).Length;
    private static readonly int deathCount = Enum.GetValues(typeof(Rules.Death)).Length;
    private static readonly int nextRoundRoleCount = Enum.GetValues(typeof(Rules.NextRoundRole)).Length;

    private static readonly int catchSize = (int)Math.Ceiling(Math.Log(catchCount + 1, 2));
    private static readonly int deathSize = (int)Math.Ceiling(Math.Log(deathCount + 1, 2));
    private static readonly int nextRoundRoleSize = (int)Math.Ceiling(Math.Log(nextRoundRoleCount + 1, 2));

    private static readonly int catchMax = (1 << catchSize) - 1;
    private static readonly int deathMax = (1 << deathSize) - 1;
    private static readonly int nextRoundRoleMax = (1 << nextRoundRoleSize) - 1;

    private static readonly int hiderCatchOffset = 0;
    private static readonly int hiderDeathOffset = hiderCatchOffset + catchSize;
    private static readonly int hunterCatchOffset = hiderDeathOffset + deathSize;
    private static readonly int hunterDeathOffset = hunterCatchOffset + catchSize;
    private static readonly int nextRoundRoleOffset = hunterDeathOffset + deathSize;

    public static explicit operator byte(Ruleset x) => (byte)(
        ((byte)x.hiderCatch << hiderCatchOffset) |
        ((byte)x.hiderDeath << hiderDeathOffset) |
        ((byte)x.hunterCatch << hunterCatchOffset) |
        ((byte)x.hunterDeath << hunterDeathOffset) |
        ((byte)x.nextRoundRole << nextRoundRoleOffset)
    );

    public static explicit operator Ruleset(byte x) => new(
        (Rules.Catch)((x >> hiderCatchOffset) & catchMax),
        (Rules.Death)((x >> hiderDeathOffset) & deathMax),
        (Rules.Catch)((x >> hunterCatchOffset) & catchMax),
        (Rules.Death)((x >> hunterDeathOffset) & deathMax),
        (Rules.NextRoundRole)((x >> nextRoundRoleOffset) & nextRoundRoleMax)
    );
}
