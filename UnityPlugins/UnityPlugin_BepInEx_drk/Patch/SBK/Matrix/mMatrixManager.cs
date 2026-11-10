using System.Runtime.CompilerServices;
using HarmonyLib;
using SBK.Matrix;

namespace BepInEx_UShoot_Plugin.Patch.SBK_Matrix
{
    class mMatrixManager
    {
        /// <summary>
        /// Forcing Dongle present data at start and change a few dongle details
        /// </summary>
        [HarmonyPatch(typeof(MatrixManager), "Awake")]
        class Awake
        {
            static bool Prefix(MatrixManager __instance)
            {
                UShoot_Plugin.MyLogger.LogMessage("mMatrixManager.Awake()");
                mset_IsDonglePresent.Set_IsDonglePresent(__instance, true);
                mset_mb_Init.Set_mb_Init(__instance, true);
                mset_DongleId.Set_DongleID(__instance, 11);
                return true;
            }

            static void Postfix(MatrixManager __instance)
            {
                UShoot_Plugin.MyLogger.LogMessage("mMatrixManager.Awake() - Postfix");
                UShoot_Plugin.MyLogger.LogMessage("  --> DeviceId=" + __instance.GetDeviceId().ToString());
                UShoot_Plugin.MyLogger.LogMessage("  --> SerialNumber=" + __instance.GetDongleSerialNumber.ToString());
                UShoot_Plugin.MyLogger.LogMessage("  --> IsDonglePresent=" + __instance.IsDonglePresent.ToString());
            }
        }

        [HarmonyPatch(typeof(MatrixManager), "GetDeviceId")]
        class GetDeviceId
        {
            static bool Prefix(MatrixManager __instance, ref string __result)
            {
                UShoot_Plugin.MyLogger.LogMessage("mMatrixManager.GetDeviceId()");
                __result = "4r60N13F0U";
                return false;
            }
        }
        
        [HarmonyPatch(typeof(MatrixManager), "ReadSerial")]
        class ReadSerial
        {
            static bool Prefix(MatrixManager __instance)
            {
                UShoot_Plugin.MyLogger.LogMessage("mMatrixManager.ReadSerial()");
                return true;
            }
        }

        
        [HarmonyPatch(typeof(MatrixManager), "DongleId", MethodType.Getter)]
        class DongleId
        {
            static bool Prefix(MatrixManager __instance, ref short __result)
            {
                __result = 11;
                return false;
            }
        }

        
        [HarmonyPatch(typeof(MatrixManager), "GetDongleSerialNumber", MethodType.Getter)]
        class GetDongleSerialNumber
        {
            static bool Prefix(MatrixManager __instance, ref int __result)
            {
                __result = 1;   //Bigger values creates Unity error from time to time during gameplay (???)
                return false;
            }
        }

        #region Dummy Calls

        [HarmonyPatch(typeof(MatrixManager), "set_DongleId")]
        class mset_DongleId
        {
            [HarmonyReversePatch]
            [MethodImpl(MethodImplOptions.NoInlining)]
            public static void Set_DongleID(object instance, short value)
            {
                //Used to call the private method    
            }
        }


        [HarmonyPatch(typeof(MatrixManager), "set_IsDonglePresent")]
        class mset_IsDonglePresent
        {
            [HarmonyReversePatch]
            [MethodImpl(MethodImplOptions.NoInlining)]
            public static void Set_IsDonglePresent(object instance, bool value)
            {
                //Used to call the private method    
            }
        }

        [HarmonyPatch(typeof(MatrixManager), "set_mb_Init")]
        class mset_mb_Init
        {
            [HarmonyReversePatch]
            [MethodImpl(MethodImplOptions.NoInlining)]
            public static void Set_mb_Init(object instance, bool value)
            {
                //Used to call the private method    
            }
        }

        #endregion
    }
}
