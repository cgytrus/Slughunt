using System;
using System.Collections.Generic;
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

        // can a catch actually take place in the current state of the hunter and the hider involved?
        public virtual bool CanCatch(Creature hunter, Creature hider) => false;

        public abstract void Join(PlayerData data);
        public abstract void Leave(PlayerData data);

        public abstract bool readyForNext { get; }
        public abstract GameState next { get; }

        public sealed class InLobby : GameState {
            public override bool canEnterGame => false;
            public override bool canJoin => true;

            public override void Enter(GameState from) {
                foreach (PlayerData data in OnlineManager.players.Select(x => lobbyData.GetPlayerData(x)))
                    from.Leave(data);
            }

            public override void Join(PlayerData data) => data.ready = true;
            public override void Leave(PlayerData data) => data.ready = false;

            public override bool readyForNext => canStartRound;
            public override GameState next => setup;
        }

        public abstract class InGame : GameState {
            public override bool canEnterGame => playerData.ready;
            public override bool canJoin => Role.hunter.canRespawn || Role.hider.canRespawn;

            protected static RainWorldGame game => (RainWorldGame)OnlineManager.instance.manager.currentMainLoop;

            public override void Join(PlayerData data) {
                bool hunterCanRespawn = Role.hunter.canRespawn;
                bool hiderCanRespawn = Role.hider.canRespawn;
                if (hunterCanRespawn && hiderCanRespawn) {
                    int hunterCount = OnlineManager.players.Count(x => lobbyData.GetPlayerData(x).role is Role.Hunter);
                    data.role = data.role.AsPreference().PickParticipant(hunterCount);
                    data.ready = true;
                }
                else if (hunterCanRespawn) {
                    data.role = Role.hunter;
                    data.ready = true;
                }
                else if (hiderCanRespawn) {
                    data.role = Role.hider;
                    data.ready = true;
                }
                else {
                    data.ready = false;
                }
            }

            public override void Leave(PlayerData data) {
                data.ready = false;
                data.role = Role.none;
                data.dead = false;
            }

            // unlike everything else here, called for every player instead of just the host
            public abstract void Tick();
        }

        public sealed class Setup : InGame {
            public override void Enter(GameState from) {
                if (from is InGame)
                    OnNextRound();

                lobbyData.startingShelter = lobbyData.RandomShelter();

                List<PlayerData> players = OnlineManager.players
                    .Select(x => lobbyData.GetPlayerData(x))
                    .Where(x => x.ready)
                    .OrderBy(_ => RXRandom.Int())
                    .ToList();
                int hunterCount = 0;
                foreach (PlayerData data in players.Where(x => x.role is Role.PreferHunter)) {
                    data.role = data.role.AsPreference().PickParticipant(hunterCount);
                    if (data.role is Role.Hunter)
                        hunterCount++;
                }
                foreach (PlayerData data in players.Where(x => x.role is Role.None)) {
                    data.role = data.role.AsPreference().PickParticipant(hunterCount);
                    if (data.role is Role.Hunter)
                        hunterCount++;
                }
                foreach (PlayerData data in players.Where(x => x.role is Role.PreferHider)) {
                    data.role = data.role.AsPreference().PickParticipant(hunterCount);
                    if (data.role is Role.Hunter)
                        hunterCount++;
                }
            }

            public override void Tick() {
                AbstractCreature abstractPlayer = game.Players[0];
                string shelter = lobbyData.startingShelter;

                bool inShelter = string.Equals(abstractPlayer.Room?.name, shelter, StringComparison.OrdinalIgnoreCase);
                if (abstractPlayer.slatedForDeletion || abstractPlayer.state.dead || !inShelter)
                    Plugin.Respawn(game, shelter);

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

            public override GameState next => hide;
        }

        public sealed class Hide : InGame {
            public override void Enter(GameState from) { }

            public override void Tick() {
                game.FirstRealizedPlayer?.ChangeCollisionLayer(1);
            }

            public override bool readyForNext =>
                OnlineManager.lobby.owner.tick - lobbyData.switchedStateAt >= (long)lobbyData.hideTimeFrames;

            public override GameState next => hunt;
        }

        public sealed class Hunt : InGame {
            public override void Enter(GameState from) {
                foreach (OnlinePlayer player in OnlineManager.players)
                    lobbyData.GetPlayerData(player).ResetUnsavedTime();
            }

            public override bool CanCatch(Creature hunter, Creature hider) =>
                Role.hunter.CanCatch(hunter.stun, hunter.dead) &&
                Role.hider.CanCatch(hider.dead);

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

            public override GameState next => lobbyData.endless && canStartRound ? setup : inLobby;
        }
    }
}
