using HarmonyLib;

namespace BepInEx_DemulShooter_Plugin.Patch
{
    internal class _Unity
    {
        /// <summary>
        /// Change data/save folder location to game folder
        /// </summary>
        [HarmonyPatch(typeof(UnityEngine.Application), "persistentDataPath", MethodType.Getter)]
        class persistentDataPath
        {
            static void Postfix(ref string __result)
            {                
                if (DemulShooter_Plugin.SaveToGameFolder)
                {
                    DemulShooter_Plugin.MyLogger.LogMessage("UnityEngine.Application.persistentDataPath: " + __result);
                    __result = BepInEx.Paths.GameRootPath + "/NVRAM";
                }
            }
        }
    }
}
