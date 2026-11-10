using HarmonyLib;
using UnityEngine;
using UnityPlugin_BepInEx.Core;

namespace BepInEx_UShoot_Plugin.Patch.SBK_
{
    class mApplicationManager
    {        
        ///// <summary>
        ///// Replace savepath from %appdata% to local folder
        ///// </summary>
        //[HarmonyPatch(typeof(SBK.ApplicationManager), "GetApplicationPersistentDirectory")]
        //class GetApplicationPersistentDirectory
        //{
        //    static bool Prefix(ref string __result)
        //    {
        //        UShoot_Plugin.MyLogger.LogWarning("--> " + UShoot_Plugin.LocalSave);
        //        if (UShoot_Plugin.LocalSave)
        //        {
        //            __result = BepInEx.Paths.GameRootPath + "/NVRAM/";
        //        }
        //        else
        //        {
        //            __result = UnityEngine.Application.persistentDataPath + "/";
        //        }
        //        return false;
        //    }
        //}

        /// <summary>
        /// Changing Resolution based on the INI file
        /// </summary>
        [HarmonyPatch(typeof(SBK.ApplicationManager), "Start")]
        class start
        {
            static void Postfix()
            {
                if (UShoot_Plugin.ForceResolution)
                    Screen.SetResolution(UShoot_Plugin.ScreenWidth, UShoot_Plugin.ScreenHeight, UShoot_Plugin.Fullscreen);
            }
        }

        /// <summary>
        /// Using this Update loop, running during the whole game, to implement our logic
        /// Usually this is in a custom MonoBehaviour element created with the plugin, but I still have to find how to add one under IL2CPP
        /// </summary>
        [HarmonyPatch(typeof(SBK.ApplicationManager), "Update")]
        class Update
        {
            static bool Prefix()
            {
                UnityEngine.Cursor.visible = false;

                //Auto closing console-debug window
                object developerConsoleVisible = null;
                var mInfo = typeof(UnityEngine.Debug).GetMethod("get_developerConsoleVisible", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
                if (mInfo != null)
                {
                    developerConsoleVisible = mInfo.Invoke(null, null);
                    bool mVisible = (bool)developerConsoleVisible;
                    if (mVisible)
                    {
                        var setConsoleVisible_MethodInfo = typeof(UnityEngine.Debug).GetMethod("set_developerConsoleVisible", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
                        if (setConsoleVisible_MethodInfo != null)
                        {
                            UShoot_Plugin.MyLogger.LogMessage("Debug.set_developerConsoleVisible() found, trying to call it...");
                            setConsoleVisible_MethodInfo.Invoke(null, new object[] { false });
                        }
                        else
                        {
                            UShoot_Plugin.MyLogger.LogError("Debug.ClearDeveloperConsole() NOT found !");
                        }
                    }
                }
                else
                {
                    UShoot_Plugin.MyLogger.LogError("Debug.ClearDeveloperConsole() NOT found !");
                }

                //Updating controls if U-SHoot is not running
                if (!UShoot_Plugin.EnableInputHack)
                {
                    foreach (PluginControllerButton b in UShoot_Plugin.GameController.Buttons)
                    {
                        b.SetRawState(Input.GetKey((KeyCode)b.KeyCode));
                    }

                    for (int i = 0; i < UShoot_Plugin.MAX_PLAYERS; i++)
                    {
                        UShoot_Plugin.PlayerControllers[i].SetAimingValues(Input.mousePosition);
                        UShoot_Plugin.PlayerControllers[i].SetRawButtonState("COIN", Input.GetKey((KeyCode)UShoot_Plugin.PlayerControllers[i].GetButtonKeyCode("COIN")) ? (byte)1 : (byte)0);
                        UShoot_Plugin.PlayerControllers[i].SetRawButtonState("TRIGGER", Input.GetMouseButton(0) ? (byte)1 : (byte)0);
                    }
                }

                //Setting Frame-BAsed buttons status and events
                UShoot_Plugin.GameController.UpdateFrameInputStates();
                for (int i = 0; i < UShoot_Plugin.MAX_PLAYERS; i++)
                {
                    UShoot_Plugin.PlayerControllers[i].UpdateFrameInputStates();
                }

                //Quit
                if (UShoot_Plugin.GameController.GetButtonDown("EXIT"))
                    UnityEngine.Application.Quit();

                //Fetching some outputs
                if (PlayerUtility.GetPlayer(SkyrideUtils.PlayerID.One).IsPlaying())
                    UShoot_Plugin.OutputData.Start_Led[0] = 0;
                else
                    UShoot_Plugin.OutputData.Start_Led[0] = 1;

                if (PlayerUtility.GetPlayer(SkyrideUtils.PlayerID.Two).IsPlaying())
                    UShoot_Plugin.OutputData.Start_Led[1] = 0;
                else
                    UShoot_Plugin.OutputData.Start_Led[1] = 1;

                UShoot_Plugin.OutputData.Credits[0] = (byte)PlayerUtility.GetPlayerCredit(0);
                UShoot_Plugin.OutputData.Credits[1] = (byte)PlayerUtility.GetPlayerCredit(1);

                //Checking for a change in output to send or not
                byte[] bOutputData = UShoot_Plugin.OutputData.ToByteArray();
                byte[] bOutputDataBefore = UShoot_Plugin.OutputDataBefore.ToByteArray();
                for (int i = 0; i < bOutputData.Length; i++)
                {
                    if (bOutputData[i] != bOutputDataBefore[i])
                    {
                        UShoot_Plugin.SendOutputs();
                        break;
                    }
                }

                //Save current state
                UShoot_Plugin.OutputDataBefore.Update(bOutputData);

                return true;                
            }
        }
    }
}
