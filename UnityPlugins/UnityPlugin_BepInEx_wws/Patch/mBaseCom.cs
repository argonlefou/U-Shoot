using HarmonyLib;

namespace BepInEx_UShoot_Plugin.Patch
{
    class mBaseCom
    {
        /// <summary>
        /// Removing the call to the dog_winfows_ dll causing a crash when it's called with admin rights (netdll broken messagebox)
        /// </summary>
        [HarmonyPatch(typeof(BaseCom), "CheckDog")]
        class CheckDog
        {
            static bool Prefix()
            {
                UShoot_Plugin.MyLogger.LogMessage("BaseCom.CheckDog()");
                return false;
            }
        }
    }
}
