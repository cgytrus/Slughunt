using System;
using System.Collections.Generic;
using Menu;
using RainMeadow;
using UnityEngine;

namespace Slughunt.Menu;

public class PlayerCards : RectangularMenuObject {
    private const int MaxShownPlayers = 8;
    private readonly List<PlayerCard> _cards = [];
    private int _playersScroll;

    public PlayerCards(global::Menu.Menu menu, MenuObject owner, Vector2 pos) : base(
        menu, owner, pos,
        new Vector2(PlayerCard.Width, MaxShownPlayers * (PlayerCard.Height + PlayerCard.Padding))
    ) {
        EventfulScrollButton playersUpButton = new(
            menu, this,
            new Vector2(PlayerCard.Width / 2f - 50f, 0f),
            0, 100f
        );
        playersUpButton.OnClick += _ => _playersScroll--;
        subObjects.Add(playersUpButton);

        EventfulScrollButton playersDownButton = new(
            menu, this,
            new Vector2(
                playersUpButton.pos.x,
                playersUpButton.pos.y - 10f - size.y - 10f - 24f
            ),
            2, 100f
        );
        playersDownButton.OnClick += _ => _playersScroll++;
        subObjects.Add(playersDownButton);
    }

    public void UpdatePlayerCards() {
        foreach (PlayerCard card in _cards) {
            RemoveSubObject(card);
            card.RemoveSprites();
        }
        _cards.Clear();

        foreach (OnlinePlayer player in OnlineManager.players) {
            PlayerCard card = new(menu, this, new Vector2(0f, 0f), player);
            _cards.Add(card);
            subObjects.Add(card);
        }
    }

    public override void GrafUpdate(float timeStacker) {
        base.GrafUpdate(timeStacker);

        _playersScroll = Mathf.Clamp(
            _playersScroll, Math.Min(MaxShownPlayers - _cards.Count, 0), 0
        );

        for (int i = 0; i < _cards.Count; i++) {
            PlayerCard card = _cards[i];
            int index = i + _playersScroll;
            card.pos.y = index switch {
                < 0 => 768f + PlayerCard.Height * 2f - pos.y,
                >= MaxShownPlayers => -PlayerCard.Height * 2f - pos.y,
                _ => -10f - index * (PlayerCard.Height + PlayerCard.Padding)
            };
            card.lastPos.y = card.pos.y;
        }
    }
}
