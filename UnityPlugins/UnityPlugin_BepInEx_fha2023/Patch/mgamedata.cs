using HarmonyLib;
using UnityEngine;

namespace BepInEx_UShoot_Plugin.Patch
{
    public class mgamedata
    {
        [HarmonyPatch(typeof(gamedata), "get_mousepos")]
        class get_gun_pos_01
        {
            static bool Prefix(int i_id, ref Vector3 __result)
            {
                __result = UShoot_Plugin.PlayerControllers[i_id].GetAimingPosition();
                return false;
            }
        }
    }
}
