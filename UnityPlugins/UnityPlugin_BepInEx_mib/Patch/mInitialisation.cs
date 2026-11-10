using HarmonyLib;
using MIB;
using UnityEngine;

namespace BepInEx_UShoot_Plugin.Patch
{
    internal class mInitialisation
    {
        [HarmonyPatch(typeof(Initialisation), "Start")]
        class Start
        {
            static void Postfix()
            {
                if (UShoot_Plugin.ForceResolution)
                    Screen.SetResolution(UShoot_Plugin.ScreenWidth, UShoot_Plugin.ScreenHeight, UShoot_Plugin.Fullscreen);

                MIBGameSettings.LoadFile();
                MIBCore.Instance.GetCreditInformaion();
            }
        }
    }
}
