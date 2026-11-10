using System.Runtime.CompilerServices;
using HarmonyLib;
using SBK.PneumaticSeatSystem;

namespace BepInEx_UShoot_Plugin.Patch.SBK_PneumaticSeatsystem
{
    class mPneumaticSeatManager
    {
        [HarmonyPatch(typeof(PneumaticSeatManager), "Awake")]
        class Awake
        {
            static bool Prefix(PneumaticSeatManager __instance)
            {
                UShoot_Plugin.MyLogger.LogMessage("mPneumaticSeatManager.Awake()");
                return true;
            }
        }

        /// <summary>
        /// Forcing Isconnected to true
        /// </summary>
        [HarmonyPatch(typeof(SBK.PneumaticSeatSystem.PneumaticSeatManager), "Connect")]
        class Connect
        {
            static bool Prefix(PneumaticSeatManager __instance)
            {
                UShoot_Plugin.MyLogger.LogMessage("mPneumaticSeatManager.Connect()");
                mset_IsConnected.Set_IsConnected(__instance, true);                
                return false;
            }
        }

        [HarmonyPatch(typeof(SBK.PneumaticSeatSystem.PneumaticSeatManager), "GetInitialisationInfo")]
        class GetInitialisationInfo
        {
            static bool Prefix(PneumaticSeatManager __instance)
            {
                UShoot_Plugin.MyLogger.LogMessage("mPneumaticSeatManager.GetInitialisationInfo()");
                return true;
            }
        }

        /// <summary>
        /// Forcing IsInitIsDone to true
        /// </summary>
        [HarmonyPatch(typeof(PneumaticSeatManager), "Init")]
        class Init
        {
            static bool Prefix(PneumaticSeatManager __instance)
            {
                UShoot_Plugin.MyLogger.LogMessage("mPneumaticSeatManager.Init()");
                mset_IsInitDone.Set_IsInitDone(__instance, true);
                //__instance.SkipInitTest();
                //__instance.StartGame();
                return false;
            }
        }

        [HarmonyPatch(typeof(PneumaticSeatManager), "StartInitTest")]
        class StartInitTest
        {
            static bool Prefix(PneumaticSeatManager __instance)
            {
                UShoot_Plugin.MyLogger.LogMessage("mPneumaticSeatManager.StartInitTest()");
                return true;
            }
        }

        [HarmonyPatch(typeof(PneumaticSeatManager), "SkipInitTest")]
        class SkipInitTest
        {
            static bool Prefix(PneumaticSeatManager __instance)
            {
                UShoot_Plugin.MyLogger.LogMessage("mPneumaticSeatManager.SkipInitTest()");
                return true;
            }
        }

        [HarmonyPatch(typeof(PneumaticSeatManager), "Update")]
        class Update
        {
            static bool Prefix()
            {
                //Plugin.myLogger.LogMessage("mPneumaticSeatManager.Update()");
                return true;
            }
        }

        [HarmonyPatch(typeof(PneumaticSeatManager), "get_IsInitDone")]
        class get_IsInitDone
        {
            static bool Prefix(ref bool __result)
            {
                UShoot_Plugin.MyLogger.LogMessage("mPneumaticSeatManager.get_IsInitDone()");
                /*__result = true;
                return false;*/
                return true;
            }

            void postfix(ref bool __result)
            {
                UShoot_Plugin.MyLogger.LogMessage("mPneumaticSeatManager.get_IsInitDone() postfix : __result=" + __result);
                //__result = true;
            }
        }

        #region Dummy Calls

        [HarmonyPatch(typeof(PneumaticSeatManager), "set_IsConnected")]
        class mset_IsConnected
        {
            [HarmonyReversePatch]
            [MethodImpl(MethodImplOptions.NoInlining)]
            public static void Set_IsConnected(object instance, bool value)
            {
                //Used to call the private method    
            }
        }

        [HarmonyPatch(typeof(PneumaticSeatManager), "set_IsInitDone")]
        class mset_IsInitDone
        {
            [HarmonyReversePatch]
            [MethodImpl(MethodImplOptions.NoInlining)]
            public static void Set_IsInitDone(object instance, bool value)
            {
                //Used to call the private method    
            }
        }

        #endregion
    }
}
