using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Menu;
using Menu.Remix;
using Menu.Remix.MixedUI;
using RainMeadow;
using UnityEngine;

namespace Slughunt.Menu;

public class WorldFilters : RectangularMenuObject {
    public event Action? onFiltersUpdated;

    private static Lobby lobby => OnlineManager.lobby;
    private static SlughuntGameMode gameMode => (SlughuntGameMode)lobby.gameMode;
    private static LobbyData lobbyData => lobby.GetData<LobbyData>();

    private readonly MenuTabWrapper _selectors;

    private readonly OpComboBox2 _regionSelector;

    private readonly OpComboBox2 _shelterSelector;
    private readonly List<string> _shelters = [];

    private readonly OpComboBox2 _gateSelector;
    private readonly List<string> _gates = [];

    private readonly OpComboBox2 _shortcutSelector1;
    private readonly OpComboBox2 _shortcutSelector2;
    private readonly List<string> _rooms = [];
    private readonly List<LobbyData.Shortcut> _shortcuts = [];

    public WorldFilters(global::Menu.Menu menu, MenuObject owner, Vector2 pos) : base(menu, owner, pos, new Vector2()) {
        _selectors = new MenuTabWrapper(menu, this);
        subObjects.Add(_selectors);

        _regionSelector = AddFilterComboBox(null);
        _regionSelector.OnValueChanged += (_, value, _) => {
            if (string.IsNullOrEmpty(value))
                ClearSelectors();
            else
                UpdateSelectors(value);
        };

        _shelterSelector = AddFilterComboBox(_regionSelector);
        OpSimpleButton shelterAddButton = AddFilterButton(_shelterSelector, "+");
        OpSimpleButton shelterRemoveButton = AddFilterButton(shelterAddButton, "-");
        OpSimpleButton shelterAllButton = AddFilterButton(shelterRemoveButton, "*");
        shelterAddButton.OnClick += _ => {
            if (string.IsNullOrEmpty(_shelterSelector.value))
                return;
            lobbyData.shelters.Add(_shelterSelector.value);
            lobby.NewVersion();
            onFiltersUpdated?.Invoke();
        };
        shelterRemoveButton.OnClick += _ => {
            lobbyData.shelters.Remove(_shelterSelector.value);
            lobby.NewVersion();
            onFiltersUpdated?.Invoke();
        };
        shelterAllButton.OnClick += _ => {
            bool anyAdded = _shelters.Aggregate(false, (curr, x) => lobbyData.shelters.Add(x) || curr);
            if (!anyAdded) {
                foreach (string x in _shelters)
                    lobbyData.shelters.Remove(x);
            }
            lobby.NewVersion();
            onFiltersUpdated?.Invoke();
        };

        _gateSelector = AddFilterComboBox(_shelterSelector);
        OpSimpleButton gateAddButton = AddFilterButton(_gateSelector, "+");
        OpSimpleButton gateRemoveButton = AddFilterButton(gateAddButton, "-");
        OpSimpleButton gateAllButton = AddFilterButton(gateRemoveButton, "*");
        gateAddButton.OnClick += _ => {
            if (string.IsNullOrEmpty(_gateSelector.value))
                return;
            lobbyData.lockedGates.Add(_gateSelector.value);
            lobby.NewVersion();
            onFiltersUpdated?.Invoke();
        };
        gateRemoveButton.OnClick += _ => {
            lobbyData.lockedGates.Remove(_gateSelector.value);
            lobby.NewVersion();
            onFiltersUpdated?.Invoke();
        };
        gateAllButton.OnClick += _ => {
            bool anyAdded = _gates.Aggregate(false, (curr, x) => lobbyData.lockedGates.Add(x) || curr);
            if (!anyAdded) {
                foreach (string x in _gates)
                    lobbyData.lockedGates.Remove(x);
            }
            lobby.NewVersion();
            onFiltersUpdated?.Invoke();
        };

        _shortcutSelector1 = AddFilterComboBox(_gateSelector, 0, 2);
        _shortcutSelector2 = AddFilterComboBox(_gateSelector, 1, 2);
        OpSimpleButton shortcutAddButton = AddFilterButton(_shortcutSelector2, "+");
        OpSimpleButton shortcutRemoveButton = AddFilterButton(shortcutAddButton, "-");
        OpSimpleButton shortcutAllButton = AddFilterButton(shortcutRemoveButton, "*");
        OpSimpleButton shortcutAllAllButton = AddFilterButton(shortcutAllButton, "**");
        _shortcutSelector1.OnValueChanged += (_, value, _) => {
            _shortcutSelector2.SetItems(_shortcuts
                .Where(x => x.a == value || x.b == value)
                .Select(x => new ListItem(x.a == value ? x.b : x.a) { desc = x.a == value ? x.b : x.a })
                .Distinct()
                .ToArray());
        };
        shortcutAddButton.OnClick += _ => {
            if (string.IsNullOrEmpty(_shortcutSelector1.value))
                return;
            if (string.IsNullOrEmpty(_shortcutSelector2.value))
                return;
            lobbyData.lockedShortcuts.Add(new LobbyData.Shortcut(_shortcutSelector1.value, _shortcutSelector2.value));
            lobby.NewVersion();
            onFiltersUpdated?.Invoke();
        };
        shortcutRemoveButton.OnClick += _ => {
            lobbyData.lockedShortcuts.Remove(new LobbyData.Shortcut(_shortcutSelector1.value, _shortcutSelector2.value));
            lobby.NewVersion();
            onFiltersUpdated?.Invoke();
        };
        shortcutAllButton.OnClick += _ => {
            string value1 = _shortcutSelector1.value;
            bool anyAdded = _shortcuts
                .Where(x => x.a == value1 || x.b == value1)
                .Aggregate(false, (curr, x) => lobbyData.lockedShortcuts.Add(x) || curr);
            if (!anyAdded) {
                foreach (LobbyData.Shortcut x in _shortcuts.Where(x => x.a == value1 || x.b == value1))
                    lobbyData.lockedShortcuts.Remove(x);
            }
            lobby.NewVersion();
            onFiltersUpdated?.Invoke();
        };
        shortcutAllAllButton.OnClick += _ => {
            bool anyAdded = _shortcuts.Aggregate(false, (curr, x) => lobbyData.lockedShortcuts.Add(x) || curr);
            if (!anyAdded) {
                foreach (LobbyData.Shortcut x in _shortcuts)
                    lobbyData.lockedShortcuts.Remove(x);
            }
            lobby.NewVersion();
            onFiltersUpdated?.Invoke();
        };

        size = shortcutAllAllButton.pos + shortcutAllAllButton.size;

        const float padding = 5f;
        return;

        OpComboBox2 AddFilterComboBox(UIelement? after, int index = 0, int count = 1) {
            Vector2 afterPos = after?.pos ?? new Vector2();
            Vector2 afterSize = after?.size ?? new Vector2(200f, -padding);
            OpComboBox2 comboBox = new(
                new Configurable<string?>(null),
                afterPos + new Vector2(afterSize.x * index / count + padding * 0.5f * index, -afterSize.y - padding),
                afterSize.x / count - padding * 0.5f * (count - 1),
                [new ListItem("", "------")]
            );
            _ = new UIelementWrapper(_selectors, comboBox);
            return comboBox;
        }

        OpSimpleButton AddFilterButton(UIelement after, string text) {
            OpSimpleButton button = new(
                after.pos + new Vector2(after.size.x + padding, 0f),
                new Vector2(after.size.y, after.size.y),
                text
            );
            _ = new UIelementWrapper(_selectors, button);
            return button;
        }
    }

