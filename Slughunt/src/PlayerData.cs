using System;
using JetBrains.Annotations;
using RainMeadow;

namespace Slughunt;

public class PlayerData : OnlineEntity.EntityData {
    public SlughuntGameMode.PlayerRole role { get; set; }
    public bool hunterCandidate { get; set; }
    public bool ready { get; set; }

    public override EntityDataState MakeState(OnlineEntity entity, OnlineResource inResource) => new State(this);

    public class State : EntityDataState {
        [OnlineField, UsedImplicitly] private byte _role;
        [OnlineField, UsedImplicitly] private bool _hunterCandidate;
        [OnlineField, UsedImplicitly] private bool _ready;

        public State() { }

        public State(PlayerData data) {
            _role = (byte)data.role;
            _hunterCandidate = data.hunterCandidate;
            _ready = data.ready;
        }

        public override void ReadTo(OnlineEntity.EntityData a, OnlineEntity b) {
            PlayerData data = (PlayerData)a;
            data.role = (SlughuntGameMode.PlayerRole)_role;
            data.hunterCandidate = _hunterCandidate;
            data.ready = _ready;
        }

        public override Type GetDataType() => typeof(PlayerData);
    }
}
