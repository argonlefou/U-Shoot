using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HarmonyLib;
using SBK;

namespace BepInEx_UShoot_Plugin.Patch
{
    internal class mPlayer
    {
        /// <summary>
        /// Intercept call to get Damage outputs
        /// </summary>
        [HarmonyPatch(typeof(Player), "ReceiveDamage")]
        class ReceiveDamage
        {
            static bool Prefix(Player __instance, float ___m_Health, int ___m_ID)
            {
                if (___m_Health <= 0f || __instance.IsInvicible || Singleton<PlayerManager>.Instance.GodMode)
                {
                    return true;
                }

                UShoot_Plugin.OutputData.Damaged[___m_ID] = 1;
                return true;
            }
        }

        [HarmonyPatch(typeof(Player), "Update")]
        class Update
        {
            static bool Prefix(Player __instance, ref float ___m_Health, ref float ___m_InvincibilityTimer)
            { 
                if (UnityEngine.Input.GetKeyDown(UnityEngine.KeyCode.M))
                {
                    ___m_InvincibilityTimer = 0;
                }
                return true;
            }
        }
    
    }
}
