using System;
using JetBrains.Annotations;
using RainMeadow;

namespace Slughunt;

public class PlayerData : OnlineEntity.EntityData {
    public bool ready { get; set; }
    public PlayerRole role { get; set; }
    public uint switchedRolesAt { get; private set; }
    public uint timeAsHunter { get; private set; }
    public uint timeAsHider { get; private set; }
    public uint caughtAsHunter { get; set; }
    public uint caughtAsHider { get; set; }

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
                timeAsHunter += OnlineManager.mePlayer.tick - switchedRolesAt;
                break;
            case PlayerRole.Hider:
                role = PlayerRole.Hunter;
                timeAsHider += OnlineManager.mePlayer.tick - switchedRolesAt;
                break;
            default:
                Plugin.logger.LogError($"unknown role? {role}");
                break;
        }
        switchedRolesAt = OnlineManager.mePlayer.tick;
    }

    public uint roleCaught {
        get => role switch {
            PlayerRole.Hunter => caughtAsHunter,
            PlayerRole.Hider => caughtAsHider,
            _ => 0
        };
        set {
            switch (role) {
                case PlayerRole.Hunter:
                    caughtAsHunter = value;
                    break;
                case PlayerRole.Hider:
                    caughtAsHider = value;
                    break;
                case PlayerRole.None:
                case PlayerRole.PreferHunter:
                default:
                    break;
            }
        }
    }

    public override EntityDataState MakeState(OnlineEntity entity, OnlineResource inResource) => new State(this);

    [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
    public class State : EntityDataState {
        [OnlineField] private bool _ready;
        [OnlineField] private byte _role;
        [OnlineField] private uint _switchedRolesAt;
        [OnlineField] private uint _timeAsHunter;
        [OnlineField] private uint _timeAsHider;
        [OnlineField] private uint _caughtAsHunter;
        [OnlineField] private uint _caughtAsHider;

        public State() { }

        public State(PlayerData data) {
            _ready = data.ready;
            _role = (byte)data.role;
            _switchedRolesAt = data.switchedRolesAt;
            _timeAsHunter = data.timeAsHunter;
            _timeAsHider = data.timeAsHider;
            _caughtAsHunter = data.caughtAsHunter;
            _caughtAsHider = data.caughtAsHider;
        }

        public override void ReadTo(OnlineEntity.EntityData a, OnlineEntity b) {
            PlayerData data = (PlayerData)a;
            data.ready = _ready;
            data.role = (PlayerRole)_role;
            data.switchedRolesAt = _switchedRolesAt;
            data.timeAsHunter = _timeAsHunter;
            data.timeAsHider = _timeAsHider;
            data.caughtAsHunter = _caughtAsHunter;
            data.caughtAsHider = _caughtAsHider;
        }

        public override Type GetDataType() => typeof(PlayerData);
    }
}
