using System;
using MonoMod.RuntimeDetour;
using RainMeadow;

namespace Slughunt;

public static partial class Hooks {
    public static class Rules {
        // ReSharper disable once MemberHidesStaticFromOuterClass
        public static void Apply() {
            OnCatch();
            OnDeath();
            OnRespawn();
        }

        private static void OnCatch() {
            On.Player.Collide += (orig, self, otherObject, myChunk, otherChunk) => {
                orig(self, otherObject, myChunk, otherChunk);
                TryCatchOrKill(self.abstractPhysicalObject, otherObject, false);
                TryCatchOrKill(otherObject.abstractPhysicalObject, self, false);
            };
            // TODO: maybe could make it so that its not only rocks?
            On.Rock.HitSomething += (orig, self, result, eu) => {
                if (!orig(self, result, eu))
                    return false;
                TryCatchOrKill(self.thrownBy.abstractPhysicalObject, result.obj, false);
                return true;
            };
        }

        private static void OnDeath() {
            On.Player.Die += (orig, self) => {
                bool alreadyWasDead = self.dead;
                orig(self);
                if (alreadyWasDead)
                    return;
                if (!self.abstractCreature.GetOnlineCreature()!.isMine)
                    return;
                if (!TryCatchOrKill(self.killTag, self, true))
                    lobby.owner.InvokeRPC(RPC.OnDeath);
            };
        }

        private static bool TryCatchOrKill(AbstractPhysicalObject? attackerObj, PhysicalObject victimObj, bool kill) {
            if (attackerObj is null)
                return false;
            Player? attacker = attackerObj.realizedObject as Player;
            if (victimObj is not Player victim)
                return false;

            OnlinePlayer? otherOnline;
            Delegate rpc;

            if (attackerObj.GetOnlineObject()!.isMine) {
                otherOnline = victim.abstractPhysicalObject.GetOnlineObject()?.owner;
                rpc = RPC.OnCatchOrKillAsAttacker;
            }
            else {
                return false;
            }

            if (otherOnline is null)
                return false;

            bool isCatch =
                attacker is not null &&
                !playerData.pendingCatch &&
                !lobbyData.GetPlayerData(otherOnline).pendingCatch &&
                lobbyData.state.CanCatch(attacker, victim);

            if (!isCatch && !kill)
                return false;

            lobby.owner.InvokeOnceRPC(rpc, otherOnline, isCatch, kill);

            return true;
        }

        private static void OnRespawn() {
            using DetourContext ctx = new(PriorityFirst);
            On.RainWorldGame.GoToDeathScreen += (_, self) => {
                // TODO: update the game over text
                // TODO: spectate
                if (!playerData.role.canRespawn)
                    return;

                Plugin.Respawn(self, lobbyData.RandomShelter());
                self.lastPauseButton = true; // prevent pause

                lobby.owner.InvokeRPC(RPC.OnRespawn);
            };
        }
    }
}
