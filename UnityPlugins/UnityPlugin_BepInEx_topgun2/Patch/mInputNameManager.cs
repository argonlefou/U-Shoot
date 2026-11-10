using HarmonyLib;
using UnityEngine;

namespace BepInEx_UShoot_Plugin.Patch
{
    internal class mInputNameManager
    {
        /// <summary>
        /// Name entry cursor is completely OFF (because of Animator ??) so just hiding it.....
        /// Find better solution later ?
        /// </summary>
        [HarmonyPatch(typeof(InPutNameManager), "Update")]
        class Update
        {
            static void Prefix(Transform[] ____UICursor)
            {
                for (int i = 0; i < ____UICursor.Length; i++)
                {
                    ____UICursor[i].gameObject.SetActive(false);
                }
            }
        }
    }
}
