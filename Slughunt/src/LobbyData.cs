using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using RainMeadow;
using Slughunt.Serialization;
using Random = UnityEngine.Random;

namespace Slughunt;

public class LobbyData : OnlineResource.ResourceData {
    //private const string DefaultShelter = "SU_S01";
    private const string DefaultShelter = "SU_S04";
    //private const string DefaultShelter = "HI_S03";

    // during gameplay
    public SlughuntGameMode.GameState state { get; set; }
    public string startingShelter { get; set; } = DefaultShelter;

    // world settings
    public SlugcatStats.Name campaign { get; set; } = SlugcatStats.Name.White;
    public bool spawnCreatures { get; set; } = true;
    public HashSet<string> shelters { get; } = [ DefaultShelter ];
    public HashSet<Shortcut> lockedShortcuts { get; } = [];
    public HashSet<string> lockedGates { get; } = [];

    // gameplay settings
    public int hunterCount { get; set; } = 1;

    public string RandomShelter() => shelters.ElementAtOrDefault(Random.Range(0, shelters.Count)) ?? DefaultShelter;

    public override ResourceDataState MakeState(OnlineResource resource) => new State(this);

    public class State : ResourceDataState {
        [OnlineField, UsedImplicitly] private byte _state;
        [OnlineField, UsedImplicitly] private string _startingShelter = DefaultShelter;

        [OnlineField, UsedImplicitly] private SlugcatStats.Name _campaign = SlugcatStats.Name.White;
        [OnlineField, UsedImplicitly] private bool _spawnCreatures = true;
        [OnlineField, UsedImplicitly] private DynamicUnorderedStrings _shelters = new([]);
        [OnlineField, UsedImplicitly] private DynamicUnorderedShortcuts _lockedShortcuts = new([]);
        [OnlineField, UsedImplicitly] private DynamicUnorderedStrings _lockedGates = new([]);

        [OnlineField, UsedImplicitly] private int _hunterCount = 1;

        public State() { }
        public State(LobbyData data) {
            _state = (byte)data.state;
            _startingShelter = data.startingShelter;
            _campaign = data.campaign;
            _spawnCreatures = data.spawnCreatures;
            _shelters = new DynamicUnorderedStrings(data.shelters);
            _lockedShortcuts = new DynamicUnorderedShortcuts(data.lockedShortcuts);
            _lockedGates = new DynamicUnorderedStrings(data.lockedGates);
            _hunterCount = data.hunterCount;
        }

        public override void ReadTo(OnlineResource.ResourceData a, OnlineResource b) {
            LobbyData data = (LobbyData)a;
            data.state = (SlughuntGameMode.GameState)_state;
            data.startingShelter = _startingShelter;
            data.spawnCreatures = _spawnCreatures;
            data.campaign = _campaign;
            _shelters.ReadTo(data.shelters);
            _lockedShortcuts.ReadTo(data.lockedShortcuts);
            _lockedGates.ReadTo(data.lockedGates);
            data.hunterCount = _hunterCount;
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
