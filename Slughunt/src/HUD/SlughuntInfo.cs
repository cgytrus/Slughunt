using System;
using HUD;
using RainMeadow;
using RWCustom;

namespace Slughunt.HUD;

public class SlughuntInfo : HudPart {
    private static Lobby lobby => OnlineManager.lobby;
    private static SlughuntGameMode gameMode => (SlughuntGameMode)lobby.gameMode;
    private static LobbyData lobbyData => lobby.GetData<LobbyData>();
    private static PlayerData playerData => gameMode.playerData;

    private readonly FLabel _stateLabel;
    private readonly FLabel _scoreLabel;

    public SlughuntInfo(global::HUD.HUD hud, FContainer container) : base(hud) {
        _stateLabel = new FLabel(Custom.GetDisplayFont(), "paws") {
            x = 0.01f + 20f,
            y = 0.01f + hud.rainWorld.options.ScreenSize.y - 20f,
            alignment = FLabelAlignment.Left,
            anchorX = 0.0f,
            anchorY = 1.0f
        };
        container.AddChild(_stateLabel);

        _scoreLabel = new FLabel(Custom.GetFont(), "swap") {
            x = _stateLabel.x + 10f,
            y = _stateLabel.y - 30f,
            alignment = FLabelAlignment.Left,
            anchorX = 0.0f,
            anchorY = 1.0f
        };
        container.AddChild(_scoreLabel);
    }

    public override void Update() {
        double fps = OnlineManager.instance.framesPerSecond;

        TimeSpan time = TimeSpan.FromSeconds((lobby.owner.tick - lobbyData.switchedStateAt) / fps);
        switch (lobbyData.state) {
            case GameState.Setup:
                _stateLabel.text = "Waiting for hiders...";
                _scoreLabel.isVisible = false;
                return;
            case GameState.Hide:
                _stateLabel.text = $"Hide: {FormatTime(time - lobbyData.hideTime, "", "-")}";
                _scoreLabel.isVisible = false;
                return;
            case GameState.Hunt:
                _stateLabel.text = $"Hunt: {FormatTime(time)}";
                break;
            case GameState.Lobby:
            default:
                _stateLabel.text = "what";
                break;
        }

        bool hunter = playerData.role == PlayerRole.Hunter;
        if (!hunter && playerData.role != PlayerRole.Hider) {
            _scoreLabel.isVisible = false;
            return;
        }
        _scoreLabel.isVisible = true;

        TimeSpan roleTime = TimeSpan.FromSeconds(playerData.unsavedTimeAsCurrentRole / fps);
        TimeSpan totalTime = TimeSpan.FromSeconds(playerData.currentTotalTime / fps);

        _scoreLabel.text = $"""
            {(hunter ? "Hunting" : "Hiding")} for {FormatTime(roleTime)}
            Total: {playerData.totalScore} / {FormatTime(totalTime)}
            """;
    }

    public override void ClearSprites() {
        base.ClearSprites();
        _stateLabel.RemoveFromContainer();
        _scoreLabel.RemoveFromContainer();
    }

    public static string FormatTime(TimeSpan time, string neg = "-", string pos = "") {
        bool negative = time.Ticks < 0;
        int seconds = Math.Abs((int)Math.Floor(time.TotalSeconds));
        int minutes = seconds / 60;
        seconds = seconds % 60;
        int hours = minutes / 60;
        if (hours == 0)
            return $"{(negative ? neg : pos)}{minutes}:{seconds:D2}";
        minutes = minutes % 60;
        return $"{(negative ? neg : pos)}{hours}:{minutes:D2}:{seconds:D2}";
    }
}
