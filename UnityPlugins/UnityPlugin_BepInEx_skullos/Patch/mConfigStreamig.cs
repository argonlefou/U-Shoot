using BepInEx_UShoot_Plugin;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Top.Modul.Uart;
using UnityEngine;

namespace BepInEx_UShoot_Plugin.Patch
{
    public class mConfigStreamig
    {
        /// <summary>
        /// Forcing use of local Data folder is UShoot plugin option is set
        /// If not, game is loooking for E: to save
        /// </summary>
        [HarmonyPatch(typeof(ConfigStreaming), "PathData")]
        class PathData
        {
            static bool Prefix(ref string __result)
            {
                UShoot_Plugin.MyLogger.LogMessage("ConfigStreaming.PathData()");

                string text = "E:/Tools";
                string text2 = "TopData.db";

                if (UShoot_Plugin.LocalSave)
                {
                    text = Application.dataPath + "/" + text2;
                }
                //Original code:
                else
                {
                    if (Directory.Exists(text))
                    {
                        text = text + "/" + text2;
                    }
                    else
                    {
                        text = Application.dataPath + "/" + text2;
                    }
                }
                __result = text;
                return false;
            }
        }
    }
}
