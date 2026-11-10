using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BepInEx_UShoot_Plugin.Patch
{
    internal class _Unity
    {
        // <summary>
        /// Change data/save folder location to game folder
        /// </summary>
        [HarmonyPatch(typeof(UnityEngine.Application), "persistentDataPath", MethodType.Getter)]
        class persistentDataPath
        {
            static void Postfix(ref string __result)
            {
                UShoot_Plugin.MyLogger.LogMessage("UnityEngine.Application.persistentDataPath: " + __result);
                UShoot_Plugin.PrintStackTrace();
                //if (UShoot_Plugin.SaveToGameFolder)
                //{
                //    UShoot_Plugin.MyLogger.LogMessage("UnityEngine.Application.persistentDataPath: " + __result);
                //    __result = BepInEx.Paths.GameRootPath + "/NVRAM";
                //}
            }
        }

        [HarmonyPatch(typeof(UnityEngine.Screen), "SetResolution", new System.Type[] { typeof(int), typeof(int), typeof(bool) })]
        class SetResolution1
        {
            static bool Prefix(ref int width, ref int height, ref bool fullscreen)
            {
                UShoot_Plugin.MyLogger.LogWarning("UnityEngine.Screen.SetResolution(" + width + ", " + height + ", " + fullscreen + ")");
                UShoot_Plugin.PrintStackTrace();
                /*width = 800;
                height = 600;
                fullscreen = false;*/
                return true;
            }
        }
        [HarmonyPatch(typeof(UnityEngine.Screen), "SetResolution", new System.Type[] { typeof(int), typeof(int), typeof(bool), typeof(int) })]
        class SetResolution2
        {
            static bool Prefix(ref int width, ref int height, ref bool fullscreen, ref int preferredRefreshRate)
            {
                UShoot_Plugin.MyLogger.LogWarning("UnityEngine.Screen.SetResolution(" + width + ", " + height + ", " + fullscreen + ", " + preferredRefreshRate + ")");
                UShoot_Plugin.PrintStackTrace();
                /*width = 800;
                height = 600;
                fullscreen = false;*/
                //preferredRefreshRate = 30;
                return true;
            }
        }
    }
}
