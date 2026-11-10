using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BepInEx_UShoot_Plugin.Patch
{
    internal class mLaserLocator
    {
        [HarmonyPatch(typeof(MRCQ.LaserLocator), "GetPosition")]
        class Update
        {
            static bool Prefix(int laserIndex)
            {
                UShoot_Plugin.MyLogger.LogMessage("MRCQ.mLaserLocator.GetPosition() : laserIndex=" + laserIndex);
                return true;
            }
        }
    }
}
