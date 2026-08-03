using System;
using System.Text;
using Menu;
using RainMeadow;
using Slughunt.HUD;
using UnityEngine;

namespace Slughunt.Menu;

public class PlayerCard : PositionedMenuObject {
    public const float Width = 300f;
    public const float Height = 16f * 3f;
    public const float Padding = 5f;

    private readonly OnlinePlayer _player;
    private static Lobby lobby => OnlineManager.lobby;
    private static LobbyData lobbyData => lobby.GetData<LobbyData>();
    private PlayerData data => lobbyData.GetPlayerData(_player);

    private readonly string _name;

    private readonly ProperlyAlignedMenuLabel _nameLabel;
    private readonly ProperlyAlignedMenuLabel _pingLabel;
    private readonly ProperlyAlignedMenuLabel _hostLabel;

    private readonly ProperlyAlignedMenuLabel _totalScoreLabel;
    private readonly ProperlyAlignedMenuLabel _hunterScoreLabel;
    private readonly ProperlyAlignedMenuLabel _hiderScoreLabel;

    public PlayerCard(global::Menu.Menu menu, MenuObject owner, Vector2 pos, OnlinePlayer player) :
        base(menu, owner, pos) {
        _player = player;

        // DisplayName may generate a new time every time its accessed so cant do it in update
        _name = player.id.DisplayName;

        _nameLabel = new ProperlyAlignedMenuLabel(menu, this, "cock", new Vector2(0f, 0f),
            new Vector2(Width, Height), false) {
            label = {
                alignment = FLabelAlignment.Left,
                anchorX = 0.0f,
                anchorY = 1.0f
            }
        };
        subObjects.Add(_nameLabel);

        _pingLabel = new ProperlyAlignedMenuLabel(menu, this, "cock", new Vector2(0f, -16f),
            new Vector2(Width, Height), false) {
            label = {
                alignment = FLabelAlignment.Left,
                anchorX = 0.0f,
                anchorY = 1.0f,
                color = global::Menu.Menu.MenuRGB(global::Menu.Menu.MenuColors.MediumGrey)
            }
        };
        subObjects.Add(_pingLabel);

        _hostLabel = new ProperlyAlignedMenuLabel(menu, this, "(host)", new Vector2(0f, -32f),
            new Vector2(Width, Height), false) {
            label = {
                alignment = FLabelAlignment.Left,
                anchorX = 0.0f,
                anchorY = 1.0f,
                color = global::Menu.Menu.MenuRGB(global::Menu.Menu.MenuColors.SaturatedGold)
            }
        };
        subObjects.Add(_hostLabel);

        _totalScoreLabel = new ProperlyAlignedMenuLabel(menu, this, "balls", new Vector2(Width, 0f),
            new Vector2(Width, Height), false) {
            label = {
                alignment = FLabelAlignment.Right,
                anchorX = 1.0f,
                anchorY = 1.0f,
                color = global::Menu.Menu.MenuRGB(global::Menu.Menu.MenuColors.MediumGrey)
            }
        };
        subObjects.Add(_totalScoreLabel);

        _hunterScoreLabel = new ProperlyAlignedMenuLabel(menu, this, "balls", new Vector2(Width, -16f),
            new Vector2(Width, Height), false) {
            label = {
                alignment = FLabelAlignment.Right,
                anchorX = 1.0f,
                anchorY = 1.0f,
                color = new HSLColor(12f / 360f, 0.5f, 0.5f).rgb
            }
        };
        subObjects.Add(_hunterScoreLabel);

        _hiderScoreLabel = new ProperlyAlignedMenuLabel(menu, this, "balls", new Vector2(Width, -32f),
            new Vector2(Width, Height), false) {
            label = {
                alignment = FLabelAlignment.Right,
                anchorX = 1.0f,
                anchorY = 1.0f,
                color = new HSLColor(0.6f, 0.5f, 0.5f).rgb
            }
        };
        subObjects.Add(_hiderScoreLabel);
    }

    private readonly StringBuilder _text = new();
    public override void Update() {
        base.Update();

        _text.Clear();
        if (_player.isMe)
            _text.Append("> ");
        _text.Append(_name);
        if (lobby.clientSettings.TryGetValue(_player, out ClientSettings settings) && settings.inGame)
            _text.Append(" (in game)");
        _nameLabel.label.text = _text.ToString();
        _nameLabel.label.color = data.role switch {
            Rules.Role.None => global::Menu.Menu.MenuRGB(data.ready ? global::Menu.Menu.MenuColors.White : global::Menu.Menu.MenuColors.MediumGrey),
            Rules.Role.PreferHunter => new HSLColor(12f / 360f, 0.65f, data.ready ? 0.67f : 0.5f).rgb,
            Rules.Role.PreferHider => new HSLColor(0.6f, 0.65f, data.ready ? 0.67f : 0.5f).rgb,
            Rules.Role.Hunter => new HSLColor(12f / 360f, 0.65f, 0.67f).rgb,
            Rules.Role.Hider => new HSLColor(0.6f, 0.65f, 0.67f).rgb,
            _ => global::Menu.Menu.MenuRGB(global::Menu.Menu.MenuColors.White)
        };

        _pingLabel.text = $"ping: {Math.Max(1, _player.ping - 16)}ms";

        _hostLabel.label.isVisible = lobby.owner == _player;

        double fps = OnlineManager.instance.framesPerSecond;
        string totalTime = SlughuntInfo.FormatTime(TimeSpan.FromSeconds(data.totalTime / fps));
        string timeAsHunter = SlughuntInfo.FormatTime(TimeSpan.FromSeconds(data.timeAsHunter / fps), "+", "-");
        string timeAsHider = SlughuntInfo.FormatTime(TimeSpan.FromSeconds(data.timeAsHider / fps), "-", "+");
        _totalScoreLabel.label.text = $"total: {data.totalScore} / {totalTime}";
        _hunterScoreLabel.label.text = $"hunter: +{data.caughtAsHunter} / {timeAsHunter}";
        _hiderScoreLabel.label.text = $"hider: -{data.caughtAsHider} / {timeAsHider}";
    }
}
