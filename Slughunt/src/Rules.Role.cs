using RainMeadow;
using UnityEngine;

namespace Slughunt;

public static partial class Rules {
    public abstract class Role : EnumClass<Role> {
        public static readonly None none = new();
        public static readonly PreferHunter preferHunter = new();
        public static readonly PreferHider preferHider = new();
        public static readonly Hunter hunter = new();
        public static readonly Hider hider = new();

        public abstract Preference AsPreference();
        public abstract Participant? AsParticipant();

        public virtual bool canRespawn => lobbyData.endless;

        // is the player even actually participating in the game?
        // after this becomes false, it should always be false until the next round
        public virtual bool IsParticipating(bool dead) => false;

        // should the players time tick in the current state?
        public virtual bool IsTimed(bool dead) => IsParticipating(dead);

        public abstract class Preference : Role {
            protected abstract bool allowed { get; }
            protected abstract Preference next { get; }
            public Preference nextAllowed => next.allowed ? next : next.nextAllowed;

            public Participant PickParticipant(int currentHunters) {
                int maxHunters = OnlineManager.players.Count - 1;
                if (currentHunters >= maxHunters)
                    return hider;
                if (currentHunters < 1)
                    return hunter;
                Preference preference = allowed ? this : none;
                if (lobbyData.targetHunterCount == 0)
                    return preference.AsParticipant() ?? (RXRandom.Bool() ? hunter : hider);
                int targetHunters = Mathf.Clamp(lobbyData.targetHunterCount, 1, maxHunters);
                return currentHunters < targetHunters ? hunter : hider;
            }
        }

        public sealed class None : Preference {
            public override Preference AsPreference() => this;
            public override Participant? AsParticipant() => null;

            protected override bool allowed => true;
            protected override Preference next => preferHunter;
        }

        public sealed class PreferHunter : Preference {
            public override Preference AsPreference() => this;
            public override Participant AsParticipant() => hunter;

            protected override bool allowed => lobbyData.allowHunterPreference;
            protected override Preference next => preferHider;
        }

        public sealed class PreferHider : Preference {
            public override Preference AsPreference() => this;
            public override Participant AsParticipant() => hider;

            protected override bool allowed => lobbyData.allowHiderPreference;
            protected override Preference next => none;
        }

        public abstract class Participant : Role {
            public abstract OnCatch onCatch { get; }
            public abstract OnRespawn onRespawn { get; }
            public abstract Participant oppositeRole { get; }

            public override bool canRespawn => base.canRespawn && onRespawn != OnRespawn.Block;

            public override bool IsParticipating(bool dead) {
                if (!dead)
                    return true;
                if (!canRespawn)
                    return false;
                return true;
            }
        }

        public sealed class Hunter : Participant {
            public override Preference AsPreference() => preferHunter;
            public override Participant AsParticipant() => this;

            public override OnCatch onCatch => lobbyData.ruleset.hunterCatch;
            public override OnRespawn onRespawn => lobbyData.ruleset.hunterRespawn;
            public override Participant oppositeRole => hider;

            // can the hunter catch someone?
            public bool CanCatch(int stun, bool dead) => IsParticipating(dead) && stun <= 0 && !dead;
        }

        public sealed class Hider : Participant {
            public override Preference AsPreference() => preferHider;
            public override Participant AsParticipant() => this;

            public override OnCatch onCatch => lobbyData.ruleset.hiderCatch;
            public override OnRespawn onRespawn => lobbyData.ruleset.hiderRespawn;
            public override Participant oppositeRole => hunter;

            public override bool IsTimed(bool dead) => base.IsTimed(dead) && CanCatch(dead);

            // can the hider be caught?
            public bool CanCatch(bool dead) {
                if (!IsParticipating(dead))
                    return false;

                if (!dead)
                    return true;

                // if hider dies on catch but is already dead, no point in catch
                if (onCatch == OnCatch.Death)
                    return false;

                return true;
            }
        }
    }
}