    public void UpdateRegions() {
        _regionSelector.SetItems(Region.GetFullRegionOrder(gameMode.timeline)
            .Select((x, i) => {
                string fullName = $"{Region.GetRegionFullName(x, gameMode.character)} ({x})";
                return new ListItem(x, fullName, i) {
                    desc = fullName
                };
            })
            .ToArray());
    }

    private void UpdateSelectors(string region) {
        _shelters.Clear();
        _gates.Clear();
        _rooms.Clear();
        _shortcuts.Clear();

        WorldLoader loader = new(
            null, gameMode.character, gameMode.timeline,
            false, region, null,
            menu.manager.rainWorld.setup,
            WorldLoader.LoadingContext.FASTTRAVEL
        );
        loader.NextActivity();
        while (!loader.Finished) {
            loader.Update();
            Thread.Sleep(1);
        }
        World world = loader.ReturnWorld();

        foreach (AbstractRoom room in world.abstractRooms) {
            if (room.offScreenDen)
                continue;
            if (room.shelter)
                _shelters.Add(room.name);
            if (room.gate)
                _gates.Add(room.name);
            _rooms.Add(room.name);
            _shortcuts.AddRange(
                room.connections
                    .Where(world.IsRoomInRegion)
                    .Select(x => new LobbyData.Shortcut(room.name, world.GetAbstractRoom(x).name))
            );
        }

        _shelterSelector.SetItems(_shelters.Select(x => new ListItem(x) { desc = x }).ToArray());
        _gateSelector.SetItems(_gates.Select(x => new ListItem(x) { desc = x }).ToArray());
        _shortcutSelector1.SetItems(_rooms.Select(x => new ListItem(x) { desc = x }).ToArray());
        _shortcutSelector2.ClearItems(); // _shortcutSelector2 item list depends on the value of _shortcutSelector1
    }

    private void ClearSelectors() {
        _shelters.Clear();
        _gates.Clear();
        _rooms.Clear();
        _shortcuts.Clear();

        _shelterSelector.ClearItems();
        _gateSelector.ClearItems();
        _shortcutSelector1.ClearItems();
        _shortcutSelector2.ClearItems();
    }

    public void Show() {
        if (_selectors._tab.isInactive)
            _selectors._tab._Activate();
    }

    public void Hide() {
        if (!_selectors._tab.isInactive)
            _selectors._tab._Deactivate();
    }
}
