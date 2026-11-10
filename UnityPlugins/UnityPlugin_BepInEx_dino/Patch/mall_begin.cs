using HarmonyLib;

namespace BepInEx_UShoot_Plugin.Patch
{
    class mall_begin
    {
        /// <summary>
        /// Force MOUSE controls
        /// Not used ???
        /// </summary>
        [HarmonyPatch(typeof(all_begin), "all_begin_work")]
        class all_begin_work
        {
            static void Postfix()
            {
                all_begin.mycontrol_way = all_begin.CONTROL_WAY.MOUSE;
            }
        }
    }
}
