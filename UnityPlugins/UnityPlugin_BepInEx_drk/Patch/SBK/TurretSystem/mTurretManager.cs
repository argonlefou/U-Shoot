using System.Runtime.CompilerServices;
using HarmonyLib;

using SBK.TurretSystem;

namespace BepInEx_UShoot_Plugin.Patch.SBK_TurretSystem
{
    class mTurretManager
    {
        [HarmonyPatch(typeof(TurretManager), "Awake")]
        class Awake
        {
            static bool Prefix()
            {
                UShoot_Plugin.MyLogger.LogMessage("mTurretManager.Awake()");
                return true;
            }
        }

        [HarmonyPatch(typeof(TurretManager), "Connect")]
        class Connect
        {
            static bool Prefix(TurretManager __instance)
            {
                //DemulShooter_Plugin.MyLogger.LogMessage("mTurretManager.Connect()");
                return true;
            }
        }

        [HarmonyPatch(typeof(TurretManager), "Init")]
        class Init
        {
            static bool Prefix()
            {
                UShoot_Plugin.MyLogger.LogMessage("mTurretManager.Init()");
                return true;
            }
        }

        /// <summary>
        /// Debug build needs the original function to run so that the mouse can work normally
        /// Release build needs to force the result or the Turret Error Message will be displayed
        /// </summary>
        [HarmonyPatch(typeof(TurretManager), "IsConnected")]
        class IsConnected
        {
            static bool Prefix(ref bool __result)
            {
                if (!UnityEngine.Debug.isDebugBuild)
                {
                    __result = true;
                    return false;
                }
                else
                {
                    return true;
                }
            }
        }

        [HarmonyPatch(typeof(TurretManager), "Update")]
        class Update
        {
            static bool Prefix()
            {
                //Plugin.myLogger.LogMessage("mPneumaticSeatManager.Update()");
                return true;
            }
        }

        /*[HarmonyPatch(typeof(TurretManager), "IsInitDone", MethodType.Getter)]
        class get_IsInitDone
        {
            static bool Prefix(TurretManager __instance)
            {
                Plugin.myLogger.LogMessage("mTurretManager.get_IsInitDone()");
                Plugin.myLogger.LogMessage(TurretManager.IsInitDone.ToString());
                return true;
            }

            void postfix(ref bool __result)
            {
                Plugin.myLogger.LogMessage("mTurretManager.get_IsInitDone() postfix : __result=" + __result);
                //__result = true;
            }
        }*/

        [HarmonyPatch(typeof(TurretManager), "set_IsInitDone")]
        class mset_IsInitDone
        {
            [HarmonyReversePatch]
            [MethodImpl(MethodImplOptions.NoInlining)]
            public static void Set_IsInitDone(object instance, bool value)
            {
                //Used to call the private method    
            }
        }



        [HarmonyPatch(typeof(TurretManager), "GetFirmwareVersion")]
        class mGetFirmwareVersion
        {
            static bool Prefix(int controllerID, ref string __result)
            {
                UShoot_Plugin.MyLogger.LogWarning("mTurretManager.GetFirmwareVersion() : controllerID=" + controllerID);
                
                return true;
            }
        }




    }
}
