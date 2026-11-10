using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;

namespace BepInEx_UShoot_Plugin
{
    class mUnityEngine
    {       
        /// <summary>
        /// Intercepting Input.GetKeydown() comming from InputCtrl.Update() to change default hardcoded Keys
        /// Set to usual controls
        /// Keeping [F] for FPS display and [C] to on/off mouse cursor
        /// </summary>
        [HarmonyPatch(typeof(UnityEngine.Input), "GetKeyDown", new Type[] { typeof(UnityEngine.KeyCode) })]
        class GetKeyDown
        {
            static bool Prefix(ref UnityEngine.KeyCode key, ref bool __result)
            {
                //DemulShooter_Plugin.MyLogger.LogWarning("GetKeyDown: key=" + key.ToString());
                //DemulShooter_Plugin.MyLogger.LogWarning(System.Environment.StackTrace);
                /*string[] buffer = System.Environment.StackTrace.Split('\n');                
                if (buffer[buffer.Length - 1].Contains("InputCtrl"))
                {
                    //DemulShooter_Plugin.MyLogger.LogMessage(buffer[buffer.Length - 1]);
                    if (key == KeyCode.Alpha3 || key == KeyCode.Alpha2 || key == KeyCode.Alpha9 || key == KeyCode.Alpha4 || key == KeyCode.P)
                    {
                        __result = false;
                        return false;
                    }
                    switch (key)
                    {
                        case KeyCode.Alpha1: key = KeyCode.Alpha5; break;
                        case KeyCode.A: key = KeyCode.LeftArrow; break;
                        case KeyCode.S: key = KeyCode.Alpha1; break;
                        case KeyCode.D: key = KeyCode.RightArrow; break;
                        case KeyCode.U: key = KeyCode.Alpha0; break;
                        case KeyCode.Y: key = KeyCode.Alpha9; break;
                        //default: DemulShooter_Plugin.MyLogger.LogMessage(key.ToString()); break;
                    }
                }*/
                return true;
            }
        }
    }
}
