using HarmonyLib;

namespace BepInEx_UShoot_Plugin
{
    class UnityDebug
    {
        /// <summary>
        /// Forcing mouse cursor ON to check aim
        /// </summary>
        [HarmonyPatch(typeof(UnityEngine.Cursor), "set_visible")]
        class Cursor_SetVisible
        {
            static bool Prefix(ref bool value)
            {
                //value = true;
                return true;
            }
        }
    }
}
