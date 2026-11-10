using System.Runtime.CompilerServices;
using HarmonyLib;
using UnityEngine;

namespace BepInEx_UShoot_Plugin.Patch
{
    internal class mChooseLevelWindow
    {
        /// <summary>
        /// To select a level, look like 2 conditions are needed :
        /// - 1) The LevelSelectorPanel must be highlighted
        /// - 2) The WindowTurretfired method must be called with Vector2 axis data corresponding to the Panel location highlighted
        /// That way P1 can only choose the level pointed by him, and not validate a level pointed by P2
        /// 
        /// Calling LevelSlectorPanel.LevelSelected directly select the Level.but we need to find how to get the good one fired at
        /// 
        /// THe LevelSelectorPanel.WindowTurretFired() has a Vector2 parameter, and coordinates are not in -1/+1 range :
        /// It's corrsponding to each LevelSelectorPanel "position" (in Unity Explorer) referential, not the screen or world
        /// 
        /// Looks like the position we want is between those bounds :
        /// -8.0 ; -5.0 for lower left
        /// +8.0 ; +5.0 for upper right
        /// Should be easy to get from the -1.0/ 1.0 axis we have with demulshooter
        /// </summary>
        [HarmonyPatch(typeof(ChooseLevelWindow), "Update")]
        class LevelUpdate
        {
            static bool Prefix(ChooseLevelWindow __instance)
            {
                //Get the IsLevelChosen flag for later use
                UShoot_Plugin.IsLevelChosen = mget_IsLevelChosen.get_IsLevelChosen(__instance);

                Il2CppSystem.Collections.Generic.List<LevelSelectorPanel> Panels = mget_panelList.Get_panelList(__instance);
                if (Panels != null && Panels.Count > 0)
                {
                    for (int i = 0; i < UShoot_Plugin.MAX_PLAYERS; i++)
                    {
                        if (UShoot_Plugin.PlayerControllers[i].GetButtonDown("TRIGGER"))
                        {
                            UShoot_Plugin.MyLogger.LogMessage("ChooseLevelWindow.Update() : TRIGGER detected for Player" + (i+1).ToString());
                            foreach (LevelSelectorPanel p in Panels)
                            {
                                Vector2 v = UShoot_Plugin.PlayerControllers[i].GetAimingPosition();
                                v.x = (2.0f * v.x /(float)Screen.width) - 1.0f;
                                v.y = (2.0f * v.y / (float)Screen.height) - 1.0f;
                                UShoot_Plugin.MyLogger.LogMessage("Player" + (i + 1).ToString() + " : " + v.ToString());

                                var methodInfo = typeof(LevelSelectorPanel).GetMethod("WindowTurretFired");
                                /// Convert [-1.0; +1.0] range Gun value to [-8.0, +8.0] for X and [-5.0, +5.0] for Y
                                Vector2 VWorldPos = new Vector2(v.x * 8, v.y * 5);
                                methodInfo.Invoke(p, new object[] { (SkyrideUtils.PlayerID)i, VWorldPos });
                            }
                        }
                    }
                }
                return true;
            }
        }

        #region Dummy Calls

        [HarmonyPatch(typeof(ChooseLevelWindow), "get_IsLevelChosen")]
        class mget_IsLevelChosen
        {
            [HarmonyReversePatch]
            [MethodImpl(MethodImplOptions.NoInlining)]
            public static bool get_IsLevelChosen(object instance)
            {
                //Used to call the private method
                return false;
            }
        }

        [HarmonyPatch(typeof(ChooseLevelWindow), "get_panelList")]
        class mget_panelList
        {
            [HarmonyReversePatch]
            [MethodImpl(MethodImplOptions.NoInlining)]
            public static Il2CppSystem.Collections.Generic.List<LevelSelectorPanel> Get_panelList(object instance)
            {
                //Used to call the private method
                return null;
            }
        }

        #endregion
    }
}
