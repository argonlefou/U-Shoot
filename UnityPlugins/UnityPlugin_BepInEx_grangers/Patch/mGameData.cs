using HarmonyLib;

namespace BepInEx_UShoot_Plugin.Patch
{
    class mGameData
    {
        /// <summary>
        /// Remove IO check at start
        /// </summary>
        [HarmonyPatch(typeof(GameData), "DefaultUserData")]
        class DefaultUserData
        {
            static void Postfix()
            {
                UShoot_Plugin.MyLogger.LogWarning("GameData.DefaultUserData:");
            }
        }

        [HarmonyPatch(typeof(GameData), "LoadUserData")]
        class LoadUserData
        {
            static void Postfix()
            {
                UShoot_Plugin.MyLogger.LogMessage("GameData.LoadUserData: ");
                global::GameData.Language = UShoot_Plugin.Language;
            }
        }

        [HarmonyPatch(typeof(GameData), "SaveUserData")]
        class SaveUserData
        {
            static void Prefix()
            {
                UShoot_Plugin.MyLogger.LogMessage("GameData.SaveUserData: ");
            }
        }
    }
}
