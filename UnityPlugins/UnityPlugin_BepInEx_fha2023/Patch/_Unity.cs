using System;
using HarmonyLib;
using UnityEngine;

namespace BepInEx_UShoot_Plugin.Patch
{
    public class _Unity
    {
        /// <summary>
        /// Blocking all calls from out of the UShoot Plugin
        /// </summary>
        [HarmonyPatch(typeof(UnityEngine.Input), "GetKey", new Type[] { typeof(UnityEngine.KeyCode) })]
        class GetKey
        {
            static bool Prefix(ref UnityEngine.KeyCode key, ref bool __result)
            {
                string[] buffer = System.Environment.StackTrace.Split('\n');
                if (!buffer[buffer.Length - 1].Contains("UShoot_Plugin.Update"))
                {
                    return false;
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(UnityEngine.Input), "GetKeyDown", new Type[] { typeof(UnityEngine.KeyCode) })]
        class GetKeyDown
        {
            static bool Prefix(ref UnityEngine.KeyCode key, ref bool __result)
            {
                string[] buffer = System.Environment.StackTrace.Split('\n');
                if (!buffer[buffer.Length - 1].Contains("UShoot_Plugin.Update"))
                {
                    return false;
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(UnityEngine.Input), "GetKeyUp", new Type[] { typeof(UnityEngine.KeyCode) })]
        class GetKeyUp
        {
            static bool Prefix(ref UnityEngine.KeyCode key, ref bool __result)
            {
                string[] buffer = System.Environment.StackTrace.Split('\n');
                if (!buffer[buffer.Length - 1].Contains("UShoot_Plugin.Update"))
                {
                    return false;
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(UnityEngine.Input), "GetMouseButton")]
        class GetMouseButton
        {
            static bool Prefix(int button, ref bool __result)
            {
                string[] buffer = System.Environment.StackTrace.Split('\n');
                if (!buffer[buffer.Length - 1].Contains("UShoot_Plugin.Update"))
                {
                    return false;
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(UnityEngine.Input), "GetMouseButtonDown")]
        class GetMouseButtonDown
        {
            static bool Prefix(int button, ref bool __result)
            {
                string[] buffer = System.Environment.StackTrace.Split('\n');
                if (!buffer[buffer.Length - 1].Contains("UShoot_Plugin.Update"))
                {
                    return false;
                }
                return true;
            }
        }
    }
}
