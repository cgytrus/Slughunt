using System;
using System.Linq;
using Menu;
using Menu.Remix;
using Menu.Remix.MixedUI;
using MoreSlugcats;
using RainMeadow;
using RainMeadow.UI.Components;
using Slughunt.Menu;
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

    private readonly PlayerCards _players;

    private readonly OpUpdown _targetHunterCount;
    private readonly SimplerCheckbox _allowHunterPreference;
    private readonly SimplerCheckbox _allowHiderPreference;
    private readonly OpUpdown _hideTime;
    private readonly OpResourceSelector2 _rulesetPreset;
    private readonly OpResourceSelector2 _rulesetHiderCatch;
    private readonly OpResourceSelector2 _rulesetHiderRespawn;
    private readonly OpResourceSelector2 _rulesetHunterCatch;
    private readonly OpResourceSelector2 _rulesetHunterRespawn;
    private readonly SimplerCheckbox _endless;
    private readonly OpResourceSelector2 _hunterCompass;
    private readonly OpResourceSelector2 _hiderCompass;
    private readonly OpResourceSelector2 _taunts;

    private readonly SimplerCheckbox _spawnCreatures;
    private readonly OpComboBox2 _campaignSelector;
    private readonly WorldFilters _worldFilters;
    private readonly WorldFiltersLabel _worldFiltersLabel;

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

        _players = new PlayerCards(this, mainPage, new Vector2(50f, 680f - 24f));
        mainPage.subObjects.Add(_players);
        _players.UpdatePlayerCards();
        MatchmakingManager.OnPlayerListReceived += OnPlayerListReceived;

        gameMode.avatarSettings.currentColors = manager.rainWorld.progression.GetCustomColors(SlughuntGameMode.save);
        SimplerButton colorsButton = new(
            this, mainPage,
            "COLORS",
            _players.pos + new Vector2(PlayerCard.Width + 20f, -30f), new Vector2(110f, 30f)
        );
        colorsButton.OnClick += _ => {
            ColorSlugcatDialog colorDialog = new(manager, SlughuntGameMode.save, () => {
                gameMode.avatarSettings.currentColors =
                    manager.rainWorld.progression.GetCustomColors(SlughuntGameMode.save);
            });
            manager.ShowDialog(colorDialog);
        };
        mainPage.subObjects.Add(colorsButton);

        const float labelsWidth = 150f;

        AlignedMenuLabel targetHunterCountLabel = new(
            this, mainPage,
            "Target hunter count",
            colorsButton.pos - new Vector2(0f, 30f + 10f),
            new Vector2(labelsWidth, 30f),
            false
        ) {
            labelPosAlignment = FLabelAlignment.Left,
            label = { alignment = FLabelAlignment.Left }
        };
        mainPage.subObjects.Add(targetHunterCountLabel);
        _targetHunterCount = new OpUpdown(
            new Configurable<int>(lobbyData.targetHunterCount, accept: new ConfigAcceptableRange<int>(0, ushort.MaxValue)),
            targetHunterCountLabel.pos + new Vector2(labelsWidth, 0f), 80f
        );
        _targetHunterCount._lastArrX = _targetHunterCount._arrX;
        _targetHunterCount.OnValueChanged += (_, _, _) => {
            if (!lobby.isOwner)
                return;
            lobbyData.targetHunterCount = (ushort)_targetHunterCount.valueInt;
            lobby.NewVersion();
        };
        _ = new UIelementWrapper(tabWrapper, _targetHunterCount);

        _allowHunterPreference = new SimplerCheckbox(
            this, mainPage,
            _targetHunterCount.pos - new Vector2(0f, 24f + 5f),
            labelsWidth, "Allow hunter preference"
        );
        _allowHunterPreference.OnClick += value => {
            if (!lobby.isOwner)
                return;
            lobbyData.allowHunterPreference = value;
            lobby.NewVersion();
        };
        mainPage.subObjects.Add(_allowHunterPreference);
        _allowHunterPreference.Checked = lobbyData.allowHunterPreference;

        _allowHiderPreference = new SimplerCheckbox(
            this, mainPage,
            _allowHunterPreference.pos - new Vector2(0f, 24f + 5f),
            labelsWidth, "Allow hider preference"
        );
        _allowHiderPreference.OnClick += value => {
            if (!lobby.isOwner)
                return;
            lobbyData.allowHiderPreference = value;
            lobby.NewVersion();
        };
        mainPage.subObjects.Add(_allowHiderPreference);
        _allowHiderPreference.Checked = lobbyData.allowHiderPreference;

        AlignedMenuLabel hideTimeLabel = new(
            this, mainPage,
            "Hide time (seconds)",
            _allowHiderPreference.pos - new Vector2(labelsWidth, 30f + 5f),
            new Vector2(labelsWidth, 30f),
            false
        ) {
            labelPosAlignment = FLabelAlignment.Left,
            label = { alignment = FLabelAlignment.Left }
        };
        mainPage.subObjects.Add(hideTimeLabel);
        _hideTime = new OpUpdown(
            new Configurable<int>((int)lobbyData.hideTime.TotalSeconds, accept: new ConfigAcceptableRange<int>(0, int.MaxValue)),
            hideTimeLabel.pos + new Vector2(labelsWidth, 0f), 80f
        );
        _hideTime._lastArrX = _hideTime._arrX;
        _hideTime.OnValueChanged += (_, _, _) => {
            if (!lobby.isOwner)
                return;
            lobbyData.hideTime = TimeSpan.FromSeconds(_hideTime.valueInt);
            lobby.NewVersion();
        };
        _ = new UIelementWrapper(tabWrapper, _hideTime);

        AlignedMenuLabel rulesetPresetLabel = new(
            this, mainPage,
            "Ruleset",
            hideTimeLabel.pos - new Vector2(0f, 24f + 10f),
            new Vector2(labelsWidth, 24f),
            false
        ) {
            labelPosAlignment = FLabelAlignment.Left,
            label = { alignment = FLabelAlignment.Left }
        };
        mainPage.subObjects.Add(rulesetPresetLabel);
        _rulesetPreset = new OpResourceSelector2(
            new Configurable<Ruleset.PresetName>(lobbyData.ruleset.GetPresetName()),
            rulesetPresetLabel.pos + new Vector2(labelsWidth, 0f),
            200f
        );
        _rulesetPreset.OnValueChanged += (_, value, _) => {
            if (!lobby.isOwner)
                return;
            lobbyData.ruleset = Ruleset.GetPreset(
                ValueConverter.ConvertToValue<Ruleset.PresetName>(value),
                lobbyData.ruleset
            );
            lobby.NewVersion();
            _rulesetHiderCatch!.value = ValueConverter.ConvertToString(lobbyData.ruleset.hiderCatch);
            _rulesetHiderRespawn!.value = ValueConverter.ConvertToString(lobbyData.ruleset.hiderRespawn);
            _rulesetHunterCatch!.value = ValueConverter.ConvertToString(lobbyData.ruleset.hunterCatch);
            _rulesetHunterRespawn!.value = ValueConverter.ConvertToString(lobbyData.ruleset.hunterRespawn);
        };
        _ = new UIelementWrapper(tabWrapper, _rulesetPreset);

        AlignedMenuLabel rulesetHiderLabel = new(
            this, mainPage,
            "Hider (catch/respawn)",
            rulesetPresetLabel.pos - new Vector2(-10f, 24f + 5f),
            new Vector2(labelsWidth, 24f),
            false
        ) {
            labelPosAlignment = FLabelAlignment.Left,
            label = { alignment = FLabelAlignment.Left }
        };
        mainPage.subObjects.Add(rulesetHiderLabel);
        _rulesetHiderCatch = new OpResourceSelector2(
            new Configurable<Rules.OnCatch>(lobbyData.ruleset.hiderCatch),
            _rulesetPreset.pos - new Vector2(0f, _rulesetPreset.size.y + 5f),
            _rulesetPreset.size.x * 0.5f - 2.5f
        );
        _rulesetHiderCatch.OnValueChanged += (_, value, _) => {
            if (!lobby.isOwner)
                return;
            lobbyData.ruleset = lobbyData.ruleset with {
                hiderCatch = ValueConverter.ConvertToValue<Rules.OnCatch>(value)
            };
            lobby.NewVersion();
            _rulesetPreset.value = ValueConverter.ConvertToString(lobbyData.ruleset.GetPresetName());
        };
        _ = new UIelementWrapper(tabWrapper, _rulesetHiderCatch);

        _rulesetHiderRespawn = new OpResourceSelector2(
            new Configurable<Rules.OnRespawn>(lobbyData.ruleset.hiderRespawn),
            _rulesetHiderCatch.pos + new Vector2(_rulesetHiderCatch.size.x + 5f, 0f),
            _rulesetHiderCatch.size.x
        );
        _rulesetHiderRespawn.OnValueChanged += (_, value, _) => {
            if (!lobby.isOwner)
                return;
            lobbyData.ruleset = lobbyData.ruleset with {
                hiderRespawn = ValueConverter.ConvertToValue<Rules.OnRespawn>(value)
            };
            lobby.NewVersion();
            _rulesetPreset.value = ValueConverter.ConvertToString(lobbyData.ruleset.GetPresetName());
        };
        _ = new UIelementWrapper(tabWrapper, _rulesetHiderRespawn);

        AlignedMenuLabel rulesetHunterLabel = new(
            this, mainPage,
            "Hunter (catch/respawn)",
            rulesetHiderLabel.pos - new Vector2(0f, 24f + 5f),
            new Vector2(labelsWidth, 24f),
            false
        ) {
            labelPosAlignment = FLabelAlignment.Left,
            label = { alignment = FLabelAlignment.Left }
        };
        mainPage.subObjects.Add(rulesetHunterLabel);
        _rulesetHunterCatch = new OpResourceSelector2(
            new Configurable<Rules.OnCatch>(lobbyData.ruleset.hunterCatch),
            _rulesetHiderCatch.pos - new Vector2(0f, _rulesetHiderCatch.size.y + 5f),
            _rulesetHiderRespawn.size.x
        );
        _rulesetHunterCatch.OnValueChanged += (_, value, _) => {
            if (!lobby.isOwner)
                return;
            lobbyData.ruleset = lobbyData.ruleset with {
                hunterCatch = ValueConverter.ConvertToValue<Rules.OnCatch>(value)
            };
            lobby.NewVersion();
            _rulesetPreset.value = ValueConverter.ConvertToString(lobbyData.ruleset.GetPresetName());
        };
        _ = new UIelementWrapper(tabWrapper, _rulesetHunterCatch);

        _rulesetHunterRespawn = new OpResourceSelector2(
            new Configurable<Rules.OnRespawn>(lobbyData.ruleset.hunterRespawn),
            _rulesetHunterCatch.pos + new Vector2(_rulesetHunterCatch.size.x + 5f, 0f),
            _rulesetHunterCatch.size.x
        );
        _rulesetHunterRespawn.OnValueChanged += (_, value, _) => {
            if (!lobby.isOwner)
                return;
            lobbyData.ruleset = lobbyData.ruleset with {
                hunterRespawn = ValueConverter.ConvertToValue<Rules.OnRespawn>(value)
            };
            lobby.NewVersion();
            _rulesetPreset.value = ValueConverter.ConvertToString(lobbyData.ruleset.GetPresetName());
        };
        _ = new UIelementWrapper(tabWrapper, _rulesetHunterRespawn);

        _endless = new SimplerCheckbox(
            this, mainPage,
            _rulesetHunterCatch.pos - new Vector2(0f, 24f + 5f),
            labelsWidth, "Endless"
        );
        _endless.OnClick += value => {
            if (!lobby.isOwner)
                return;
            lobbyData.endless = value;
            lobby.NewVersion();
        };
        mainPage.subObjects.Add(_endless);
        _endless.Checked = lobbyData.endless;

        AlignedMenuLabel hunterCompassLabel = new(
            this, mainPage,
            "Hunter compass",
            _endless.pos - new Vector2(labelsWidth, 24f + 10f),
            new Vector2(labelsWidth, 24f),
            false
        ) {
            labelPosAlignment = FLabelAlignment.Left,
            label = { alignment = FLabelAlignment.Left }
        };
        mainPage.subObjects.Add(hunterCompassLabel);
        _hunterCompass = new OpResourceSelector2(
            new Configurable<CompassMode>(lobbyData.hunterCompass),
            hunterCompassLabel.pos + new Vector2(labelsWidth, 0f),
            200f
        );
        _hunterCompass.OnValueChanged += (_, value, _) => {
            if (!lobby.isOwner)
                return;
            lobbyData.hunterCompass = ValueConverter.ConvertToValue<CompassMode>(value);
            lobby.NewVersion();
        };
        _ = new UIelementWrapper(tabWrapper, _hunterCompass);

        AlignedMenuLabel hiderCompassLabel = new(
            this, mainPage,
            "Hider compass",
            hunterCompassLabel.pos - new Vector2(0f, 24f + 5f),
            new Vector2(labelsWidth, 24f),
            false
        ) {
            labelPosAlignment = FLabelAlignment.Left,
            label = { alignment = FLabelAlignment.Left }
        };
        mainPage.subObjects.Add(hiderCompassLabel);
        _hiderCompass = new OpResourceSelector2(
            new Configurable<CompassMode>(lobbyData.hiderCompass),
            hiderCompassLabel.pos + new Vector2(labelsWidth, 0f),
            200f
        );
        _hiderCompass.OnValueChanged += (_, value, _) => {
            if (!lobby.isOwner)
                return;
            lobbyData.hiderCompass = ValueConverter.ConvertToValue<CompassMode>(value);
            lobby.NewVersion();
        };
        _ = new UIelementWrapper(tabWrapper, _hiderCompass);

        AlignedMenuLabel tauntsLabel = new(
            this, mainPage,
            "Taunts",
            hiderCompassLabel.pos - new Vector2(0f, 24f + 5f),
            new Vector2(labelsWidth, 24f),
            false
        ) {
            labelPosAlignment = FLabelAlignment.Left,
            label = { alignment = FLabelAlignment.Left }
        };
        mainPage.subObjects.Add(tauntsLabel);
        _taunts = new OpResourceSelector2(
            new Configurable<TauntMode>(lobbyData.taunts),
            tauntsLabel.pos + new Vector2(labelsWidth, 0f),
            200f
        );
        _taunts.OnValueChanged += (_, value, _) => {
            if (!lobby.isOwner)
                return;
            lobbyData.taunts = ValueConverter.ConvertToValue<TauntMode>(value);
            lobby.NewVersion();
        };
        _ = new UIelementWrapper(tabWrapper, _taunts);

        _spawnCreatures = new SimplerCheckbox(
            this, mainPage,
            new Vector2(_taunts.pos.x + _taunts.size.x + 20f, colorsButton.pos.y),
            42f, "Spawn creatures", true
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
            _worldFiltersLabel!.UpdateText();
            _worldFilters!.UpdateRegions();
        };
        _ = new UIelementWrapper(tabWrapper, _campaignSelector);

        _worldFilters = new WorldFilters(
            this, mainPage,
            _campaignSelector.pos - new Vector2(0f, _campaignSelector.size.y + 10f)
        );
        mainPage.subObjects.Add(_worldFilters);

        _worldFiltersLabel = new WorldFiltersLabel(
            this, mainPage,
            new Vector2(
                _worldFilters.pos.x + _worldFilters.size.x + 20f,
                colorsButton.pos.y + colorsButton.size.y
            )
        );
        mainPage.subObjects.Add(_worldFiltersLabel);

        _worldFilters.onFiltersUpdated += _worldFiltersLabel.UpdateText;
        _worldFiltersLabel.UpdateText();
        _worldFilters.UpdateRegions();
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
        int readyPlayers = OnlineManager.players.Count(x => lobbyData.GetPlayerData(x).ready);
        _startButton.greyedOut = !playerData.ready || readyPlayers < 2;
        _forceStartButton.greyedOut = _startButton.greyedOut;

        _targetHunterCount.greyedOut = false;
        _allowHunterPreference.inactive = false;
        _allowHiderPreference.inactive = false;
        _hideTime.greyedOut = false;
        _rulesetPreset.greyedOut = false;
        _rulesetHiderCatch.greyedOut = false;
        _rulesetHiderRespawn.greyedOut = false;
        _rulesetHunterCatch.greyedOut = false;
        _rulesetHunterRespawn.greyedOut = false;
        _endless.inactive = false;
        _hunterCompass.greyedOut = false;
        _hiderCompass.greyedOut = false;
        _taunts.greyedOut = false;

        _spawnCreatures.inactive = false;
        _campaignSelector.greyedOut = false;

        _worldFilters.Show();
    }

    private void OtherUpdate() {
        _startButton.Hide();
        _forceStartButton.Hide();
        _worldFilters.Hide();

        _targetHunterCount.greyedOut = true;
        _allowHunterPreference.inactive = true;
        _allowHiderPreference.inactive = true;
        _hideTime.greyedOut = true;
        _rulesetPreset.greyedOut = true;
        _rulesetHiderCatch.greyedOut = true;
        _rulesetHiderRespawn.greyedOut = true;
        _rulesetHunterCatch.greyedOut = true;
        _rulesetHunterRespawn.greyedOut = true;
        _endless.inactive = true;
        _hunterCompass.greyedOut = true;
        _hiderCompass.greyedOut = true;
        _taunts.greyedOut = true;

        _spawnCreatures.inactive = true;
        _campaignSelector.greyedOut = true;

        _targetHunterCount.valueInt = lobbyData.targetHunterCount;
        _allowHunterPreference.Checked = lobbyData.allowHunterPreference;
        _allowHiderPreference.Checked = lobbyData.allowHiderPreference;
        _hideTime.valueInt = (int)lobbyData.hideTime.TotalSeconds;
        _rulesetPreset.value = ValueConverter.ConvertToString(lobbyData.ruleset.GetPresetName());
        _rulesetHiderCatch.value = ValueConverter.ConvertToString(lobbyData.ruleset.hiderCatch);
        _rulesetHiderRespawn.value = ValueConverter.ConvertToString(lobbyData.ruleset.hiderRespawn);
        _rulesetHunterCatch.value = ValueConverter.ConvertToString(lobbyData.ruleset.hunterCatch);
        _rulesetHunterRespawn.value = ValueConverter.ConvertToString(lobbyData.ruleset.hunterRespawn);
        _endless.Checked = lobbyData.endless;
        _hunterCompass.value = ValueConverter.ConvertToString(lobbyData.hunterCompass);
        _hiderCompass.value = ValueConverter.ConvertToString(lobbyData.hiderCompass);
        _taunts.value = ValueConverter.ConvertToString(lobbyData.taunts);

        _spawnCreatures.Checked = lobbyData.spawnCreatures;
        _campaignSelector.value = lobbyData.campaign.value;

        // if we are the owner this is updated by our own ui interactions
        _worldFiltersLabel.UpdateText();
    }

    private void OnPlayerListReceived(PlayerInfo[] stupidAndUselessBullshit) => _players.UpdatePlayerCards();

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
