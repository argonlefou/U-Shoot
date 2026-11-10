using HarmonyLib;

namespace BepInEx_UShoot_Plugin.Patch
{
    class mSetMousecursor
    {
        /// <summary>
        /// Remove crosshair
        /// </summary>
        [HarmonyPatch(typeof(SetMouseCursor), "ShowCursor")]
        class ShowCursor
        {
            static bool Prefix()
            {
                UShoot_Plugin.MyLogger.LogWarning("SetMousecursor.ShowCursor()");
                if (UShoot_Plugin.CrossHairVisibility == false)
                    UnityEngine.Cursor.visible = false;
                else
                    UnityEngine.Cursor.visible = true;
                return false;
            }
        }

        [HarmonyPatch(typeof(SetMouseCursor), "SetCursor")]
        class SetCursor
        {
            static bool Prefix(ref int cursorNumber)
            {
                UShoot_Plugin.MyLogger.LogWarning("SetMousecursor.SetCursor() => cursorNumber=" + cursorNumber);
                if (UShoot_Plugin.CrossHairVisibility == false)
                    cursorNumber = -1;  //-1 makes the original function call HideCursor()
                return true;
            }
        }
    }
}
