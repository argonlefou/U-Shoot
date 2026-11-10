using HarmonyLib;

namespace BepInEx_UShoot_Plugin.Patch
{
    internal class mInputController
    {
        /// <summary>
        /// Remove Keyboard/Mouse enable
        /// </summary>
        [HarmonyPatch(typeof(InputController), "Update")]
        class Update
        {
            static bool Prefix()
            {
                return false;
            }
        }

    }
}
