using HarmonyLib;
using System;

namespace BepInEx_UShoot_Plugin.Patch
{
    class mSecuritySystem
    {
        /// <summary>
        /// CheckKey calls BaseDog.CheckKey()  then Application.Quit()
        /// </summary>
        [HarmonyPatch(typeof(SecuritySystem), "CheckKey")]
        class CheckKey
        {
            static bool Prefix(byte[] key, byte[] key2, BaseDog ___m_Dog, SecuritySystem __instance)
            {
                UShoot_Plugin.MyLogger.LogMessage("SecuritySystem.CheckKey()");
                UShoot_Plugin.SecurityKeyChecked = true;
               __instance.CheckSuccess();

                return false;
            }
        }

        [HarmonyPatch(typeof(SecuritySystem), "CheckFail")]
        class CheckFail
        {
            static bool Prefix()
            {
                UShoot_Plugin.MyLogger.LogMessage("SecuritySystem.CheckFail()");
                UShoot_Plugin.MyLogger.LogMessage(System.Environment.StackTrace);
                return true;
            }
        }

        [HarmonyPatch(typeof(SecuritySystem), "CheckSuccess")]
        class CheckSuccess
        {
            static bool Prefix()
            {
                UShoot_Plugin.MyLogger.LogMessage("SecuritySystem.CheckSuccess()");
                return true;
            }
        }


        /// <summary>
        /// Force OK
        /// Still, displays 0 days Left in menu....
        /// </summary>
        [HarmonyPatch(typeof(SecuritySystem), "ActivateLicense", new Type[] {typeof(string)})]
        class ActivateLicense
        {
            static bool Prefix(string key, ref bool __result)
            {
                UShoot_Plugin.MyLogger.LogMessage("SecuritySystem.ActivateLicense(): key=" + key);
                __result = true;
                return false;
            }
        }
        [HarmonyPatch(typeof(SecuritySystem), "ActivateLicense", new Type[] { typeof(int), typeof(int) })]
        class ActivateLicense2
        {
            static bool Prefix(int requestCode, int respondCode, ref bool __result)
            {
                UShoot_Plugin.MyLogger.LogMessage("SecuritySystem.ActivateLicense(): requestCode=" + requestCode + ", respondCode=" + respondCode);
                __result = true;
                return false;
            }
        }
        [HarmonyPatch(typeof(SecuritySystem), "Check", new Type[] { typeof(byte[]), typeof(byte[]), typeof(Action) })]
        class Check_1
        {
            static bool Prefix(byte[] key, byte[] key2, Action expiredAction, ref bool __result)
            {
                UShoot_Plugin.MyLogger.LogMessage("SecuritySystem.Check_1()");
                __result = true;
                return false;
            }
        }
        [HarmonyPatch(typeof(SecuritySystem), "Check", new Type[] { typeof(byte[]), typeof(byte[]), typeof(string), typeof(string), typeof(Action) })]
        class Check_2
        {
            static bool Prefix(byte[] key, byte[] key2, string hint, string hint2, Action expiredAction, ref bool __result)
            {
                UShoot_Plugin.MyLogger.LogMessage("SecuritySystem.Check_2()");
                __result = true;
                return false;
            }
        }
        [HarmonyPatch(typeof(SecuritySystem), "Check", new Type[] { typeof(byte[]), typeof(byte[]), typeof(string), typeof(string), typeof(int), typeof(Action) })]
        class Check_3
        {
            static bool Prefix(byte[] key, byte[] key2, string hint, string hint2, int limitDay, Action expiredAction, ref bool __result)
            {
                UShoot_Plugin.MyLogger.LogMessage("SecuritySystem.Check_3(): limitDay=" + limitDay);
                __result = true;
                return false;
            }
        }

        [HarmonyPatch(typeof(SecuritySystem), "get_License_EN")]
        class get_License_EN
        {
            static bool Prefix(ref string __result)
            {
                __result = "Official Version";
                return false;
            }
        }

        [HarmonyPatch(typeof(SecuritySystem), "get_License_CN")]
        class get_License_CH
        {
            static bool Prefix(ref string __result)
            {
                __result = "正式版";
                return false;
            }
        }
    }
}
