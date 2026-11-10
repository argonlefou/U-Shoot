using HarmonyLib;

namespace BepInEx_UShoot_Plugin.Patch
{
    internal class mUnityEngine
    {
        /// <summary>
        /// Replacing PluginControllers.getButtonDown as the flag is already used in the main plugin
        /// So just using the Keycode as GetKeyDown is valid untill end of frame, no matter ther number of request
        /// </summary>
        [HarmonyPatch(typeof(UnityEngine.Input), "GetButtonDown", new System.Type[] { typeof(string) })]
        class GetButtonDown
        {
            static bool Prefix(string buttonName, ref bool __result)
            {
                if (buttonName.Equals("PlayerStart1"))
                {
                    __result = UShoot_Plugin.PlayerControllers[0].GetButtonDown("START");
                    return false;
                }
                else if (buttonName.Equals("PlayerStart2"))
                {
                    __result = UShoot_Plugin.PlayerControllers[1].GetButtonDown("START");
                    return false;
                }
                return true;
            }
        }
    }
}
