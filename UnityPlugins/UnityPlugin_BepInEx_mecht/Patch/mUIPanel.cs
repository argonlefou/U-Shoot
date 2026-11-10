using HarmonyLib;
using System.Reflection;

namespace BepInEx_UShoot_Plugin.Patch
{
    class mUIPanel
    {
        /// <summary>
        /// Original function allows HArdcoded keyboard keys to be pushed even if IO board is connected
        /// Recreating the function so that HArcoded keys are not used anymore
        /// </summary>
        [HarmonyPatch(typeof(UIPanel), "OnKeyDown")]
        class OnKeyDown
        {
            static bool Prefix(UIPanel __instance)
            {
                if (__instance.Root == null)
                    return false;

                if (dllInstance.getdlldata(__instance.Root.ID).biosucees && dllInstance.getdlldata(__instance.Root.ID).inputkeydown[UIback.STARTKEY])
                {
                    foreach (MethodInfo mi in __instance.GetType().GetMethods(BindingFlags.Instance | BindingFlags.NonPublic))
                        if (mi.Name.Equals("Submit"))
                        {
                            mi.Invoke(__instance, null);
                            break;
                        }
                }
                return false;
            }
        }
    }
}
