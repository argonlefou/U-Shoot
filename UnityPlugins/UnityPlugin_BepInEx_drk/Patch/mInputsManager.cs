using HarmonyLib;
using UnityEngine;

namespace BepInEx_UShoot_Plugin.Patch
{
    /// <summary>
    /// Those 2 functions are called in a loop, so we can just intercept the calls and insert our own data
    /// </summary>
    class mInputsManager
    {
        [HarmonyPatch(typeof(InputsManager), "SendButtonEvent")]
        class SendButtonEvent
        {
            static bool Prefix(int player, ref bool shooting)
            {
                //Ushoot_Plugin.MyLogger.LogMessage("mInputsManager.SendButtonEvent() : player=" + player.ToString() + ", shooting=" + shooting.ToString());
                shooting = UShoot_Plugin.PlayerControllers[player].GetButton("TRIGGER");
                return true;
            }
        }

        [HarmonyPatch(typeof(InputsManager), "SendPositionEvent")]
        class SendPositionEvent
        {
            static bool Prefix(int player, ref UnityEngine.Vector3 viewportPosition)
            {
                //Ushoot_Plugin.MyLogger.LogMessage("mInputsManager.SendPositionEvent(): player=" + player.ToString() + ", viewportPosition=" + viewportPosition.ToString());
                Vector3 v = UShoot_Plugin.PlayerControllers[player].GetAimingPosition();
                v.x = (2.0f * v.x / (float)Screen.width) - 1.0f;
                v.y = (2.0f * v.y / (float)Screen.height) - 1.0f;
                viewportPosition = v;
                //Ushoot_Plugin.MyLogger.LogMessage("mInputsManager.SendPositionEvent(): player=" + player.ToString() + ", viewportPosition=" + viewportPosition.ToString());
                return true;
            }
        }

        #region Dummy Calls

        /*
        [HarmonyPatch(typeof(InputsManager), "SendButtonEvent")]
        class mSendButtonEvent
        {
            [HarmonyReversePatch]
            [MethodImpl(MethodImplOptions.NoInlining)]
            public static void SendButtonEvent(object instance, int player, bool shooting)
            {
                //Used to call the private method
            }
        }
        
        [HarmonyPatch(typeof(InputsManager), "SendPositionEvent")]
        class mSendPositionEvent
        {
            [HarmonyReversePatch]
            [MethodImpl(MethodImplOptions.NoInlining)]
            public static void SendPositionEvent(object instance, int player, UnityEngine.Vector3 viewportPosition)
            {
                //Used to call the private method
            }
        }*/

        #endregion
    }
}
