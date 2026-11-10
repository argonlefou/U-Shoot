using System;
using HarmonyLib;
using Microsoft.Win32;
using UnityEngine;

namespace BepInEx_UShoot_Plugin
{
    class mApplicationManager
    {
        /// <summary>
        /// The original function restarts the game if the fullscreen is disabled
        /// And it's overriding FullScreen option in the registry, forcing it to 1 again
        /// </summary>
        [HarmonyPatch(typeof(SBK.ApplicationManager), "Awake")]
        class Awake
        {
            static void Postfix()
            {
                UShoot_Plugin.MyLogger.LogMessage("mApplicationManager.Awake(), Screen=" + Screen.width + ", " + Screen.height + "," + Screen.fullScreen);
                if (UShoot_Plugin.ForceResolution)
                {
                    Screen.SetResolution(UShoot_Plugin.ScreenWidth, UShoot_Plugin.ScreenHeight, UShoot_Plugin.Fullscreen);
                }
                else
                {
                    try
                    {
                        RegistryKey key = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Sarbakan\\RabbidsShooter");
                        if (key != null)
                        {
                            System.Object o = key.GetValue("Screenmanager Is Fullscreen mode_h3981298716");
                            if (o != null)
                            {
                                if ((int)o == 1)
                                    Screen.SetResolution(Screen.width, Screen.height, true);
                                else if ((int)o == 0)
                                    Screen.SetResolution(Screen.width, Screen.height, false);
                                UShoot_Plugin.MyLogger.LogMessage("mApplicationManager.Awake(), FullScreen now is " + Screen.fullScreen);
                            }
                            else
                            {
                                UShoot_Plugin.MyLogger.LogMessage("mApplicationManager.Awake() : Can't find Registry Value : HK_CURRENT_USER\\SOFTWARE\\Sarbakan\\RabbidsShooter\\Screenmanager Is Fullscreen mode_h3981298716");
                            }
                        }
                        else
                        {
                            UShoot_Plugin.MyLogger.LogMessage("mApplicationManager.Awake() : Can't find Registry Key : HK_CURRENT_USER\\SOFTWARE\\Sarbakan\\RabbidsShooter");
                        }
                    }
                    catch (Exception Ex)
                    {
                        UShoot_Plugin.MyLogger.LogMessage("mApplicationManager.Awake() : Error reading fullscreen registry value. " + Ex.Message.ToString());
                    }
                }
            }
        }

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
