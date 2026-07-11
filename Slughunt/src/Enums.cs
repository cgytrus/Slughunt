using System.ComponentModel;

namespace Slughunt;

public static class Rules {
    public enum OnCatch { Nothing, Death, SwitchSide }
    public enum OnRespawn { Nothing, SwitchSide }
}

public enum PlayerRole : byte { None, PreferHunter, PreferHider, Hunter, Hider }
public enum GameState : byte { Lobby, Setup, Hide, Hunt }
public enum CompassMode { Off, Radar, Room, Position } // TODO: i dont like the name radar
public enum TauntMode { Off, Sound, Radar, Room, Position }

public readonly record struct Ruleset(
    Rules.OnCatch hiderCatch, Rules.OnRespawn hiderRespawn,
    Rules.OnCatch hunterCatch, Rules.OnRespawn hunterRespawn
) {
    public static readonly Ruleset manhunt =
        new(Rules.OnCatch.Death, Rules.OnRespawn.Nothing, Rules.OnCatch.Nothing, Rules.OnRespawn.Nothing);

    public static readonly Ruleset infection1 =
        new(Rules.OnCatch.SwitchSide, Rules.OnRespawn.SwitchSide, Rules.OnCatch.Nothing, Rules.OnRespawn.Nothing);

    public static readonly Ruleset infection2 =
        new(Rules.OnCatch.SwitchSide, Rules.OnRespawn.Nothing, Rules.OnCatch.Nothing, Rules.OnRespawn.Nothing);

    public static readonly Ruleset tag1 =
        new(Rules.OnCatch.SwitchSide, Rules.OnRespawn.Nothing, Rules.OnCatch.SwitchSide, Rules.OnRespawn.Nothing);

    public static readonly Ruleset tag2 =
        new(Rules.OnCatch.SwitchSide, Rules.OnRespawn.SwitchSide, Rules.OnCatch.SwitchSide, Rules.OnRespawn.Nothing);

    public Rules.OnCatch GetCatchRuleFor(PlayerRole role) => role switch {
        PlayerRole.Hunter => hunterCatch,
        PlayerRole.Hider => hiderCatch,
        _ => Rules.OnCatch.Nothing
    };

    public Rules.OnRespawn GetRespawnRuleFor(PlayerRole role) => role switch {
        PlayerRole.Hunter => hunterRespawn,
        PlayerRole.Hider => hiderRespawn,
        _ => Rules.OnRespawn.Nothing
    };

    public enum PresetName {
        [Description("Custom")] Custom,
        [Description("Manhunt")] Manhunt,
        [Description("Infection (Variant 1)")] Infection1,
        [Description("Infection (Variant 2)")] Infection2,
        [Description("Tag (Variant 1)")] Tag1,
        [Description("Tag (Variant 2)")] Tag2
    }

    public PresetName GetPresetName() =>
        this == manhunt ? PresetName.Manhunt :
        this == infection1 ? PresetName.Infection1 :
        this == infection2 ? PresetName.Infection2 :
        this == tag1 ? PresetName.Tag1 :
        this == tag2 ? PresetName.Tag2 :
        PresetName.Custom;

    public static Ruleset GetPreset(PresetName preset, Ruleset custom) => preset switch {
        PresetName.Custom => custom,
        PresetName.Manhunt => manhunt,
        PresetName.Infection1 => infection1,
        PresetName.Infection2 => infection2,
        PresetName.Tag1 => tag1,
        PresetName.Tag2 => tag2,
        _ => default(Ruleset)
    };

    public static explicit operator byte(Ruleset x) => (byte)(
        (byte)x.hiderCatch * 2 * 3 * 2 +
        (byte)x.hiderRespawn * 2 * 3 +
        (byte)x.hunterCatch * 2 +
        (byte)x.hunterRespawn
    );

    public static explicit operator Ruleset(byte x) => new(
        (Rules.OnCatch)(x / 2 / 3 / 2 % 3),
        (Rules.OnRespawn)(x / 2 / 3 % 2),
        (Rules.OnCatch)(x / 2 % 3),
        (Rules.OnRespawn)(x % 2)
    );
}
