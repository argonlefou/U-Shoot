using HarmonyLib;

namespace BepInEx_UShoot_Plugin.Patch
{
    internal class mGlobal
    {
        /// <summary>
        /// Using this to get Damage output
        /// </summary>
        [HarmonyPatch(typeof(Global), "AddDamage")]
        class AddDamage
        {
            static bool Prefix(int index, float dam)
            {
                if (!Global.IsPlayerAI(index))
                    UShoot_Plugin.OutputData.Damaged[index] = 1;
                return true;
            }
        }
    }
}
