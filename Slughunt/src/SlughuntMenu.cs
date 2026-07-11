using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Menu;
using Menu.Remix;
using Menu.Remix.MixedUI;
using MoreSlugcats;
using RainMeadow;
using RainMeadow.UI.Components;
using UnityEngine;

namespace Slughunt;

public class SlughuntMenu : SmartMenu {
    public static ProcessManager.ProcessID id { get; } = new($"{nameof(SlughuntMenu)}+{Plugin.Id}", true);

    // TODO: changing scene based on the region selector would b so cool!
    public override MenuScene.SceneID GetScene => MenuScene.SceneID.Landscape_CC;

    private static Lobby lobby => OnlineManager.lobby;
    private static SlughuntGameMode gameMode => (SlughuntGameMode)lobby.gameMode;
    private static LobbyData lobbyData => lobby.GetData<LobbyData>();
    private static PlayerData playerData => gameMode.playerData;

    private readonly SimplerButton _readyButton;
    private readonly SimplerButton _hunterButton;
    private readonly OpSimpleButton _startButton;
    private readonly OpHoldButton _forceStartButton;

    private readonly SimplerCheckbox _spawnCreatures;
    private readonly OpComboBox2 _campaignSelector;

    private readonly MenuTabWrapper _worldFiltersSelectors;

    private readonly OpComboBox2 _regionSelector;

    private readonly OpComboBox2 _shelterSelector;
    private readonly List<string> _shelters = [];

    private readonly OpComboBox2 _gateSelector;
    private readonly List<string> _gates = [];

    private readonly OpComboBox2 _shortcutSelector1;
    private readonly OpComboBox2 _shortcutSelector2;
    private readonly List<string> _rooms = [];
    private readonly List<LobbyData.Shortcut> _shortcuts = [];

    private readonly ProperlyAlignedMenuLabel _worldFiltersLabel;

    private class PlayerCard : PositionedMenuObject {
        public const float Width = 256f;
        public const float Height = 20f;
        public const float Padding = 2f;

        private readonly OnlinePlayer _player;
        private bool hasSettings => lobby.clientSettings.ContainsKey(_player);
        private ClientSettings settings => lobby.clientSettings[_player];
        private PlayerData data => lobbyData.GetPlayerData(_player);

        private readonly string _name;

        private readonly ProperlyAlignedMenuLabel _label;

        public PlayerCard(Menu.Menu menu, MenuObject owner, Vector2 pos, OnlinePlayer player) :
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
            if (!hasSettings) {
                _text.Append(" ...");
                _label.label.text = _text.ToString();
                return;
            }
            if (settings.inGame) {
                _text.Append(" (in game)");
            }
            _label.label.text = _text.ToString();

