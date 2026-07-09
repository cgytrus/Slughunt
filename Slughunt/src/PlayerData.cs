using System;
using JetBrains.Annotations;
using RainMeadow;

namespace Slughunt;

public class PlayerData : OnlineEntity.EntityData {
    public bool ready { get; set; }
    // TODO: move to somewhere controlled by the lobby
    public PlayerRole role { get; set; }
    // TODO: points

    public void SwitchSide() {
        switch (role) {
            case PlayerRole.None:
                role = PlayerRole.PreferHunter;
                break;
            case PlayerRole.PreferHunter:
                role = PlayerRole.None;
                break;
            case PlayerRole.Hunter:
                role = PlayerRole.Hider;
                break;
            case PlayerRole.Hider:
                role = PlayerRole.Hunter;
                break;
            default:
                Plugin.logger.LogError($"unknown role? {role}");
                break;
        }
    }

    public override EntityDataState MakeState(OnlineEntity entity, OnlineResource inResource) => new State(this);

    [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
    public class State : EntityDataState {
        [OnlineField] private bool _ready;
        [OnlineField] private byte _role;

        public State() { }

        public State(PlayerData data) {
            _ready = data.ready;
            _role = (byte)data.role;
        }

        public override void ReadTo(OnlineEntity.EntityData a, OnlineEntity b) {
            PlayerData data = (PlayerData)a;
            data.ready = _ready;
            data.role = (PlayerRole)_role;
        }

        public override Type GetDataType() => typeof(PlayerData);
    }
}
