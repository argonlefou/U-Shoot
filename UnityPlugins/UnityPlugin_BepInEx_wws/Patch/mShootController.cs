using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HarmonyLib;
using UnityEngine;

namespace BepInEx_UShoot_Plugin.Patch
{
    internal class mShootController
    {
        /// <summary>
        /// Geting In-Game recoil
        /// </summary>
        [HarmonyPatch(typeof(ShootController), "HitDown")]
        class HitDown
        {
            static bool Prefix(Vector3 hitPoint, PlayerType playerType, bool delBulletFlag = true)
            {
                //DemulShooter_Plugin.MyLogger.LogWarning("ShootController.HitDown(): playerType=" + playerType);

                if (GameData.GetPlayerData(playerType).PlayGaming() && GameUIController.Instance.CanShoot(playerType) && GameUIController.Instance.begin) 
                    UShoot_Plugin.OutputData.Recoil[(int)playerType] = 1;

                return true;
            }
        }
    }
}
