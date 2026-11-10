using HarmonyLib;

namespace BepInEx_UShoot_Plugin.Patch
{
    internal class mUIHurtScar_Player
    {
        /// <summary>
        /// Intercept call to get "Damaged" player if it returns true
        /// </summary>
        [HarmonyPatch(typeof(UIHurtScar_Player), "UpdateHurt")]
        class UpdateHurt
        {            
            static void Postfix(bool Hard, UIHurtScar_Player __instance, bool __result)
            {
                if (__result)
                    UShoot_Plugin.OutputData.Damaged[__instance.Data.PlayerID] = 1;
            }
        }
    }
}
