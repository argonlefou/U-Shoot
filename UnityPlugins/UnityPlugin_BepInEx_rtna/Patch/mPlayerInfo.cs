using HarmonyLib;

namespace BepInEx_UShoot_Plugin
{
    class mPlayerInfo
    {
        /// <summary>
        /// Creating recoil data
        /// </summary>
        [HarmonyPatch(typeof(PlayerInfo), "ShotFired")]
        class ShotFired
        {
            static bool Prefix(int ___m_playerIndex)
            {
                UShoot_Plugin.OutputData.Recoil[___m_playerIndex] = 1;
                return true;
            }
        }
    }
}
