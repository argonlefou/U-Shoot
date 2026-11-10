using System.Runtime.CompilerServices;
using HarmonyLib;
using SBK.Skyride.Turret;
using UnityEngine;

namespace BepInEx_UShoot_Plugin.Patch.SBK_Skyride_Turret
{
    internal class mTurret
    {
        /// <summary>
        /// Check for each player if FireBreath is activated, to create our own Rumble output
        /// </summary>
        [HarmonyPatch(typeof(Turret), "Update")]
        class Update
        {
            static bool Prefix(Turret __instance)
            {                
                SBK.ArcadePlayer.Player CurrentPlayer = mget_arcadePlayer.Get_arcadePlayer(__instance);
                if (mget_FireBreathEnabled.Get_FireBreathEnabled(__instance))
                    UShoot_Plugin.OutputData.Rumble[CurrentPlayer.id] = 1;
                else
                    UShoot_Plugin.OutputData.Rumble[CurrentPlayer.id] = 0;
                return true;
            }
        }

        /// <summary>
        /// Intercepting the FireBullet event to create our own recoil output
        /// </summary>
        [HarmonyPatch(typeof(Turret), "FireBullet")]
        class FireBullet
        {
            static bool Prefix(Turret __instance, Vector3 hitScreenPos)
            {
                //DemulShooter_Plugin.MyLogger.LogWarning("SBK.Skyride.Turret.Turret.Firebullet() : ,hitScreenPos=" + hitScreenPos.ToString() + ", arcadePlayer.id=" + mget_arcadePlayer.Get_arcadePlayer(__instance).id.ToString() + ", arcadePlayer.name=" + mget_arcadePlayer.Get_arcadePlayer(__instance).name);
                SBK.ArcadePlayer.Player CurrentPlayer = mget_arcadePlayer.Get_arcadePlayer(__instance);
                UShoot_Plugin.OutputData.Recoil[CurrentPlayer.id] = 1;                
                return true;
            }
        }        

        #region Dummy Calls

        [HarmonyPatch(typeof(Turret), "get_arcadePlayer")]
        class mget_arcadePlayer
        {
            [HarmonyReversePatch]
            [MethodImpl(MethodImplOptions.NoInlining)]
            public static SBK.ArcadePlayer.Player Get_arcadePlayer(object instance)
            {
                //Used to call the private method
                return null;
            }
        }
        [HarmonyPatch(typeof(Turret), "get_FireBreathEnabled")]
        class mget_FireBreathEnabled
        {
            [HarmonyReversePatch]
            [MethodImpl(MethodImplOptions.NoInlining)]
            public static bool Get_FireBreathEnabled(object instance)
            {
                //Used to call the private method
                return true;
            }
        }

        #endregion
    }
}
