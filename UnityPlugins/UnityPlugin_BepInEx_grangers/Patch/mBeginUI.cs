using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace BepInEx_UShoot_Plugin.Patch
{
    class mBeginUI
    {
        /// <summary>
        /// Handling Coins and Start in the main title screen
        /// </summary>
        [HarmonyPatch(typeof(BeginUI), "InCoinAction")]
        class InCoinAction
        {
            static bool Prefix(global::PlayerType player, int num, global::StringSpriteController[] ___coins, BeginUI __instance)
            {
                //DemulShooter_Plugin.MyLogger.LogMessage("BeginUI.InCoinAction() : " + player.ToString() + ", " + num);

                global::PlayerData playerData = global::GameData.GetPlayerData(player);
                ___coins[(int)player].SetString(playerData.Coins + "_" + global::GameData.NeedCoins, -1, default(Color), false);

                if (global::GameData.GetPlayerData(player).Stau == global::PlayerStatu.Begin && playerData.Coins >= global::GameData.NeedCoins)
                {
                    playerData.Coins -= global::GameData.NeedCoins;
                    playerData.playBeginTime = 0f;
                    playerData.playRoundTime = 0f;
                    playerData.consolationTickets += global::GameData.ConsolationTickets;
                    playerData.maxTickets += global::GameData.MaxTickets;
                    playerData.life = (float)global::GameData.DefultLife;
                    playerData.gameTime = (float)global::GameData.DefultTime;
                    playerData.playing = true;
                    playerData.ChangeStatu(global::PlayerStatu.Choose);
                    global::GameData.SaveCountData();
                    playerData.isPassLevel = false;

                    MethodInfo mi = typeof(BeginUI).GetMethod("PlayeGame", BindingFlags.Instance | BindingFlags.NonPublic);
                    if (mi != null)
                    {
                        mi.Invoke(__instance, new object[] { player });
                    }
                }

                return false;
            }
        }
    }
}
