using HarmonyLib;

namespace BepInEx_UShoot_Plugin.Patch
{
    internal class mLights
    {
        [HarmonyPatch(typeof(Lights), "Play")]
        class Play
        {
            static bool Prefix(string s)
            {
                //DemulShooter_Plugin.MyLogger.LogMessage("Lights.Play(" + s + ")");
                if (s == "NEURO")
                {
                    UShoot_Plugin.OutputData.NeuralyzerLamp = 1;
                }
                else if (s == "GUN1_FIRE")
                {
                    UShoot_Plugin.OutputData.GunLight[0] = 1;
                }
                else if (s == "GUN2_FIRE")
                {
                    UShoot_Plugin.OutputData.GunLight[1] = 1;
                }
                else if (s == "GUN1_OFF")
                {
                    UShoot_Plugin.OutputData.GunLight[1] = 0;
                }
                else if (s == "GUN2_OFF")
                {
                    UShoot_Plugin.OutputData.GunLight[1] = 0;
                }
                return false;
            }
        }
    }
}
