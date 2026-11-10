using BepInEx_UShoot_Plugin;
using HarmonyLib;
using System.IO;

namespace BepInEx_Ushoot_Plugin
{
    class _Unity
    {
        /// <summary>
        /// Change data/save folder location to game folder
        /// </summary>
        [HarmonyPatch(typeof(UnityEngine.Application), "persistentDataPath", MethodType.Getter)]
        class persistentDataPath
        {
            static void Postfix(ref string __result)
            {                
                if (UShoot_Plugin.SaveToGameFolder)
                {
                    UShoot_Plugin.MyLogger.LogMessage("UnityEngine.Application.persistentDataPath: " + __result);
                    __result = Path.Combine(BepInEx.Paths.GameRootPath, "SaveData");
                    UShoot_Plugin.MyLogger.LogMessage("New Path: " + __result);
                }
            }
        }
    }
}
