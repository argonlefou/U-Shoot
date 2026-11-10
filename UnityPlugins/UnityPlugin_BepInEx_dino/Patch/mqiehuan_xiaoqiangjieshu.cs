using HarmonyLib;

namespace BepInEx_UShoot_Plugin.Patch
{
    internal class mqiehuan_xiaoqiangjieshu
    {
        /// <summary>
        /// Replace the original [J] key to enter TEST menu
        /// </summary>
        [HarmonyPatch(typeof(qiehuan_xiaoqiangjieshu), "Update")]
        class Update
        {
            static bool Prefix(qiehuan_xiaoqiangjieshu __instance)
            {
               if (UShoot_Plugin.GameController.GetRawButtonDown("TEST"))
                    __instance.normal_control_work();
                return false;
            }
        }
    }
}
