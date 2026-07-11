using System.Text;
using Menu;
using RainMeadow;
using UnityEngine;

namespace Slughunt.Menu;

public class PlayerCard : PositionedMenuObject {
    public const float Width = 256f;
    public const float Height = 20f;
    public const float Padding = 2f;

    private readonly OnlinePlayer _player;
    private static Lobby lobby => OnlineManager.lobby;
    private static SlughuntGameMode gameMode => (SlughuntGameMode)lobby.gameMode;
    private static LobbyData lobbyData => lobby.GetData<LobbyData>();
    private PlayerData data => lobbyData.GetPlayerData(_player);

    private readonly string _name;

    private readonly ProperlyAlignedMenuLabel _label;

    public PlayerCard(global::Menu.Menu menu, MenuObject owner, Vector2 pos, OnlinePlayer player) :
        base(menu, owner, pos) {
        _player = player;

        // DisplayName may generate a new time every time its accessed so cant do it in update
        _name = player.id.DisplayName;

        _label = new ProperlyAlignedMenuLabel(menu, this, "balls", new Vector2(0f, 0f),
            new Vector2(Width, Height), false) {
            label = {
                alignment = FLabelAlignment.Left,
                anchorX = 0.0f,
                anchorY = 0.5f
            }
        };
        subObjects.Add(_label);
    }

    private readonly StringBuilder _text = new();
    public override void Update() {
        base.Update();

        _text.Clear();
        if (_player.isMe)
            _text.Append("> ");
        _text.Append(_name);
        if (lobby.owner == _player)
            _text.Append(" ^");
        if (lobby.clientSettings.TryGetValue(_player, out ClientSettings settings) && settings.inGame)
            _text.Append(" (in game)");
        _label.label.text = _text.ToString();

        _label.label.color = data.role switch {
            PlayerRole.None => global::Menu.Menu.MenuRGB(data.ready ? global::Menu.Menu.MenuColors.White : global::Menu.Menu.MenuColors.MediumGrey),
            PlayerRole.PreferHunter => new HSLColor(12f / 360f, 0.65f, data.ready ? 0.67f : 0.5f).rgb,
            PlayerRole.PreferHider => new HSLColor(0.6f, 0.65f, data.ready ? 0.67f : 0.5f).rgb,
            PlayerRole.Hunter => new HSLColor(12f / 360f, 0.65f, 0.67f).rgb,
            PlayerRole.Hider => new HSLColor(0.6f, 0.65f, 0.67f).rgb,
            _ => global::Menu.Menu.MenuRGB(global::Menu.Menu.MenuColors.White)
        };
    }
}
