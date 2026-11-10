using HarmonyLib;
using UnityEngine.Playables;

namespace BepInEx_UShoot_Plugin.Patch
{
    internal class mSkipMixerBehaviour
    {
        /// <summary>
        /// Remove Some functions used by Alpha1...Alpha9 Keys
        /// </summary>
        [HarmonyPatch(typeof(TRA.Timeline.Skip.SkipMixerBehaviour), "ProcessFrame")]
        class ProcessFrame
        {
            static bool Prefix(Playable i_Playable, FrameData i_Info, object i_PlayerData)
            {
                return false;
            }
        }


    }
}
