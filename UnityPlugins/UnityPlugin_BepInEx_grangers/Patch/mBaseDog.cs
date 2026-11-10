using HarmonyLib;
using System;

namespace BepInEx_UShoot_Plugin.Patch
{
    internal class mBaseDog
    {
        /// <summary>
        /// Forcing check OK when it's called during gameplay
        /// </summary>
        [HarmonyPatch(typeof(BaseDog), "CheckDog", new Type[] { typeof(byte[]), typeof(byte[])})]
        class CheckDog_1
        {
            static bool Prefix(byte[] key, byte[] key2, BaseDog __instance)
            {
                UShoot_Plugin.MyLogger.LogMessage("BaseDog.CheckDog(byte[], byte[])");
                __instance.CheckSuccess();
                return false;
            }
        }

        /// <summary>
        /// Forcing check OK when it's called during gameplay
        /// </summary>
        [HarmonyPatch(typeof(BaseDog), "CheckDog", new Type[] { typeof(byte[]), typeof(byte[]), typeof(string), typeof(string), typeof(int), typeof(Action) })]
        class CheckDog_2
        {
            static bool Prefix(byte[] key, byte[] key2, string hint, string hint2, int limitDay, Action expiredAction, BaseDog __instance)
            {
                UShoot_Plugin.MyLogger.LogMessage("BaseDog.CheckDog(byte[], byte[], string, string, int, Action)");
                __instance.CheckSuccess();
                return false;
            }
        }

        [HarmonyPatch(typeof(BaseDog), "CloseApp")]
        class CloseApp
        {
            static bool Prefix()
            {
                UShoot_Plugin.MyLogger.LogMessage("BaseDog.CloseApp()");
                return false;
            }
        }


        /// <summary>
        /// These are the steps when Success() is called
        /// SaveTempData will throw a log error, but skipping it will make some registration screen appear
        /// </summary>
        [HarmonyPatch(typeof(BaseDog), "CheckSuccess")]
        class CheckSuccess
        {
            static bool Prefix()
            {
                UShoot_Plugin.MyLogger.LogMessage("BaseDog.CheckSuccess()");
                return true;
            }
        }

        [HarmonyPatch(typeof(BaseDog), "SaveCheckTimes")]
        class SaveCheckTimes
        {
            static bool Prefix(int times)
            {
                UShoot_Plugin.MyLogger.LogMessage("BaseDog.SaveCheckTimes(): times=" + times);
                return true;
            }
        }

        [HarmonyPatch(typeof(BaseDog), "SaveTempData")]
        class SaveTempData
        {
            static bool Prefix(string key, string value)
            {
                UShoot_Plugin.MyLogger.LogMessage("BaseDog.SaveTempData(): key=" + key + ", value=" + value);
                return true;
            }
        }

        [HarmonyPatch(typeof(BaseDog), "get_ExpiryDay")]
        class get_ExpiryDay
        {
            static bool Prefix(ref int __result)
            {
                UShoot_Plugin.MyLogger.LogMessage("BaseDog.get_ExpiryDay(): " + __result);
                __result = -1;
                return false;
            }
        }
        [HarmonyPatch(typeof(BaseDog), "get_InValid")]
        class get_InValid
        {
            static bool Prefix(ref bool __result)
            {
                UShoot_Plugin.MyLogger.LogMessage("BaseDog.get_InValid(): " + __result);
                return true;
            }
        }
        [HarmonyPatch(typeof(BaseDog), "get_IsOfficial")]
        class get_IsOfficial
        {
            static bool Prefix(ref bool __result)
            {
                UShoot_Plugin.MyLogger.LogMessage("BaseDog.get_IsOfficial(): " + __result);
                __result = true;
                return false;
            }
        }
        [HarmonyPatch(typeof(BaseDog), "get_IsTrial")]
        class get_IsTrial
        {
            static bool Prefix(ref bool __result)
            {
                UShoot_Plugin.MyLogger.LogMessage("BaseDog.get_IsTrial(): " + __result);
                return true;
            }
        }


        [HarmonyPatch(typeof(BaseDog), "set_IsOfficial")]
        class set_IsOfficial
        {
            static bool Prefix(ref bool value)
            {
                UShoot_Plugin.MyLogger.LogMessage("BaseDog.get_IsOfficial(): " + value);
                value = true;
                UShoot_Plugin.MyLogger.LogMessage(System.Environment.StackTrace);
                return true;
            }
        }
        [HarmonyPatch(typeof(BaseDog), "set_ExpiryDay")]
        class set_ExpiryDay
        {
            static bool Prefix(ref int value)
            {
                UShoot_Plugin.MyLogger.LogMessage("BaseDog.get_ExpiryDay(): " + value);
                value = -1;
                UShoot_Plugin.MyLogger.LogMessage(System.Environment.StackTrace);
                return true;
            }
        }

        [HarmonyPatch(typeof(BaseDog), "CheckFile")]
        class CheckFile
        {
            static bool Prefix(BaseDog __instance)
            {
                UShoot_Plugin.MyLogger.LogMessage("BaseDog.CheckFile)");
                //__instance.CheckSuccess();
                return false;
            }
        }
    }
}
