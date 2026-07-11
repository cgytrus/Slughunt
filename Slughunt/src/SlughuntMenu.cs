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

    private readonly SimplerCheckbox _spawnCreatures;
    private readonly OpComboBox2 _campaignSelector;

    private readonly WorldFilters _worldFilters;
    private readonly WorldFiltersLabel _worldFiltersLabel;

    private readonly PlayerCards _players;

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

        _players = new PlayerCards(this, mainPage, new Vector2(100f, 680f - 24f));
        mainPage.subObjects.Add(_players);
        _players.UpdatePlayerCards();
        MatchmakingManager.OnPlayerListReceived += OnPlayerListReceived;

        gameMode.avatarSettings.currentColors = manager.rainWorld.progression.GetCustomColors(SlughuntGameMode.save);
        SimplerButton colorsButton = new(
            this, mainPage,
            "COLORS",
            _players.pos + new Vector2(PlayerCard.Width + 10f, -30f), new Vector2(110f, 30f)
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
                _worldFilters.pos.x + _worldFilters.size.x + 50f,
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
        _startButton.greyedOut = !playerData.ready || OnlineManager.players.Count < 2;
        _forceStartButton.greyedOut = _startButton.greyedOut;

        _spawnCreatures.inactive = false;
        _campaignSelector.greyedOut = false;

        _worldFilters.Show();
    }

    private void OtherUpdate() {
        _startButton.Hide();
        _forceStartButton.Hide();
        _worldFilters.Hide();

        _spawnCreatures.inactive = true;
        _spawnCreatures.Checked = lobbyData.spawnCreatures;

        _campaignSelector.greyedOut = true;
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
