using HarmonyLib;
using Virtuallyz.VRShooter;

namespace BepInEx_UShoot_Plugin
{
    class mGameManager
    {
        /// <summary>
        /// On LevelStopped (quit or died) => set Ammo and Life to 0
        /// </summary>
        [HarmonyPatch(typeof(GameManager), "OnLevelActStopped")]
        class Update
        {
            static bool Prefix()
            {
                UShoot_Plugin.MyLogger.LogWarning("Virtuallyz.VRShooter.GameManager.OnLevelActStopped()");
                for (int i = 0; i < UShoot_Plugin.MAX_PLAYERS; i++)
                {
                    UShoot_Plugin.OutputData.Life[i] = 0;
                    UShoot_Plugin.OutputData.Ammo[i] = 0; 
                }

                return true;
            }

        }

        [HarmonyPatch(typeof(GameManager), "OnEnable")]
        class OnEnable
        {
            static bool Prefix()
            {
                UShoot_Plugin.MyLogger.LogWarning("Virtuallyz.VRShooter.GameManager.OnEnable()");
                UnityEngine.Cursor.visible = false;

                return true;
            }

        }
    }
}
