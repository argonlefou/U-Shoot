using HarmonyLib;

namespace BepInEx_UShoot_Plugin.Patch
{
    public class mApplicationManager
    {
        /// <summary>
        /// Replace savepath from %appdata% to local folder
        /// </summary>
        [HarmonyPatch(typeof(SBK.ApplicationManager), "GetApplicationPersistentDirectory")]
        class GetApplicationPersistentDirectory
        {
            static bool Prefix(ref string __result)
            {
                if (UShoot_Plugin.LocalSave)
                {
                    __result = BepInEx.Paths.GameRootPath + "/NVRAM/";
                }
                else
                {
                    __result = UnityEngine.Application.persistentDataPath + "/";
                }
                return false;
            }
        }
    }
}
