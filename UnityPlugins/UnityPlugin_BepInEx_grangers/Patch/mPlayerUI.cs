using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace BepInEx_UShoot_Plugin.Patch
{
    class mPlayerUI
    {
        /// <summary>
        /// Handling Coins inputs and Continue in-game
        /// </summary>
        [HarmonyPatch(typeof(PlayerUI), "InCoins")]
        class InCoins
        {
            static bool Prefix(global::PlayerData ___playerData, ref global::StringSpriteController ___coins, ref global::StringSpriteController ___coins2, ref global::StringSpriteController ___coinsContinue, ref int ___continueTime, PlayerUI __instance)
            {
                //DemulShooter_Plugin.MyLogger.LogMessage("PlayerUI.InCoins");

                if (!___playerData.PlayGaming() && ___playerData.Coins >= global::GameData.NeedCoins)
                {
                    ___playerData.Coins -= global::GameData.NeedCoins;
                    ___playerData.playBeginTime = 0f;
                    ___playerData.playRoundTime = 0f;
                    ___playerData.consolationTickets += global::GameData.ConsolationTickets;
                    ___playerData.maxTickets += global::GameData.MaxTickets;
                    global::GameData.SaveCountData();
                    __instance.PlayGame();
                }
                ___coins.SetString(___playerData.Coins + "_" + global::GameData.NeedCoins, -1, default(Color), false);
                ___coins2.SetString(___playerData.Coins + "_" + global::GameData.NeedCoins, -1, default(Color), false);
                if (___playerData.Coins > 0 && global::GameData.Free == 0)
                {
                    ___coins2.transform.parent.gameObject.SetActive(true);
                }
                else
                {
                    ___coins2.transform.parent.gameObject.SetActive(false);
                }
                ___coinsContinue.SetString(___playerData.Coins + "_" + global::GameData.NeedCoins, -1, default(Color), false);
                ___continueTime = global::GameData.ContinueTime;

                return false;
            }
        }

        [HarmonyPatch(typeof(PlayerUI), "Update")]
        class Update
        {
            static bool Prefix(global::PlayerData ___playerData, BulletUIBase ___curbulletUI)
            {
                int PlayerId = (int)___playerData.playerTpye;

                UShoot_Plugin.OutputData.Ammo[PlayerId] = ___curbulletUI.CurBullets;
                UShoot_Plugin.OutputData.Credits[PlayerId] = ___playerData.Coins;

                return true;
            }
        }
    }
}
