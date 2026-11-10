using HarmonyLib;

namespace BepInEx_UShoot_Plugin.Patch
{
    /// <summary>
    /// Adding the possibility to send a "START button pressed" signal by shooting (like it is written on screen)
    /// </summary>
    internal class mContinueWindow
    {
        [HarmonyPatch(typeof(ContinueWindow), "Update")]
        class Update
        {
            static bool Prefix(ContinueWindow __instance)
            {
                for (int i = 0; i < UShoot_Plugin.MAX_PLAYERS; i++)
                {
                    if (UShoot_Plugin.PlayerControllers[i].GetButton("TRIGGER") && UShoot_Plugin.InactiveStates[i] != null)
                    {
                        UShoot_Plugin.MyLogger.LogMessage("mContinueWindow.Update() : P" + (i + 1).ToString() +" START pressed");

                        var methodInfo = typeof(InactivePlayerState).GetMethod("OnPlayerStartButtonPressed");
                        methodInfo.Invoke(UShoot_Plugin.InactiveStates[i], new object[] { PlayerUtility.GetPlayer(i) });

                        methodInfo = typeof(ContinueWindow).GetMethod("OnPlayerStartButtonPressed");
                        methodInfo.Invoke(__instance, new object[] { PlayerUtility.GetPlayer(i) });
                    }
                }
                return true;
            }
        }
    }
}
