using HarmonyLib;

namespace BepInEx_UShoot_Plugin.Patch
{
    internal class mInactivePlayerState
    {
        /// <summary>
        /// Saving each player's IncativePlayerState instance uppon entering them
        /// So that we can use it later to call the "OnPlayerButtonPressed" method of that instace
        /// </summary>
        [HarmonyPatch(typeof(InactivePlayerState), "OnEnter")]
        class OnEnter
        {
            static bool Prefix(InactivePlayerState __instance)
            {
                UShoot_Plugin.MyLogger.LogMessage("InactivePlayerState.OnEnter() : " + __instance.name + ", " + __instance.Controller.name + ", " + __instance.controller.id);
                UShoot_Plugin.InactiveStates[__instance.controller.id] = __instance;
                return true;
            }
        }

        /// <summary>
        /// Resetting the instance to null so that the event can be sent only once when the KEY is pressed
        /// </summary>
        [HarmonyPatch(typeof(InactivePlayerState), "OnExit")]
        class OnExit
        {
            static bool Prefix(InactivePlayerState __instance)
            {
                UShoot_Plugin.MyLogger.LogMessage("InactivePlayerState.OnExit() : " + __instance.name + ", " + __instance.Controller.name + ", " + __instance.controller.id);
                UShoot_Plugin.InactiveStates[__instance.controller.id] = null;
                return true;
            }
        }
    }
}
