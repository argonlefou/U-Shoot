using HarmonyLib;

namespace BepInEx_UShoot_Plugin.Patch
{
    internal class mPlayerWindow
    {
        /// <summary>
        /// Adding the possibility to send a "START button pressed" signal by shooting (like it is written on screen)
        /// </summary>
        [HarmonyPatch(typeof(PlayerWindow), "Update")]
        class Update
        {
            static bool Prefix(PlayerWindow __instance)
            {
                for (int i = 0; i < UShoot_Plugin.MAX_PLAYERS; i++)
                {
                    if (UShoot_Plugin.PlayerControllers[i].GetButton("TRIGGER") && UShoot_Plugin.InactiveStates[i] != null)
                    {
                        //UShoot_Plugin.MyLogger.LogMessage("mPlayerWindow.Update() : P" + (i + 1).ToString() + " START pressed");

                        var methodInfo = typeof(InactivePlayerState).GetMethod("OnPlayerStartButtonPressed");
                        methodInfo.Invoke(UShoot_Plugin.InactiveStates[i], new object[] { PlayerUtility.GetPlayer(i) });
                    }
                }
                return true;
            }
        }
    }
}
