using GUNSDK;
using HarmonyLib;
using System.Collections;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BepInEx_UShoot_Plugin.Patch
{
    class mInitUI
    {
        /// <summary>
        /// Copying initialisation code and removing references to Arcadecontrols lib
        /// </summary>
        [HarmonyPatch(typeof(InitUI), "Start")]
        class Start
        {
            static bool Prefix(ref IEnumerator __result)
            {
                UShoot_Plugin.MyLogger.LogMessage("InitUI.Start()");
                __result = mEnumerator();
                return false;
            }

            private static IEnumerator mEnumerator()
            {
                if (!UShoot_Plugin.Initialized)
                {
                    UShoot_Plugin.Initialized = true;
                    Cursor.visible = false;
                    GameData.GamePlayerCount = (MaxPlayer)UShoot_Plugin.PlayerCount;
                    GameData.KeyBoard = false;
                    GameData.OutTicketFlag = false;
                    GameData.CheckConnect = false;
                    Application.runInBackground = true;
                    if (UShoot_Plugin.ForceResolution)
                        Screen.SetResolution(UShoot_Plugin.ScreenWidth, UShoot_Plugin.ScreenHeight, UShoot_Plugin.Fullscreen);                 
                }
                BaseGun gun = BaseGun.Instance;
                MVConfig.Instance.GUN_MODEL = ((GameData.GamePlayerCount == MaxPlayer.Two) ? GunType.Two : GunType.Four);
                gun.FrameData = new BaseData[4];
                for (int i = 0; i < 4; i++)
                {
                    gun.FrameData[i] = new BaseData(1, 1);
                }
                typeof(BaseGun).GetField("States", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(gun, new bool[4]);
                InputController controller = InputController.Instance;

                yield return null;

                AudioListener.volume = (float)GameData.SoundVolume * 0.1f;
                SceneManager.LoadScene(GameData.Begin);
                yield break;
            }
        }
    }
}
