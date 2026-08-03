using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using RainMeadow;
using Slughunt.Serialization;

namespace Slughunt;

public class LobbyData : OnlineResource.ResourceData {
    //private const string DefaultShelter = "SU_S01";
    private const string DefaultShelter = "SU_S04";
    //private const string DefaultShelter = "HI_S03";

    // world settings
    public bool spawnCreatures { get; set; } = true;
    public SlugcatStats.Name campaign { get; set; } = SlugcatStats.Name.White;
    public string startingShelter { get; set; } = DefaultShelter;
    public HashSet<string> shelters { get; } = [ DefaultShelter ];
    public HashSet<Shortcut> lockedShortcuts { get; } = [];
    public HashSet<string> lockedGates { get; } = [];

    // pre gameplay settings
    public ushort targetHunterCount { get; set; } = 1;
    public bool allowHunterPreference { get; set; } = true;
    public bool allowHiderPreference { get; set; } = true;

    // gameplay settings
    public TimeSpan hideTime { get; set; } = TimeSpan.FromSeconds(6.0);
    public double hideTimeFrames => OnlineManager.instance.framesPerSecond * hideTime.TotalSeconds;
    public Ruleset ruleset { get; set; } = Ruleset.hideAndSeek;
    public bool endless { get; set; }
    public Rules.CompassMode hunterCompass { get; set; } = Rules.CompassMode.Off; // TODO
    public Rules.CompassMode hiderCompass { get; set; } = Rules.CompassMode.Off; // TODO
    public Rules.TauntMode taunts { get; set; } = Rules.TauntMode.Off; // TODO

    private Rules.GameState _state = Rules.GameState.inLobby;
    public Rules.GameState state {
        get => _state;
        set {
            if (_state == value)
                return;
            value.Enter(_state);
            _state = value;
            switchedStateAt = OnlineManager.lobby.owner.tick;
        }
    }

    public uint switchedStateAt { get; private set; }
    private Dictionary<ushort, PlayerData> playerData { get; } = [];

    public SlugcatStats.Name character => campaign;
    public SlugcatStats.Timeline timeline => SlugcatStats.SlugcatToTimeline(campaign);

    public string RandomShelter() => shelters.ElementAtOrDefault(RXRandom.Int(shelters.Count)) ?? DefaultShelter;

    public void RemovePlayerData(OnlinePlayer player) {
        playerData.Remove(player.inLobbyId);
    }

    public PlayerData GetPlayerData(OnlinePlayer player) {
        if (playerData.TryGetValue(player.inLobbyId, out PlayerData? data))
            return data;
        data = new PlayerData();
        playerData[player.inLobbyId] = data;
        return data;
    }

    public override ResourceDataState MakeState(OnlineResource resource) => new State(this);

    [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
    public class State : ResourceDataState {
        [OnlineField] private bool _spawnCreatures;
        [OnlineField] private SlugcatStats.Name _campaign = SlugcatStats.Name.White;
        [OnlineField] private string _startingShelter = DefaultShelter;
        [OnlineField] private DynamicUnorderedStrings _shelters = new([]);
        [OnlineField] private DynamicUnorderedShortcuts _lockedShortcuts = new([]);
        [OnlineField] private DynamicUnorderedStrings _lockedGates = new([]);

        [OnlineField] private ushort _targetHunterCount;
        [OnlineField] private bool _allowHunterPreference;
        [OnlineField] private bool _allowHiderPreference;

        [OnlineField] private ulong _hideTime; // rain meadow doesnt support fucking longs but supports ulongs, cool
        [OnlineField] private byte _ruleset;
        [OnlineField] private bool _endless;
        [OnlineField] private byte _hunterCompass;
        [OnlineField] private byte _hiderCompass;
        [OnlineField] private byte _taunts;

        [OnlineField] private byte _state;
        [OnlineField] private uint _switchedStateAt;
        [OnlineField] private DynamicPlayerData _playerData = new([]);

        public State() { }
        public State(LobbyData data) {
            _spawnCreatures = data.spawnCreatures;
            _campaign = data.campaign;
            _startingShelter = data.startingShelter;
            _shelters = new DynamicUnorderedStrings(data.shelters);
            _lockedShortcuts = new DynamicUnorderedShortcuts(data.lockedShortcuts);
            _lockedGates = new DynamicUnorderedStrings(data.lockedGates);
            _targetHunterCount = data.targetHunterCount;
            _allowHunterPreference = data.allowHunterPreference;
            _allowHiderPreference = data.allowHiderPreference;
            _hideTime = unchecked((ulong)data.hideTime.Ticks);
            _ruleset = (byte)data.ruleset;
            _endless = data.endless;
            _hunterCompass = (byte)data.hunterCompass;
            _hiderCompass = (byte)data.hiderCompass;
            _taunts = (byte)data.taunts;
            _state = (byte)data._state;
            _switchedStateAt = data.switchedStateAt;
            _playerData = new DynamicPlayerData(data.playerData);
        }

        public override void ReadTo(OnlineResource.ResourceData a, OnlineResource b) {
            LobbyData data = (LobbyData)a;
            data.spawnCreatures = _spawnCreatures;
            data.campaign = _campaign;
            data.startingShelter = _startingShelter;
            _shelters.ReadTo(data.shelters);
            _lockedShortcuts.ReadTo(data.lockedShortcuts);
            _lockedGates.ReadTo(data.lockedGates);
            data.targetHunterCount = _targetHunterCount;
            data.allowHunterPreference = _allowHunterPreference;
            data.allowHiderPreference = _allowHiderPreference;
            data.hideTime = TimeSpan.FromTicks(unchecked((long)_hideTime));
            data.ruleset = (Ruleset)_ruleset;
            data.endless = _endless;
            data.hunterCompass = (Rules.CompassMode)_hunterCompass;
            data.hiderCompass = (Rules.CompassMode)_hiderCompass;
            data.taunts = (Rules.TauntMode)_taunts;
            data._state = (Rules.GameState)_state;
            data.switchedStateAt = _switchedStateAt;
            _playerData.ReadTo(data.playerData);
        }

        public override Type GetDataType() => typeof(LobbyData);
    }

    public readonly record struct Shortcut(string a, string b) {
        public bool Equals(Shortcut other) => a == other.a && b == other.b || a == other.b && b == other.a;

        public override int GetHashCode() {
            unchecked {
                return a.GetHashCode() + b.GetHashCode();
            }
        }

        public override string ToString() => $"{a} - {b}";
    }
}
