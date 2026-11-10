using HarmonyLib;
using Top.Modul.Menu;

namespace BepInEx_UShoot_Plugin.Patch
{
    internal class mMenu
    {
        /// <summary>
        /// Forcing English language
        /// </summary>
        [HarmonyPatch(typeof(Menu), "GetLanguage")]
        class GetLanguage
        {
            static void Postfix(ref int __result)
            {
                if (UShoot_Plugin.Language.Equals("EN"))
                    __result = 0;
                else if (UShoot_Plugin.Language.Equals("CH"))
                    __result = 1;
            }
        }
    }
}
