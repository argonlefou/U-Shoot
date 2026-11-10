using HarmonyLib;
using UnityEngine;

namespace BepInEx_UShoot_Plugin.Patch
{
    class mNPCBase
    {
        /// <summary>
        /// Removing call to the ArcadeControls library
        /// </summary>
        [HarmonyPatch(typeof(NPCBase), "NpcScale")]
        class Start
        {
            static bool Prefix(float scale, ref float ___npcScale, NPCBase __instance)
            {
                ___npcScale = scale;
                __instance.transform.localScale *= scale;
                if (__instance.shadowGo != null)
                {
                    Projector component = __instance.shadowGo.GetComponent<Projector>();
                    if (component != null)
                    {
                        component.orthographicSize *= scale;
                    }
                }
                return false;
            }
        }
    }
}
