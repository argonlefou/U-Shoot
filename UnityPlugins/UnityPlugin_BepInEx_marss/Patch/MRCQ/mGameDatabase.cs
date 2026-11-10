using HarmonyLib;

namespace BepInEx_UShoot_Plugin.Patch
{
    class mGameDatabase
    {
        /// <summary>
        /// Called in a loop
        /// Station is player : 0 to 5
        /// We can get credits value for outputs
        /// 
        /// Also check GetcoinsIn() ???
        /// </summary>
        [HarmonyPatch(typeof(MRCQ.GameDataBase), "GetCredit")]
        class GetCredits
        {
            static bool Prefix(int station = -1, int record = -1, bool total = false)
            {
                //MarsSortie_Test_BepInEx_Plugin.MyLogger.LogMessage("MRCQ.GameDataBase.GetCredits(): station=" + station + ", record=" + record + ",total=" + total );
                return true;
            }
            static void Postfix(ref int __result, int station = -1, int record = -1, bool total = false)
            {
                if (station >= 0 && station <= 4)
                    UShoot_Plugin.OutputData.Credits[station] = __result;
            }
        }
    }
}