            HSLColor color;
            switch (data.role) {
                case PlayerRole.None:
                    color = MenuColor(data.ready ? MenuColors.White : MenuColors.MediumGrey);
                    break;
                case PlayerRole.PreferHunter:
                    color = MenuColor(MenuColors.DarkRed);
                    color.lightness = data.ready ? 0.67f : 0.5f;
                    break;
                case PlayerRole.PreferHider:
                    color = new HSLColor(0.6f, 0.65f, data.ready ? 0.67f : 0.5f);
                    break;
                case PlayerRole.Hunter:
                    color = MenuColor(MenuColors.DarkRed);
                    color.lightness = 0.67f;
                    break;
                case PlayerRole.Hider:
                    color = new HSLColor(0.6f, 0.65f, 0.67f);
                    break;
                default:
                    color = MenuColor(MenuColors.White);
                    break;
            }
            _label.label.color = color.rgb;
        }
    }

    private const int MaxShownPlayers = 20;
    private static readonly Vector2 playersPos = new(100f, 680f - 24f);
    private readonly List<PlayerCard> _players = [];
    private int _playersScroll;

    public SlughuntMenu(ProcessManager manager) : base(manager, id) {
        backTarget = RainMeadow.RainMeadow.Ext_ProcessID.LobbySelectMenu;

        _readyButton = new SimplerButton(
            this, mainPage,
            "stupid",
            new Vector2(1056f, 50f), new Vector2(110f, 30f)
        );
        _readyButton.OnClick += _ => {
            lobby.owner.InvokeRPC(SwitchReady);
        };
        mainPage.subObjects.Add(_readyButton);

        _hunterButton = new SimplerButton(
            this, mainPage,
            "penis",
            _readyButton.pos + new Vector2(0f, _readyButton.size.y + 10f), new Vector2(110f, 30f)
        );
        _hunterButton.OnClick += _ => {
            lobby.owner.InvokeRPC(SwitchSide);
        };
        mainPage.subObjects.Add(_hunterButton);

        _startButton = new OpSimpleButton(
            new Vector2(1056f - _readyButton.size.x - 10f, 50f), new Vector2(110f, 30f),
            Translate("START")
        );
        _startButton.OnClick += _ => gameMode.StartGame();
        _ = new UIelementWrapper(tabWrapper, _startButton);

        _forceStartButton = new OpHoldButton(
            new Vector2(1056f - _readyButton.size.x - 10f, 50f), new Vector2(110f, 30f),
            Translate("FORCE START")
        ) {
            description = "Only ready players will enter the game"
        };
        _forceStartButton.OnPressDone += _ => gameMode.StartGame();
        _ = new UIelementWrapper(tabWrapper, _forceStartButton);

        EventfulScrollButton playersUpButton = new(
            this, mainPage,
            playersPos + new Vector2(PlayerCard.Width / 2f - 50f, 0f),
            0, 100f
        );
        playersUpButton.OnClick += _ => _playersScroll--;
        mainPage.subObjects.Add(playersUpButton);

        EventfulScrollButton playersDownButton = new(
            this, mainPage,
            new Vector2(
                playersUpButton.pos.x,
                playersUpButton.pos.y - 10f - MaxShownPlayers * (PlayerCard.Height + PlayerCard.Padding) - 10f - 24f
            ),
            2, 100f
        );
        playersDownButton.OnClick += _ => _playersScroll++;
        mainPage.subObjects.Add(playersDownButton);

        UpdatePlayerCards();
        MatchmakingManager.OnPlayerListReceived += OnPlayerListReceived;

        gameMode.avatarSettings.currentColors = manager.rainWorld.progression.GetCustomColors(SlughuntGameMode.save);
        SimplerButton colorsButton = new(
            this, mainPage,
            "COLORS",
            playersPos + new Vector2(PlayerCard.Width + 10f, -30f), new Vector2(110f, 30f)
        );
        colorsButton.OnClick += _ => {
            ColorSlugcatDialog colorDialog = new(manager, SlughuntGameMode.save, () => {
                gameMode.avatarSettings.currentColors =
                    manager.rainWorld.progression.GetCustomColors(SlughuntGameMode.save);
            });
            manager.ShowDialog(colorDialog);
        };
        mainPage.subObjects.Add(colorsButton);

        _spawnCreatures = new SimplerCheckbox(
            this, mainPage, colorsButton.pos - new Vector2(0f, 40f), 42f, "Spawn creatures", true
        );
        _spawnCreatures.OnClick += value => {
            if (!lobby.isOwner)
                return;
            lobbyData.spawnCreatures = value;
            lobby.NewVersion();
        };
        mainPage.subObjects.Add(_spawnCreatures);
        _spawnCreatures.Checked = lobbyData.spawnCreatures;

        _campaignSelector = new OpComboBox2(
            new Configurable<SlugcatStats.Name>(lobbyData.campaign),
            _spawnCreatures.pos - new Vector2(0f, _spawnCreatures.size.y + 10f),
            200f, new[] {
                    SlugcatStats.Name.White,
                    SlugcatStats.Name.Yellow,
                    SlugcatStats.Name.Red,
                    MoreSlugcatsEnums.SlugcatStatsName.Rivulet,
                    MoreSlugcatsEnums.SlugcatStatsName.Artificer,
                    MoreSlugcatsEnums.SlugcatStatsName.Spear,
                    MoreSlugcatsEnums.SlugcatStatsName.Gourmand,
                    MoreSlugcatsEnums.SlugcatStatsName.Saint
                }
                .Where(x => ModManager.MSC || !SlugcatStats.IsSlugcatFromMSC(x))
                .Select((x, i) => new ListItem(x.value, SlugcatStats.getSlugcatName(x), i))
                .ToList()
        ) {
            colorEdge = MenuColorEffect.rgbWhite
        };
        _campaignSelector.OnValueChanged += (_, value, _) => {
            if (!lobby.isOwner)
                return;
            lobbyData.campaign = new SlugcatStats.Name(value);
            lobby.NewVersion();
            UpdateRegionSelector();
        };
        _ = new UIelementWrapper(tabWrapper, _campaignSelector);

        _worldFiltersSelectors = new MenuTabWrapper(this, mainPage);
        mainPage.subObjects.Add(_worldFiltersSelectors);

        _regionSelector = AddFilterComboBox(_campaignSelector);
        _regionSelector.OnValueChanged += (_, value, _) => {
            if (string.IsNullOrEmpty(value))
                ClearFilterSelectors();
            else
                UpdateFilterSelectors(value);
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
            UpdateFiltersLabel();
        };
        shelterRemoveButton.OnClick += _ => {
            lobbyData.shelters.Remove(_shelterSelector.value);
            lobby.NewVersion();
            UpdateFiltersLabel();
        };
        shelterAllButton.OnClick += _ => {
            bool anyAdded = _shelters.Aggregate(false, (curr, x) => lobbyData.shelters.Add(x) || curr);
            if (!anyAdded) {
                foreach (string x in _shelters)
                    lobbyData.shelters.Remove(x);
            }
            lobby.NewVersion();
            UpdateFiltersLabel();
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
            UpdateFiltersLabel();
        };
        gateRemoveButton.OnClick += _ => {
            lobbyData.lockedGates.Remove(_gateSelector.value);
            lobby.NewVersion();
            UpdateFiltersLabel();
        };
        gateAllButton.OnClick += _ => {
            bool anyAdded = _gates.Aggregate(false, (curr, x) => lobbyData.lockedGates.Add(x) || curr);
            if (!anyAdded) {
                foreach (string x in _gates)
                    lobbyData.lockedGates.Remove(x);
            }
            lobby.NewVersion();
            UpdateFiltersLabel();
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
            UpdateFiltersLabel();
        };
        shortcutRemoveButton.OnClick += _ => {
            lobbyData.lockedShortcuts.Remove(new LobbyData.Shortcut(_shortcutSelector1.value, _shortcutSelector2.value));
            lobby.NewVersion();
            UpdateFiltersLabel();
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
            UpdateFiltersLabel();
        };
        shortcutAllAllButton.OnClick += _ => {
            bool anyAdded = _shortcuts.Aggregate(false, (curr, x) => lobbyData.lockedShortcuts.Add(x) || curr);
            if (!anyAdded) {
                foreach (LobbyData.Shortcut x in _shortcuts)
                    lobbyData.lockedShortcuts.Remove(x);
            }
            lobby.NewVersion();
            UpdateFiltersLabel();
        };

        _worldFiltersLabel = new ProperlyAlignedMenuLabel(
            this, mainPage, "penis",
            new Vector2(
                shortcutAllAllButton.pos.x + shortcutAllAllButton.size.x + 50f,
                colorsButton.pos.y + colorsButton.size.y
            ),
            new Vector2(200f, 400f), false
        ) {
            label = {
                anchorY = 1.0f
            }
        };
        mainPage.subObjects.Add(_worldFiltersLabel);

        UpdateRegionSelector();
        return;

        OpComboBox2 AddFilterComboBox(UIelement after, int index = 0, int count = 1) {
            OpComboBox2 comboBox = new(
                new Configurable<string?>(null),
                after.pos + new Vector2(after.size.x * index / count + 5f * index, -after.size.y - 10f),
                after.size.x / count - 5f * (count - 1),
                [new ListItem("", "------")]
            );
            _ = new UIelementWrapper(_worldFiltersSelectors, comboBox);
            return comboBox;
        }

        OpSimpleButton AddFilterButton(UIelement after, string text) {
            OpSimpleButton button = new(
                after.pos + new Vector2(after.size.x + 10f, 0f),
                new Vector2(after.size.y, after.size.y),
                text
            );
            _ = new UIelementWrapper(_worldFiltersSelectors, button);
            return button;
        }
    }

    public override void ShutDownProcess() {
        base.ShutDownProcess();
        MatchmakingManager.OnPlayerListReceived -= OnPlayerListReceived;
        if (manager.upcomingProcess != ProcessManager.ProcessID.Game)
            OnlineManager.LeaveLobby();
        manager.dialogStack.Clear();
        if (manager.dialog is null)
            return;
        manager.sideProcesses.Remove(manager.dialog);
        manager.dialog.ShutDownProcess();
        manager.dialog = null;
    }

    public override void GrafUpdate(float timeStacker) {
        base.GrafUpdate(timeStacker);

        _playersScroll = Mathf.Clamp(
            _playersScroll, Math.Min(MaxShownPlayers - _players.Count, 0), 0
        );

        for (int i = 0; i < _players.Count; i++) {
            PlayerCard card = _players[i];
            int index = i + _playersScroll;
            card.pos.y = index switch {
                < 0 => 768f + PlayerCard.Height * 2f,
                >= MaxShownPlayers => -PlayerCard.Height * 2f,
                _ => playersPos.y - 10f - index * (PlayerCard.Height + PlayerCard.Padding)
            };
            card.lastPos.y = card.pos.y;
        }
    }

    public override void Update() {
        base.Update();

        if (lobby.isOwner)
            OwnerUpdate();
        else
            OtherUpdate();

        _hunterButton.menuLabel.text = Translate(playerData.role switch {
            PlayerRole.None => "PREFER: NEITHER",
            PlayerRole.PreferHunter => "PREFER: HUNTER",
            PlayerRole.PreferHider => "PREFER: HIDER",
            _ => "what"
        });

        if (lobbyData.state == GameState.Lobby) {
            _hunterButton.inactive = lobbyData is { allowHunterPreference: false, allowHiderPreference: false };

            _readyButton.inactive = false;
            _readyButton.menuLabel.text = Translate(playerData.ready ? "NOT READY" : "READY");
        }
        else {
            _hunterButton.inactive = true;

            _readyButton.inactive = !lobbyData.endless;
            _readyButton.menuLabel.text = Translate("ENTER");
            _readyButton.Description = lobbyData.endless ? "" : Translate("Wait for the current round to finish");
        }
    }

    private void OwnerUpdate() {
        if (OnlineManager.players.All(x => lobbyData.GetPlayerData(x).ready)) {
            _startButton.Show();
            _forceStartButton.Hide();
        }
        else {
            _startButton.Hide();
            _forceStartButton.Show();
        }
        _startButton.greyedOut = !playerData.ready || OnlineManager.players.Count < 2;
        _forceStartButton.greyedOut = _startButton.greyedOut;

        _spawnCreatures.inactive = false;
        _campaignSelector.greyedOut = false;

        if (_worldFiltersSelectors._tab.isInactive)
            _worldFiltersSelectors._tab._Activate();
    }

    private void OtherUpdate() {
        _startButton.Hide();
        _forceStartButton.Hide();
        if (!_worldFiltersSelectors._tab.isInactive)
            _worldFiltersSelectors._tab._Deactivate();

        _spawnCreatures.inactive = true;
        _spawnCreatures.Checked = lobbyData.spawnCreatures;

        _campaignSelector.greyedOut = true;
        _campaignSelector.value = lobbyData.campaign.value;

        // if we are the owner this is updated by our own ui interactions
        UpdateFiltersLabel();
    }

    private readonly StringBuilder _filtersLabelBuilder = new();
    private void UpdateFiltersLabel() {
        _filtersLabelBuilder.Clear();
        AppendFiltersSet("Selected shelters:", lobbyData.shelters, IsRoomInRegion);
        AppendFiltersSet("Locked gates:", lobbyData.lockedGates, IsRoomInRegion);
        AppendFiltersSet("Locked shortcuts:", lobbyData.lockedShortcuts, IsShortcutInRegion);
        _worldFiltersLabel.label.text = _filtersLabelBuilder.ToString();
    }

    private readonly List<Region> _orderedRegions = Region.GetFullRegionOrder(null)
        .Join(Region.LoadAllRegions(null, null), x => x, x => x.name, (_, x) => x)
        .ToList();

    private readonly HashSet<string> _leftoverStrings = [];
    private void AppendFiltersSet<TItem>(string title, HashSet<TItem> set, Func<TItem, Region, bool> isItemInRegion)
        where TItem : notnull {
        if (set.Count == 0)
            return;
        _filtersLabelBuilder.AppendLine(title);
        _leftoverStrings.Clear();
        foreach (TItem item in set)
            _leftoverStrings.Add(item.ToString());
        foreach (Region region in _orderedRegions) {
            bool appendedRegion = false;
            foreach (string item in set.Where(x => isItemInRegion(x, region)).Select(x => x.ToString())) {
                if (!appendedRegion) {
                    _filtersLabelBuilder.Append("  ");
                    _filtersLabelBuilder.Append(Region.GetRegionFullName(region.name, gameMode.character));
                    _filtersLabelBuilder.Append(" (");
                    _filtersLabelBuilder.Append(region.name);
                    _filtersLabelBuilder.AppendLine("):");
                    appendedRegion = true;
                }
                _filtersLabelBuilder.Append("    ");
                _filtersLabelBuilder.AppendLine(item);
                _leftoverStrings.Remove(item);
            }
        }
        foreach (string x in _leftoverStrings) {
            _filtersLabelBuilder.Append("  ");
            _filtersLabelBuilder.AppendLine(x);
        }
        _filtersLabelBuilder.AppendLine();
    }

    // TODO: better gate checking
    private static bool IsRoomInRegion(string room, Region region) {
        if (RainWorld.roomNameToIndex.TryGetValue(room, out int index) && region.IsRoomInRegion(index))
            return true;
        if (!CmpOrd(room, 0, "GATE_"))
            return false;
        return CmpOrdPostfix(room, "GATE_".Length, region.name, '_') ||
            CmpOrdPrefix(room, room.Length - region.name.Length - 1, '_', region.name);
    }
    private static bool CmpOrd(string a, int indexA, string b) =>
        string.CompareOrdinal(a, indexA, b, 0, b.Length) == 0;
    private static bool CmpOrdPostfix(string a, int indexA, string b, char c) =>
        indexA + b.Length < a.Length && CmpOrd(a, indexA, b) && a[indexA + b.Length] == c;
    private static bool CmpOrdPrefix(string a, int indexA, char c, string b) =>
        indexA >= 0 && CmpOrd(a, indexA + 1, b) && a[indexA] == c;

    private static bool IsShortcutInRegion(LobbyData.Shortcut shortcut, Region region) =>
        IsRoomInRegion(shortcut.a, region) || IsRoomInRegion(shortcut.b, region);

    private void UpdateRegionSelector() {
        UpdateFiltersLabel();
        _regionSelector.SetItems(Region.GetFullRegionOrder(gameMode.timeline)
            .Select((x, i) => {
                string fullName = $"{Region.GetRegionFullName(x, gameMode.character)} ({x})";
                return new ListItem(x, fullName, i) {
                    desc = fullName
                };
            })
            .ToArray());
    }

    private void UpdateFilterSelectors(string region) {
        _shelters.Clear();
        _gates.Clear();
        _rooms.Clear();
        _shortcuts.Clear();

        WorldLoader loader = new(
            null, gameMode.character, gameMode.timeline,
            false, region, null,
            manager.rainWorld.setup,
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

    private void ClearFilterSelectors() {
        _shelters.Clear();
        _gates.Clear();
        _rooms.Clear();
        _shortcuts.Clear();

        _shelterSelector.ClearItems();
        _gateSelector.ClearItems();
        _shortcutSelector1.ClearItems();
        _shortcutSelector2.ClearItems();
    }

    private void OnPlayerListReceived(PlayerInfo[] stupidAndUselessBullshit) => UpdatePlayerCards();
    private void UpdatePlayerCards() {
        foreach (PlayerCard card in _players) {
            pages[0].RemoveSubObject(card);
            card.RemoveSprites();
        }
        _players.Clear();

        foreach (OnlinePlayer player in OnlineManager.players) {
            PlayerCard card = new(this, pages[0], new Vector2(playersPos.x, 0f), player);
            _players.Add(card);
            pages[0].subObjects.Add(card);
        }
    }

    [RPCMethod]
    private static void SwitchReady(RPCEvent rpcEvent) {
        PlayerData data = lobbyData.GetPlayerData(rpcEvent.from);
        data.ready = !data.ready && (lobbyData.endless || lobbyData.state == GameState.Lobby);
        if (data.ready && lobbyData.state != GameState.Lobby)
            gameMode.AssignLateRole(data);
        lobby.NewVersion();
    }

    [RPCMethod]
    private static void SwitchSide(RPCEvent rpcEvent) {
        PlayerData data = lobbyData.GetPlayerData(rpcEvent.from);
        data.SwitchSide();
        if (data.role == PlayerRole.PreferHunter && !lobbyData.allowHunterPreference)
            data.SwitchSide();
        if (data.role == PlayerRole.PreferHider && !lobbyData.allowHiderPreference)
            data.SwitchSide();
        lobby.NewVersion();
    }
}
