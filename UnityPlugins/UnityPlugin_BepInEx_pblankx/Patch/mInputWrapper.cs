using HarmonyLib;
using UnityEngine;

namespace BepInEx_UShoot_Plugin
{
    class mInputWrapper
    {
        /// <summary>
        /// Force Credits key Handling
        /// </summary>
        [HarmonyPatch(typeof(InputWrapper), "isCreditAdded")]
        class isCreditAdded
        {
            static bool Prefix(ref bool __result)
            {
                //UShoot_Plugin.MyLogger.LogMessage("InputWrapper.isCreditAdded()");
                __result = UShoot_Plugin.GameController.GetButtonDown("COIN");
                return false;
            }
        }

        /// <summary>
        /// Force Start Keys handling
        /// </summary>
        //[HarmonyPatch(typeof(InputWrapper), "isStartPressed")]
        //class isStartPressed
        //{
        //    static bool Prefix(string playerName, ref bool __result)
        //    {
        //        __result = false;
        //        //DemulShooter_Plugin.MyLogger.LogWarning("InputWrapper.isStartPressed() => " + playerName);

        //        if (playerName.Equals("P1") && DemulShooter_Plugin.PluginControllers[0].GetButtonDown(UnityPlugin_BepInEx_Core.PluginController.MyInputButtons.Start))
        //        {
        //            DemulShooter_Plugin.MyLogger.LogWarning("InputWrapper.isStartPressed() => P1 Start Detected");
        //            __result = true;
        //            return false;
        //        }

        //        if (playerName.Equals("P2") && DemulShooter_Plugin.PluginControllers[1].GetButtonDown(UnityPlugin_BepInEx_Core.PluginController.MyInputButtons.Start))
        //        {
        //            DemulShooter_Plugin.MyLogger.LogWarning("InputWrapper.isStartPressed() => P2 start Detected");
        //            __result = true;
        //            return false;
        //        }

        //        /*for (int i = 0; i < DemulShooter_Plugin.MAX_PLAYERS; i++)
        //        {
        //            if (playerName.Equals("P" + (i + 1).ToString()) && DemulShooter_Plugin.PluginControllers[i].GetButtonDown(UnityPlugin_BepInEx_Core.PluginController.MyInputButtons.Start))
        //            {
        //                DemulShooter_Plugin.MyLogger.LogWarning("InputWrapper.isStartPressed() => P" + (i + 1).ToString() + " Start Detected");
        //                __result = true;
        //                return false;
        //            }
        //        }*/
        //        return false;
        //    }
        //}

        /// <summary>
        /// Feeding gun trigger with DemulShooter
        /// </summary>
        [HarmonyPatch(typeof(InputWrapper), "isShooting")]
        class isShooting
        {
            static bool Prefix(string playerName, ref int ___p1ShootFrameCount, ref int ___p2ShootFrameCount, ref float ___p1ShootHoldTime, ref float ___p2ShootHoldTime, float ___rapidFireShootInterval, ref bool __result, bool allowRapidFire = false)
            {
                //UShoot.MyLogger.LogWarning("InputWrapper.isShooting() => Player: " + playerName + "allowRapidFire: " + allowRapidFire);
                if (playerName == "P1")
                {
                    if (!UShoot_Plugin.PlayerControllers[0].GetButton("TRIGGER"))
                    {
                        ___p1ShootFrameCount = 0;
                        ___p1ShootHoldTime = 0f;
                        __result = false;
                        return false;
                    }
                    ___p1ShootFrameCount++;
                    ___p1ShootHoldTime += Time.deltaTime;
                    if (allowRapidFire)
                    {
                        bool flag3 = false;
                        if (___p1ShootHoldTime > ___rapidFireShootInterval)
                        {
                            flag3 = true;
                            ___p1ShootHoldTime -= ___rapidFireShootInterval;
                        }
                        __result = ___p1ShootFrameCount == 1 || flag3;
                        return false;
                    }
                    __result = ___p1ShootFrameCount == 1;
                    return false;
                }
                else
                {
                    if (!UShoot_Plugin.PlayerControllers[1].GetButton("TRIGGER"))
                    {
                        ___p2ShootFrameCount = 0;
                        ___p2ShootHoldTime = 0f;
                        __result = false;
                        return false;
                    }
                    ___p2ShootFrameCount++;
                    ___p2ShootHoldTime += Time.deltaTime;
                    if (allowRapidFire)
                    {
                        bool flag4 = false;
                        if (___p2ShootHoldTime > ___rapidFireShootInterval)
                        {
                            flag4 = true;
                            ___p2ShootHoldTime -= ___rapidFireShootInterval;
                        }
                        __result = ___p2ShootFrameCount == 1 || flag4;
                        return false;
                    }
                    __result = ___p2ShootFrameCount == 1;
                    return false;
                }
            }
        }

        /// <summary>
        /// Feeding gun position
        ///
        ///Coordinates goes from [0.0, 0.0] in bottom left corner to [1.0, 1.0] in upper right when the game is on a 16/9 display ration
        ///If the ratio is different, the Y value must go over 1.0 or else there will be some offset
        /// </summary>
        [HarmonyPatch(typeof(InputWrapper), "getPositionOnScreen")]
        class getPositionOnScreen
        {
            static bool Prefix(string playerName, ref Vector2 __result)
            {
                Vector2 v = new Vector2();
                if (playerName.Equals("P1"))
                {
                    v = UShoot_Plugin.PlayerControllers[0].GetAimingPosition();
                }
                else if (playerName.Equals("P2"))
                {
                    v = UShoot_Plugin.PlayerControllers[1].GetAimingPosition();
                }

                float Y_Max = (16.0f / 9.0f) / ((float)Screen.width / (float)Screen.height);
                __result.x = v.x / (float)Screen.width;
                __result.y = v.y / (float)Screen.height * Y_Max;

                return false;
            }
        }
        
    }
}
