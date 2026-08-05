using System;
using HUD;
using RainMeadow;
using RWCustom;

namespace Slughunt.HUD;

public class SlughuntInfo : HudPart {
    private static Lobby lobby => OnlineManager.lobby;
    private static LobbyData lobbyData => lobby.GetData<LobbyData>();
    private static PlayerData playerData => lobbyData.GetPlayerData(OnlineManager.mePlayer);

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
        switch (lobbyData.state) {
            case Rules.GameState.Setup:
                _stateLabel.text = "Waiting for hiders...";
                _scoreLabel.isVisible = false;
                break;
            case Rules.GameState.Hide:
                _stateLabel.text = $"Hide: {FormatTime(lobbyData.stateTime.time - lobbyData.hideTime, "", "-")}";
                _scoreLabel.isVisible = false;
                break;
            case Rules.GameState.Hunt:
                _stateLabel.text = $"Hunt: {FormatTime(lobbyData.stateTime.time)}";
                _scoreLabel.isVisible = playerData.role is Rules.Role.Participant;
                break;
        }

        if (!_scoreLabel.isVisible || playerData.dead)
            return;

        string roleTime = FormatTime(playerData.unsavedTime.time);
        string totalTime = FormatTime(playerData.currentTotalTime);
        _scoreLabel.text = $"""
            {(playerData.role is Rules.Role.Hunter ? "Hunting" : "Hiding")} for {roleTime}
            Total: {playerData.totalScore} / {totalTime}
            """;
    }

    public override void ClearSprites() {
        base.ClearSprites();
        _stateLabel.RemoveFromContainer();
        _scoreLabel.RemoveFromContainer();
    }

    public static string FormatTime(TimeSpan time, string neg = "-", string pos = "") {
        int seconds = Math.Abs((int)Math.Floor(time.TotalSeconds));

        int minutes = seconds / 60;
        seconds %= 60;

        int hours = minutes / 60;
        minutes %= 60;

        return hours == 0 ?
            $"{(time.Ticks < 0 ? neg : pos)}{minutes}:{seconds:D2}" :
            $"{(time.Ticks < 0 ? neg : pos)}{hours}:{minutes:D2}:{seconds:D2}";
    }
}
