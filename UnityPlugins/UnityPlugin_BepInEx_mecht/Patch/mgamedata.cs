using HarmonyLib;

namespace BepInEx_UShoot_Plugin.Patch
{
    class mgamedata
    {
        /// <summary>
        /// Force player number
        /// </summary>
        [HarmonyPatch(typeof(gamedata), "Awake")]
        class Awake
        {
            static void Postfix()
            {
                UShoot_Plugin.MyLogger.LogMessage("gamedata.Awake()");
                UIback.playernum = UShoot_Plugin.PlayerNum;
            }
        }
    }
}
