using System.ComponentModel;

namespace Slughunt;

public static class Rules {
    public enum OnCatch {
        Nothing,
        Death,
        [Description("Switch Side")] SwitchSide
    }

    public enum OnRespawn {
        Nothing,
        Block,
        [Description("Switch Side")] SwitchSide
    }

    public enum OnNextRound {
        [Description("Random Side")] RandomSide,
        [Description("Switch Side")] SwitchSide
    }
}

public enum PlayerRole : byte { None, PreferHunter, PreferHider, Hunter, Hider }
public enum GameState : byte { Lobby, Setup, Hide, Hunt }
public enum CompassMode { Off, Radar, Room, Position } // TODO: i dont like the name radar
public enum TauntMode { Off, Sound, Radar, Room, Position }

public readonly record struct Ruleset(
    Rules.OnCatch hiderCatch, Rules.OnRespawn hiderRespawn,
    Rules.OnCatch hunterCatch, Rules.OnRespawn hunterRespawn,
    Rules.OnNextRound nextRound
) {
    public static readonly Ruleset hideAndSeek = new(
        Rules.OnCatch.Death, Rules.OnRespawn.Block,
        Rules.OnCatch.Nothing, Rules.OnRespawn.Nothing,
        Rules.OnNextRound.RandomSide
    );

    public static readonly Ruleset infection = new(
        Rules.OnCatch.SwitchSide, Rules.OnRespawn.SwitchSide,
        Rules.OnCatch.Nothing, Rules.OnRespawn.Nothing,
        Rules.OnNextRound.RandomSide
    );

    public static readonly Ruleset tag = new(
        Rules.OnCatch.SwitchSide, Rules.OnRespawn.Block,
        Rules.OnCatch.SwitchSide, Rules.OnRespawn.Nothing,
        Rules.OnNextRound.SwitchSide
    );

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

    public static explicit operator byte(Ruleset x) => (byte)(
        (byte)x.hiderCatch * 2 * 3 * 3 * 3 +
        (byte)x.hiderRespawn * 2 * 3 * 3 +
        (byte)x.hunterCatch * 2 * 3 +
        (byte)x.hunterRespawn * 2 +
        (byte)x.nextRound
    );

    public static explicit operator Ruleset(byte x) => new(
        (Rules.OnCatch)(x / 2 / 3 / 3 / 3 % 3),
        (Rules.OnRespawn)(x / 2 / 3 / 3 % 3),
        (Rules.OnCatch)(x / 2 / 3 % 3),
        (Rules.OnRespawn)(x / 2 % 3),
        (Rules.OnNextRound)(x % 2)
    );
}
