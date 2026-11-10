using HarmonyLib;
using UnityEngine;

namespace BepInEx_UShoot_Plugin.Patch
{
    class mCursorManager
    {
        /// <summary>
        /// GunCorsor Width & Height must be 1920*1080, whatever the screen size is (or crosshair/impact will not be able to cover all screen)
        /// Initialy based on Camera width, but if screen is smaller than 1920p, causing issue
        /// </summary>
        [HarmonyPatch(typeof(CursorManager), "InitGunCursor")]
        class InitGunCursor
        {
            static void Postfix(CursorManager __instance, ref int ___Width, ref int ___Height)
            {
                ___Width = 1920;
                ___Height = 1080;
                UShoot_Plugin.MyLogger.LogMessage("CursorManager.InitGunCursor(): Width=" + ___Width + ", Height=" + ___Height);
            }
        }

        /// <summary>
        /// CursorManager is hardcoded to be displayed in a range 0-1920 and 0-1080
        /// Cursor position is previously computed by the game with some sort of interpolation from calibration, messing the positionning
        /// Restoring good value here before it moves the asset
        /// 
        /// Also, scalling value as it's not made to be resized
        /// </summary>
        [HarmonyPatch(typeof(CursorManager), "UpdateGunCusor")]
        class UpdateGunCusor
        {
            static bool Prefix(CursorManager __instance, Vector2[] ___GunScreenPoint, int ___Width, ref int ___Height)
            {
                /*for (int i = 0; i < MonoSingleton<GameData>.Instance.TotalPlayer; i++)
                {
                    if (!DemulShooter_Plugin.EnableInputHack)
                    {
                        float fx = Input.mousePosition.x * (float)___Width / (float)DemulShooter_Plugin.ScreenWidth;
                        float fy = Input.mousePosition.y * (float)___Height / (float)DemulShooter_Plugin.ScreenHeight;
                        ___GunScreenPoint[i] = new Vector2(fx, fy);
                    }
                    else
                    {
                        Vector3 ScreenPos = DemulShooter_Plugin.PluginControllers[i].GetAimingPosition();
                        float fx = DemulShooter_Plugin.PluginControllers[i].GetAimingPosition().x * (float)___Width / (float)DemulShooter_Plugin.ScreenWidth;
                        float fy = DemulShooter_Plugin.PluginControllers[i].GetAimingPosition().y * (float)___Height / (float)DemulShooter_Plugin.ScreenHeight;
                        ___GunScreenPoint[i] = new Vector2(fx, fy);
                    }    
                }*/
                return true;
            }
        }

        /// <summary>
        /// GunTool.UpdateGunAimScreenPoint() mess with the received data with Calibration and player angle, and call that function by a delagate event
        /// Cleanest hack would be remove the Calibration change in GunTool.UpdateGunAimScreenPoint but I was not able to call the delegate....
        /// So, instead, overwriting values here at each call with "GOOD" coordinates
        /// </summary>
        [HarmonyPatch(typeof(CursorManager), "SetAimScreenPoint")]
        class SetAimScreenPoint
        {
            static bool Prefix(int id, ref Vector2 v2, int ___Width, ref int ___Height)
            {
                float fx = UShoot_Plugin.PlayerControllers[id].GetAimingPosition().x * (float)___Width / (float)UShoot_Plugin.ScreenWidth;
                float fy = UShoot_Plugin.PlayerControllers[id].GetAimingPosition().y * (float)___Height / (float)UShoot_Plugin.ScreenHeight;
                v2 = new Vector2(fx, fy);
                return true;
            }
        }
    }
}
