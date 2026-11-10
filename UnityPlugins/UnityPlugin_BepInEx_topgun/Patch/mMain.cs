using System.Diagnostics;
using System.IO;
using GameBase.Manager;
using HarmonyLib;
using UnityEngine;

namespace BepInEx_USoot_Plugin.Patch
{
    internal class mMain
    {
        /// <summary>
        /// Change Tmp data saving folder to local folder instead of hardcoded D:\Gamedata
        /// </summary>
        [HarmonyPatch(typeof(Main), "InitData")]
        class RecvCoin
        {
            static bool Prefix()
            {
                if (UShoot_Plugin.SaveToGameFolder)
                {
                    string TmpPath = BepInEx.Paths.GameRootPath + @"\NVRAM\gametemp";
                    UShoot_Plugin.MyLogger.LogMessage("Main.InitData(): changing folder to " + TmpPath);
                    int playerNumber = 1;
                    if (!Directory.Exists(TmpPath))
                    {
                        Directory.CreateDirectory(TmpPath);
                    }
                    SingletonComponent<GameManager>.Instance.dataManager.Init(8170, playerNumber, TmpPath, "temp");
                    SingletonComponent<GameManager>.Instance.dataManager.Load();
                    return false;
                }
                return true;
            }
        }

        /// <summary>
        /// Change graphics settings
        /// </summary>
        [HarmonyPatch(typeof(Main), "InitGameSettings")]
        class InitGameSettings
        {
            static bool Prefix(Main __instance)
            {
                Application.targetFrameRate = 50;
                if (UShoot_Plugin.ForceResolution)
                    Screen.SetResolution(UShoot_Plugin.ScreenWidth, UShoot_Plugin.ScreenHeight, UShoot_Plugin.Fullscreen);
                else
                    Screen.SetResolution(1080, 1920, true);
                Cursor.visible = false;
                SingletonComponent<DebugManager>.Instance.open = false;
                SingletonComponent<DebugManager>.Instance.fontSize = 15;
                global::UnityEngine.Object.DontDestroyOnLoad(__instance.gameObject);
                return false;
            }
        }

        /// <summary>
        /// Changing log file path, and skipping that external process that does not stops at the end, keeping a window opended
        /// </summary>
        [HarmonyPatch(typeof(Main), "InitOutputLogger")]
        class InitOutputLogger
        {
            static bool Prefix(Main __instance, Process ___m_process_outputloggerCopy)
            {
                if (UShoot_Plugin.SaveToGameFolder)
                {
                    string LogPath = BepInEx.Paths.GameRootPath + @"\NVRAM\GameLog";
                    UShoot_Plugin.MyLogger.LogMessage("Main.InitOutputLogger(): changing folder to " + LogPath);
                    __instance.outputLogger = new OutputLogger(LogPath, 7);
                    __instance.outputLogger.StartLog(false);
                    /*try
                    {
                        ___m_process_outputloggerCopy = new Process();
                        ___m_process_outputloggerCopy.StartInfo.FileName = Application.streamingAssetsPath + "/BackgroundWin.exe";
                        ___m_process_outputloggerCopy.StartInfo.WorkingDirectory = Application.streamingAssetsPath;
                        ___m_process_outputloggerCopy.Start();
                        DemulShooter_Plugin.OutputLoggerProcess = ___m_process_outputloggerCopy;
                    }
                    catch
                    {
                        UnityEngine.Debug.Log("outputlogger process error");
                    }*/
                }
                else
                {
                    __instance.outputLogger = new OutputLogger("c:/GameLog", 7);
                    __instance.outputLogger.StartLog(false);
                }
                return false;
            }
        }


    }

    
}
