using System;
using System.Linq;
using RainMeadow;

namespace Slughunt;

public static partial class Rules {
    public abstract class GameState : EnumClass<GameState> {
        public static readonly InLobby inLobby = new();
        public static readonly Setup setup = new();
        public static readonly Hide hide = new();
        public static readonly Hunt hunt = new();

        private static int readyCount => OnlineManager.players.Count(x => lobbyData.GetPlayerData(x).ready);
        private static bool canStartRound => playerData.ready && readyCount >= 2;

        public abstract bool canEnterGame { get; }
        public abstract bool canJoin { get; }

        public abstract void Enter(GameState from);

        public abstract bool readyForNext { get; }
        protected abstract GameState next { get; }

        public void GoToNextIfReady() {
            if (!readyForNext)
                return;
            lobbyData.state = next;
            OnlineManager.lobby.NewVersion();
        }

        public sealed class InLobby : GameState {
            public override bool canEnterGame => false;
            public override bool canJoin => true;

            public override void Enter(GameState from) {
                foreach (PlayerData data in OnlineManager.players.Select(x => lobbyData.GetPlayerData(x)))
                    data.Reset();
            }

            public override bool readyForNext => canStartRound;
            protected override GameState next => setup;
        }

        public abstract class InGame : GameState {
            public override bool canEnterGame => playerData.ready;
            public override bool canJoin => lobbyData.endless && (
                lobbyData.ruleset.hiderRespawn != OnRespawn.Block ||
                lobbyData.ruleset.hunterRespawn != OnRespawn.Block
            );

            public virtual bool canCatch => false;

            protected static RainWorldGame game => (RainWorldGame)OnlineManager.instance.manager.currentMainLoop;

            // unlike everything else here, called for every player instead of just the host
            public abstract void Tick();
        }

        public sealed class Setup : InGame {
            public override void Enter(GameState from) {
                if (from is InGame)
                    ApplyNextRound();
                lobbyData.startingShelter = lobbyData.RandomShelter();
                JoinAllReady();
            }

            public override void Tick() {
                AbstractCreature abstractPlayer = game.Players[0];
                string shelter = lobbyData.startingShelter;

                bool inShelter = string.Equals(abstractPlayer.Room?.name, shelter, StringComparison.OrdinalIgnoreCase);
                if (abstractPlayer.slatedForDeletion || abstractPlayer.state.dead || !inShelter) {
                    Plugin.Respawn(game, shelter);
                }

                Player? player = game.FirstRealizedPlayer;
                if (player is null)
                    return;

                if (playerData.role is Role.Hunter)
                    player.stun = (int)(40 * lobbyData.hideTime.TotalSeconds);
                else if (playerData.role is Role.Hider)
                    player.inShortcutVessel?.wait = 2;

                player.ChangeCollisionLayer(0);
            }

            public override bool readyForNext {
                get {
                    bool allHidersInShortcuts = true;
                    foreach (OnlinePlayer player in OnlineManager.players) {
                        PlayerData data = lobbyData.GetPlayerData(player);
                        if (!data.ready)
                            continue;
                        data.dead = false;
                        if (data.role is not Role.Hider)
                            continue;
                        allHidersInShortcuts = allHidersInShortcuts &&
                            OnlineManager.lobby.clientSettings.TryGetValue(player, out ClientSettings settings) &&
                            settings.inGame &&
                            settings.avatars.All(x => x.FindEntity(true) is OnlineCreature {
                                realized: true, realizedCreature.inShortcut: true
                            });
                    }
                    return allHidersInShortcuts;
                }
            }

            protected override GameState next => hide;
        }

        public sealed class Hide : InGame {
            public override void Enter(GameState from) { }

            public override void Tick() {
                game.FirstRealizedPlayer?.ChangeCollisionLayer(1);
            }

            public override bool readyForNext =>
                OnlineManager.lobby.owner.tick - lobbyData.switchedStateAt >= (long)lobbyData.hideTimeFrames;

            protected override GameState next => hunt;
        }

        public sealed class Hunt : InGame {
            public override bool canCatch => true;

            public override void Enter(GameState from) {
                foreach (OnlinePlayer player in OnlineManager.players)
                    lobbyData.GetPlayerData(player).ResetUnsavedTime();
            }

            public override void Tick() { }

            public override bool readyForNext {
                get {
                    bool anyHunters = false;
                    bool anyHiders = false;
                    foreach (OnlinePlayer player in OnlineManager.players) {
                        PlayerData data = lobbyData.GetPlayerData(player);
                        if (!data.ready)
                            continue;
                        if (data.role is Role.Hunter)
                            anyHunters = anyHunters || data.role.IsParticipating(data.dead);
                        else if (data.role is Role.Hider)
                            anyHiders = anyHiders || data.role.IsParticipating(data.dead);
                    }
                    return !anyHunters || !anyHiders;
                }
            }

            protected override GameState next => lobbyData.endless && canStartRound ? setup : inLobby;
        }
    }
}
