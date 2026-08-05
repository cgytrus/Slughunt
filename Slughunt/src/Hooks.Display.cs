using MonoMod.RuntimeDetour;
using RainMeadow;
using Slughunt.HUD;
using UnityEngine;

namespace Slughunt;

public static partial class Hooks {
    public static class Display {
        // ReSharper disable once MemberHidesStaticFromOuterClass
        public static void Apply() {
            RoleColors();
            FoodHud();
            KarmaHud();
            HudParts();
        }

        private static void RoleColors() {
            On.PlayerGraphics.DrawSprites += (orig, self, sLeaser, rCam, timeStacker, camPos) => {
                if (
                    !self.player.abstractPhysicalObject.GetOnlineObject(out OnlinePhysicalObject? opo) ||
                    opo?.owner is null
                ) {
                    orig(self, sLeaser, rCam, timeStacker, camPos);
                    return;
                }
                PlayerData data = lobbyData.GetPlayerData(opo.owner);
                if (data.role is Slughunt.Rules.Role.Hunter) {
                    self.markAlpha = 1.0f;
                    sLeaser.sprites[10].color = new HSLColor(12f / 360f, 1.0f, 0.55f).rgb;
                    sLeaser.sprites[11].color = Color.Lerp(sLeaser.sprites[10].color, Color.white, 0.3f);
                }
                else {
                    self.markAlpha = 0.0f;
                }
                orig(self, sLeaser, rCam, timeStacker, camPos);
            };
        }

        private static void FoodHud() {
            using DetourContext ctx = new(PriorityFirst);
            On.HUD.FoodMeter.Draw += (_, _, _) => { };
        }

        private static void KarmaHud() {
            On.HUD.KarmaMeter.Update += (orig, self) => {
                orig(self);
                self.UpdateGraphic(
                    playerData.role switch {
                        Slughunt.Rules.Role.Hunter => 0,
                        Slughunt.Rules.Role.Hider => 4,
                        _ => 9
                    }, 9
                );
            };
        }

        private static void HudParts() {
            On.HUD.HUD.InitSinglePlayerHud += (orig, self, cam) => {
                orig(self, cam);
                self.AddPart(new GameInfoPart(self, self.fContainers[0]));

                if (!MatchmakingManager.currentInstance.canSendChatMessages)
                    return;
                if (!RMOverlayHUDMenu.TryGetOverlay(out RMOverlayHUD overlayHud))
                    return;
                if (overlayHud.chatHud is null) {
                    overlayHud.AddChatHUD(cam);
                }
                else {
                    overlayHud.SetNewChatHUDCamera(cam);
                }
            };
        }
    }
}
