using BepInEx_UShoot_Plugin;
using HarmonyLib;
using MRCQ;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BepInEx_UShoot_Plugin.Patch
{
    class _Unity
    {
        /// axisName is "x01" ... "x06" and "y01" ... "y06"
        /// X value is 0.0f (left) to 1.0f (right)
        /// Y value is 0.0f (Bottom) to 1.0f (top)
        /// </summary>
        [HarmonyPatch(typeof(UnityEngine.Input), "GetKey", new Type[] { typeof(string) })]
        class GetKey
        {
            static bool Prefix(string name, ref bool __result)
            {
                switch (name)
                {
                    case "5":
                        __result = UShoot_Plugin.PlayerControllers[0].GetButton("COIN"); return false;
                    case "6":
                        __result = UShoot_Plugin.PlayerControllers[1].GetButton("COIN"); return false;
                    case "7":
                        __result = UShoot_Plugin.PlayerControllers[2].GetButton("COIN"); return false;
                    case "8":
                        __result = UShoot_Plugin.PlayerControllers[3].GetButton("COIN"); return false;

                    case "space":
                        __result = UShoot_Plugin.GameController.GetButton("TEST"); return false;
                    case "up":
                        __result = UShoot_Plugin.GameController.GetButton("MENU_UP"); return false;
                    case "down":
                        __result = UShoot_Plugin.GameController.GetButton("MENU_DOWN"); return false;
                    case "left":
                        __result = UShoot_Plugin.GameController.GetButton("MENU_LEFT"); return false;
                    case "right":
                        __result = UShoot_Plugin.GameController.GetButton("MENU_RIGHT"); return false;

                    default:
                        return true;
                }
            }
        }
    }
}
