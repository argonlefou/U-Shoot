using System.Reflection;
using HarmonyLib;

namespace BepInEx_USoot_Plugin.Patch
{
    internal class mSyFireComm
    {
        /// <summary>
        /// Disable COM opening and getting references for instance of the class and some function we can call to handle fake inputs
        /// </summary>
        [HarmonyPatch(typeof(SyFireComm), "Open")]
        class Open
        {
            static bool Prefix(ref bool __result, ref int ___m_port_id, SyFireComm __instance)
            {
                UShoot_Plugin.MyLogger.LogMessage("SyFireComm.Open()");

                //SyFireComm can also be obtained anywhere by using :
                //SyFireComm c = SingletonComponent<GameManager>.Instance.IOManager.GetGameIOComm<SYFireAndroidIO>().Comm;
                //But doing it here will make sure class is initialized and not need to be pulling in the "Update" plugin function to wait for it
                foreach (MethodInfo m in __instance.GetType().GetMethods(BindingFlags.Instance | BindingFlags.NonPublic))
                {
                    switch (m.Name)
                    {
                        case "RecvCoin":
                            {
                                UShoot_Plugin.mRecvCoin = m;
                                UShoot_Plugin.MyLogger.LogMessage("SysFireComm.RecvCoin() method found");
                                break;
                            }
                        case "RecvKey":
                            {
                                UShoot_Plugin.mRecvKey = m;
                                UShoot_Plugin.MyLogger.LogMessage("SysFireComm.RecvKey() method found");
                                break;
                            }
                        case "RecvDoorState":
                            {
                                UShoot_Plugin.mRecvDoorState = m;
                                UShoot_Plugin.MyLogger.LogMessage("SysFireComm.RecvDoorState() method found");
                                break;
                            }

                        case "RecvGunInCameraSpace":
                            {
                                UShoot_Plugin.mRecvGunInCameraSpace = m;
                                UShoot_Plugin.MyLogger.LogMessage("SysFireComm.RecvGunInCameraSpace() method found");
                                break;
                            }
                        case "RecvGunInScreenSpace":
                            {
                                UShoot_Plugin.mRecvGunInScreenSpace = m;
                                UShoot_Plugin.MyLogger.LogMessage("SysFireComm.RecvGunInScreenSpace() method found");
                                break;
                            }
                        case "RecvTickets":
                            {
                                UShoot_Plugin.mRecvTickets = m;
                                UShoot_Plugin.MyLogger.LogMessage("SysFireComm.RecvTickets() method found");
                                break;
                            }
                        default: break;
                    }
                }
                UShoot_Plugin.SyFireComm_Instance = __instance;
                ___m_port_id = 1;
                __result = true;
                return false;
            }
        }

        /// <summary>
        /// Disable COM Close
        /// </summary>
        [HarmonyPatch(typeof(SyFireComm), "Close")]
        class Close
        {
            static bool Prefix(ref bool __result)
            {
                UShoot_Plugin.MyLogger.LogMessage("SyFireComm.Close()");
                __result = true;
                return false;
            }
        }

        /// <summary>
        /// Disable COM read
        /// Should not be used, but who knows....
        /// </summary>
        [HarmonyPatch(typeof(SyFireComm), "ReadDataInPort")]
        class ReadDataInPort
        {
            static bool Prefix(ref int __result, int id, byte[] data, int len)
            {
                UShoot_Plugin.MyLogger.LogMessage("SyFireComm.ReadDataInPort()");
                __result = 0;
                return false;
            }
        }

        /// <summary>
        /// Disable COM write
        /// </summary>
        [HarmonyPatch(typeof(SyFireComm), "SendDataInPort")]
        class SendDataInPort
        {
            static bool Prefix(ref int __result, int id, byte[] data, int len)
            {
                UShoot_Plugin.MyLogger.LogMessage("SyFireComm.SendDataInPort()");
                __result = 0;
                return false;
            }
        }

        [HarmonyPatch(typeof(SyFireComm), "ReadThread")]
        class ReadThread
        {
            static bool Prefix(bool ___m_isClose, int ___READ_LOOP_TIME, SyFireComm __instance, uint[] ___m_door_state)
            {
                UShoot_Plugin.MyLogger.LogMessage("SyFireComm.ReadThread()");
                return false;
            }
        }


    }
}
