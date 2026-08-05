using HUD;
using RainMeadow;
using RWCustom;
using Slughunt.Utils;

namespace Slughunt.HUD;

public class GameInfoPart : HudPart {
    private static Lobby lobby => OnlineManager.lobby;
    private static LobbyData lobbyData => lobby.GetData<LobbyData>();
    private static PlayerData playerData => lobbyData.GetPlayerData(OnlineManager.mePlayer);

    private readonly FLabel _stateLabel;
    private readonly FLabel _scoreLabel;

    public GameInfoPart(global::HUD.HUD hud, FContainer container) : base(hud) {
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

    private OnlineTimeSpan _lastUnpausedUnsavedTime;

    public override void Update() {
        switch (lobbyData.state) {
            case Rules.GameState.Setup:
                _stateLabel.text = "Waiting for hiders...";
                _scoreLabel.isVisible = false;
                break;
            case Rules.GameState.Hide:
                _stateLabel.text = $"Hide: {Epic.FormatTime(lobbyData.stateTime.time - lobbyData.hideTime, "", "-")}";
                _scoreLabel.isVisible = false;
                break;
            case Rules.GameState.Hunt:
                _stateLabel.text = $"Hunt: {Epic.FormatTime(lobbyData.stateTime.time)}";
                _scoreLabel.isVisible = playerData.role is Rules.Role.Participant;
                break;
        }

        if (playerData.unsavedTime.isRunning)
            _lastUnpausedUnsavedTime = playerData.unsavedTime.time;

        if (!_scoreLabel.isVisible)
            return;

        string roleTime = Epic.FormatTime(_lastUnpausedUnsavedTime);
        string totalTime = Epic.FormatTime(playerData.currentTotalTime);
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
}
