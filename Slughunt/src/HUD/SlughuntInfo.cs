using System;
using HUD;
using MoreSlugcats;
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
                _stateLabel.text = $"Hide: {SpeedRunTimer.TimeFormat(lobbyData.hideTime - time)}";
                _scoreLabel.isVisible = false;
                return;
            case GameState.Hunt:
                _stateLabel.text = $"Hunt: {SpeedRunTimer.TimeFormat(time)}";
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

        TimeSpan roleTime = TimeSpan.FromSeconds(playerData.dead ? 0d : playerData.currentStateFor / fps);
        TimeSpan totalTime = TimeSpan.FromSeconds(playerData.totalTime / fps) + (hunter ? -roleTime : roleTime);

        _scoreLabel.text = $"""
            {(hunter ? "Hunting" : "Hiding")} for {SpeedRunTimer.TimeFormat(roleTime)}
            Total: {playerData.totalScore} / {SpeedRunTimer.TimeFormat(totalTime)}
            """;
    }

    public override void ClearSprites() {
        base.ClearSprites();
        _stateLabel.RemoveFromContainer();
        _scoreLabel.RemoveFromContainer();
    }
}
