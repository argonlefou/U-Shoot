using System.Reflection;
using HarmonyLib;

namespace BepInEx_UShoot_Plugin.Patch
{
    internal class mMatrixManager
    {
        /// <summary>
        /// Method is called in MatrixManager.Awake()
        /// Forcing return SBK.Matrix.MatrixManager.MPLBPAMKFJP.MATRIX_NO_ERROR, but the enum in internal and we can't access the type
        /// So, returning the enum value (= 0)
        /// </summary>
        [HarmonyPatch]
        class KDLGNPHHMKI
        {
            static MethodBase TargetMethod()
            {
                foreach (MethodInfo mi in AccessTools.TypeByName("SBK.Matrix.MatrixManager").GetMethods(BindingFlags.NonPublic | BindingFlags.Instance))
                {
                    if (mi.Name.Equals("KDLGNPHHMKI"))
                    {
                        return mi;
                    }
                }
                return null;
            }
            static bool Prefix(ref object __result)
            {
                UShoot_Plugin.MyLogger.LogWarning("SBK.MatrixManager.KDLGNPHHMKI()");
                //DemulShooter_Plugin.PrintStackTrace();
                __result = 0;
                return false;
            }
        }

        ///Ducon also patched:
        ///SBK.Matrix.MatrixManager.EGIEJAEHODM()
        ///SBK.Matrix.MatrixManager.KFPJOBFGLKO()
        ///SBK.Matrix.MatrixManager.FDEMJBICGOM()
        ///SBK.Matrix.MatrixManager.BPMOIOJDHDD()
        ///SBK.Matrix.MatrixManager.MBCKFLLBDFN()
        ///SBK.Matrix.MatrixManager.EOMEICJLBHH()
        ///SBK.Matrix.MatrixManager.MGILPFJAAEE()
        ///SBK.Matrix.MatrixManager.GEAIHJMFPEJ()

    }
}
