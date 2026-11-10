using HarmonyLib;
using System;
using System.Collections;
using System.Reflection;
using UnityEngine;

namespace BepInEx_UShoot_Plugin.Patch
{
    public class mdllinstance
    {
        [HarmonyPatch(typeof(dllInstance), "Awake")]
        class Awake
        {
            static bool Prefix(dllInstance __instance, IntPtr ___m_dll, dlldata[] ___m_dlldata)
            {
                UShoot_Plugin.MyLogger.LogMessage("dllInstance.Awake");
                UnityEngine.Object.DontDestroyOnLoad(__instance.gameObject);
                if (Application.platform == RuntimePlatform.WindowsPlayer)
                {
                    Cursor.visible = false;
                }
                dllInstance.instance = __instance;
                for (int i = 0; i < dllInstance.PLAYERNUM; i++)
                {
                    ___m_dlldata[i] = new dlldata();
                    ___m_dlldata[i].inputkey = new bool[32];
                    ___m_dlldata[i].inputkeyup = new bool[32];
                    ___m_dlldata[i].inputkeydown = new bool[32];
                }

                MethodInfo mi = __instance.GetType().GetMethod("waitopen", BindingFlags.Instance | BindingFlags.NonPublic);
                if (mi != null)
                {
                    IEnumerator enumerator = (IEnumerator)mi.Invoke(__instance, null);
                    if (enumerator != null)
                    {
                        UShoot_Plugin.MyLogger.LogMessage("dllInstance.Awake --> Starting Coroutine !");
                        __instance.StartCoroutine(enumerator);
                    }
                }
                return false;
            }
        }               

        #region "gameport.dll" emulation

        [HarmonyPatch(typeof(dllInstance), "ClearBufferTicketCoin")]
        class ClearBufferTicketCoin
        {
            static bool Prefix(dllInstance __instance, IntPtr dll)
            {
                for (int i = 0; i < UShoot_Plugin.Coin.Length; i++)
                    UShoot_Plugin.Coin[i] = 0;
                return false;
            }
        }

        [HarmonyPatch(typeof(dllInstance), "CloseCam")]
        class closeCam
        {
            static bool Prefix(dllInstance __instance, IntPtr dll)
            {
                return false;
            }
        }

        [HarmonyPatch(typeof(dllInstance), "GetBufferCoin1p")]
        class GetBufferCoin1p
        {
            static bool Prefix(dllInstance __instance, IntPtr dll, ref int __result)
            {
                __result = UShoot_Plugin.Coin[0];
                return false;
            }
        }
        [HarmonyPatch(typeof(dllInstance), "GetBufferCoin2p")]
        class GetBufferCoin2p
        {
            static bool Prefix(dllInstance __instance, IntPtr dll, ref int __result)
            {
                __result = UShoot_Plugin.Coin[1];
                return false;
            }
        }
        [HarmonyPatch(typeof(dllInstance), "GetBufferCoin3p")]
        class GetBufferCoin3p
        {
            static bool Prefix(dllInstance __instance, IntPtr dll, ref int __result)
            {
                __result = UShoot_Plugin.Coin[2];
                return false;
            }
        }
        [HarmonyPatch(typeof(dllInstance), "GetBufferCoin4p")]
        class GetBufferCoin4p
        {
            static bool Prefix(dllInstance __instance, IntPtr dll, ref int __result)
            {
                __result = UShoot_Plugin.Coin[3];
                return false;
            }
        }

        /*** Unused ***/
        [HarmonyPatch(typeof(dllInstance), "GetBufferEgg1p")]
        class GetBufferEgg1p
        {
            static bool Prefix(dllInstance __instance, IntPtr dll, ref int __result)
            {
                UShoot_Plugin.PrintStackTrace();
                return false;
            }
        }
        [HarmonyPatch(typeof(dllInstance), "GetBufferEgg2p")]
        class GetBufferEgg2p
        {
            static bool Prefix(dllInstance __instance, IntPtr dll, ref int __result)
            {
                UShoot_Plugin.PrintStackTrace();
                return false;
            }
        }
        
        [HarmonyPatch(typeof(dllInstance), "GetBufferTicket1p")]
        class GetBufferTicket1p
        {
            static bool Prefix(dllInstance __instance, IntPtr dll, ref int __result)
            {
                __result = 0;
                return false;
            }
        }
        [HarmonyPatch(typeof(dllInstance), "GetBufferTicket2p")]
        class GetBufferTicket2p
        {
            static bool Prefix(dllInstance __instance, IntPtr dll, ref int __result)
            {
                __result = 0;
                return false;
            }
        }
        [HarmonyPatch(typeof(dllInstance), "GetBufferTicket3p")]
        class GetBufferTicket3p
        {
            static bool Prefix(dllInstance __instance, IntPtr dll, ref int __result)
            {
                __result = 0;
                return false;
            }
        }
        [HarmonyPatch(typeof(dllInstance), "GetBufferTicket4p")]
        class GetBufferTicket4p
        {
            static bool Prefix(dllInstance __instance, IntPtr dll, ref int __result)
            {
                __result = 0;
                return false;
            }
        }

        [HarmonyPatch(typeof(dllInstance), "GetComErr")]
        class GetComErr
        {
            static bool Prefix(dllInstance __instance, IntPtr dll, ref bool __result)
            {
                __result = false;
                return false;
            }
        }

        [HarmonyPatch(typeof(dllInstance), "GetDownWaterWarning")]
        class GetDownWaterWarning
        {
            static bool Prefix(dllInstance __instance, IntPtr dll, ref bool __result)
            {
                __result = false;
                return false;
            }
        }

        [HarmonyPatch(typeof(dllInstance), "GetInstance")]
        class GetInstance
        {
            static bool Prefix(dllInstance __instance, ref IntPtr __result)
            {
                __result = new IntPtr(0xC0FFE);
                UShoot_Plugin.MyLogger.LogWarning("dllInstance.GetInstance()");
                return false;
            }
        }

        [HarmonyPatch(typeof(dllInstance), "GetLeftKey")]
        class GetLeftKey
        {
            static bool Prefix(dllInstance __instance, IntPtr dll, ref bool __result)
            {
                __result = UShoot_Plugin.GameController.GetButton("MENU_LEFT");
                return false;
            }
        }

        [HarmonyPatch(typeof(dllInstance), "GetP1EggEmpty")]
        class GetP1EggEmpty
        {
            static bool Prefix(dllInstance __instance, IntPtr dll, ref bool __result)
            {
                return false;
            }
        }
        [HarmonyPatch(typeof(dllInstance), "GetP1Frie1")]
        class GetP1Frie1
        {
            static bool Prefix(dllInstance __instance, IntPtr dll, ref bool __result)
            {
                __result = UShoot_Plugin.PlayerControllers[0].GetButton("TRIGGER");
                return false;
            }
        }
        [HarmonyPatch(typeof(dllInstance), "GetP1Frie2")]
        class GetP1Frie2
        {
            static bool Prefix(dllInstance __instance, IntPtr dll, ref bool __result)
            {
                return false;
            }
        }
        [HarmonyPatch(typeof(dllInstance), "GetP1FrontTurn")]
        class GetP1FrontTurn
        {
            static bool Prefix(dllInstance __instance, IntPtr dll, ref ushort __result)
            {
                __result = UShoot_Plugin.FrontTurn[0];
                return false;
            }
        }
        [HarmonyPatch(typeof(dllInstance), "GetP1StartKey")]
        class GetP1StartKey
        {
            static bool Prefix(dllInstance __instance, IntPtr dll, ref bool __result)
            {
                __result = UShoot_Plugin.PlayerControllers[0].GetButton("START");
                return false;
            }
        }
        [HarmonyPatch(typeof(dllInstance), "GetP1WadcX")]
        class GetP1WadcX
        {
            static bool Prefix(dllInstance __instance, IntPtr dll, ref ushort __result)
            {
                __result = (ushort)UShoot_Plugin.PlayerControllers[0].Axis_X;
                return false;
            }
        }
        [HarmonyPatch(typeof(dllInstance), "GetP1WadcY")]
        class GetP1WadcY
        {
            static bool Prefix(dllInstance __instance, IntPtr dll, ref ushort __result)
            {
                __result = (ushort)UShoot_Plugin.PlayerControllers[0].Axis_Y;
                return false;
            }
        }

        [HarmonyPatch(typeof(dllInstance), "GetP2EggEmpty")]
        class GetP2EggEmpty
        {
            static bool Prefix(dllInstance __instance, IntPtr dll, ref bool __result)
            {
                return false;
            }
        }
        [HarmonyPatch(typeof(dllInstance), "GetP2Frie1")]
        class GetP2Frie1
        {
            static bool Prefix(dllInstance __instance, IntPtr dll, ref bool __result)
            {
                __result = UShoot_Plugin.PlayerControllers[1].GetButton("TRIGGER");
                return false;
            }
        }
        [HarmonyPatch(typeof(dllInstance), "GetP2Frie2")]
        class GetP2Frie2
        {
            static bool Prefix(dllInstance __instance, IntPtr dll, ref bool __result)
            {
                return false;
            }
        }
        [HarmonyPatch(typeof(dllInstance), "GetP2FrontTurn")]
        class GetP2FrontTurn
        {
            static bool Prefix(dllInstance __instance, IntPtr dll, ref ushort __result)
            {
                __result = UShoot_Plugin.FrontTurn[1];
                return false;
            }
        }
        [HarmonyPatch(typeof(dllInstance), "GetP2StartKey")]
        class GetP2StartKey
        {
            static bool Prefix(dllInstance __instance, IntPtr dll, ref bool __result)
            {
                __result = UShoot_Plugin.PlayerControllers[1].GetButton("START");
                return false;
            }
        }
        [HarmonyPatch(typeof(dllInstance), "GetP2WadcX")]
        class GetP2WadcX
        {
            static bool Prefix(dllInstance __instance, IntPtr dll, ref ushort __result)
            {
                __result = (ushort)UShoot_Plugin.PlayerControllers[1].Axis_X;
                return false;
            }
        }
        [HarmonyPatch(typeof(dllInstance), "GetP2WadcY")]
        class GetP2WadcY
        {
            static bool Prefix(dllInstance __instance, IntPtr dll, ref ushort __result)
            {
                __result = (ushort)UShoot_Plugin.PlayerControllers[1].Axis_Y;
                return false;
            }
        }

        [HarmonyPatch(typeof(dllInstance), "GetP3EggEmpty")]
        class GetP3EggEmpty
        {
            static bool Prefix(dllInstance __instance, IntPtr dll, ref bool __result)
            {
                return false;
            }
        }
        [HarmonyPatch(typeof(dllInstance), "GetP3Frie1")]
        class GetP3Frie1
        {
            static bool Prefix(dllInstance __instance, IntPtr dll, ref bool __result)
            {
                __result = UShoot_Plugin.PlayerControllers[2].GetButton("TRIGGER");
                return false;
            }
        }
        [HarmonyPatch(typeof(dllInstance), "GetP3Frie2")]
        class GetP3Frie2
        {
            static bool Prefix(dllInstance __instance, IntPtr dll, ref bool __result)
            {
                return false;
            }
        }
        [HarmonyPatch(typeof(dllInstance), "GetP3StartKey")]
        class GetP3StartKey
        {
            static bool Prefix(dllInstance __instance, IntPtr dll, ref bool __result)
            {
                __result = UShoot_Plugin.PlayerControllers[2].GetButton("START");
                return false;
            }
        }
        [HarmonyPatch(typeof(dllInstance), "GetP3WadcX")]
        class GetP3WadcX
        {
            static bool Prefix(dllInstance __instance, IntPtr dll, ref ushort __result)
            {
                __result = (ushort)UShoot_Plugin.PlayerControllers[2].Axis_X;
                return false;
            }
        }
        [HarmonyPatch(typeof(dllInstance), "GetP3WadcY")]
        class GetP3WadcY
        {
            static bool Prefix(dllInstance __instance, IntPtr dll, ref ushort __result)
            {
                __result = (ushort)UShoot_Plugin.PlayerControllers[2].Axis_Y;
                return false;
            }
        }

        [HarmonyPatch(typeof(dllInstance), "GetP4EggEmpty")]
        class GetP4EggEmpty
        {
            static bool Prefix(dllInstance __instance, IntPtr dll, ref bool __result)
            {
                return false;
            }
        }
        [HarmonyPatch(typeof(dllInstance), "GetP4Frie1")]
        class GetP4Frie1
        {
            static bool Prefix(dllInstance __instance, IntPtr dll, ref bool __result)
            {
                __result = UShoot_Plugin.PlayerControllers[3].GetButton("TRIGGER");
                return false;
            }
        }
        [HarmonyPatch(typeof(dllInstance), "GetP4StartKey")]
        class GetP4StartKey
        {
            static bool Prefix(dllInstance __instance, IntPtr dll, ref bool __result)
            {
                __result = UShoot_Plugin.PlayerControllers[3].GetButton("START");
                return false;
            }
        }
        [HarmonyPatch(typeof(dllInstance), "GetP4WadcX")]
        class GetP4WadcX
        {
            static bool Prefix(dllInstance __instance, IntPtr dll, ref ushort __result)
            {
                __result = (ushort)UShoot_Plugin.PlayerControllers[3].Axis_X;
                return false;
            }
        }
        [HarmonyPatch(typeof(dllInstance), "GetP4WadcY")]
        class GetP4WadcY
        {
            static bool Prefix(dllInstance __instance, IntPtr dll, ref ushort __result)
            {
                __result = (ushort)UShoot_Plugin.PlayerControllers[3].Axis_Y;
                return false;
            }
        }

        /*** Unused ***/
        [HarmonyPatch(typeof(dllInstance), "GetResponcoin1p")]
        class GetResponcoin1p
        {
            static bool Prefix(dllInstance __instance, IntPtr dll, ref bool __result)
            {
                UShoot_Plugin.PrintStackTrace();
                return false;
            }
        }
        [HarmonyPatch(typeof(dllInstance), "GetResponcoin2p")]
        class GetResponcoin2p
        {
            static bool Prefix(dllInstance __instance, IntPtr dll, ref bool __result)
            {
                UShoot_Plugin.PrintStackTrace();
                return false;
            }
        }
        [HarmonyPatch(typeof(dllInstance), "GetResponcoin3p")]
        class GetResponcoin3p
        {
            static bool Prefix(dllInstance __instance, IntPtr dll, ref bool __result)
            {
                UShoot_Plugin.PrintStackTrace();
                return false;
            }
        }
        [HarmonyPatch(typeof(dllInstance), "GetResponcoin4p")]
        class GetResponcoin4p
        {
            static bool Prefix(dllInstance __instance, IntPtr dll, ref bool __result)
            {
                UShoot_Plugin.PrintStackTrace();
                return false;
            }
        }

        [HarmonyPatch(typeof(dllInstance), "GetRightKey")]
        class GetRightKey
        {
            static bool Prefix(dllInstance __instance, IntPtr dll, ref bool __result)
            {
                __result = UShoot_Plugin.GameController.GetButton("MENU_RIGHT");
                return false;
            }
        }

        [HarmonyPatch(typeof(dllInstance), "GetSettingKey")]
        class GetSettingKey
        {
            static bool Prefix(dllInstance __instance, IntPtr dll, ref bool __result)
            {
                __result = UShoot_Plugin.GameController.GetButton("TEST");
                return false;
            }
        }

        [HarmonyPatch(typeof(dllInstance), "GetTicketEmpty1p")]
        class GetTicketEmpty1p
        {
            static bool Prefix(dllInstance __instance, IntPtr dll, ref bool __result)
            {
                __result = false;
                return false;
            }
        }
        [HarmonyPatch(typeof(dllInstance), "GetTicketEmpty2p")]
        class GetTicketEmpty2p
        {
            static bool Prefix(dllInstance __instance, IntPtr dll, ref bool __result)
            {
                __result = false;
                return false;
            }
        }
        [HarmonyPatch(typeof(dllInstance), "GetTicketEmpty3p")]
        class GetTicketEmpty3p
        {
            static bool Prefix(dllInstance __instance, IntPtr dll, ref bool __result)
            {
                __result = false;
                return false;
            }
        }
        [HarmonyPatch(typeof(dllInstance), "GetTicketEmpty4p")]
        class GetTicketEmpty4p
        {
            static bool Prefix(dllInstance __instance, IntPtr dll, ref bool __result)
            {
                __result = false;
                return false;
            }
        }

        //Unused
        [HarmonyPatch(typeof(dllInstance), "GetTicketKey")]
        class GetTicketKey
        {
            static bool Prefix(dllInstance __instance, IntPtr dll, ref bool __result)
            {
                return false;
            }
        }

        [HarmonyPatch(typeof(dllInstance), "GetUpWaterWarning")]
        class GetUpWaterWarning
        {
            static bool Prefix(dllInstance __instance, IntPtr dll, ref bool __result)
            {
                __result = false;
                return false;
            }
        }

        [HarmonyPatch(typeof(dllInstance), "init")]
        class init
        {
            static bool Prefix(dllInstance __instance, IntPtr dll, int nparam, ref int __result)
            {
                __result = 1;
                return false;
            }
        }

        [HarmonyPatch(typeof(dllInstance), "OpenCam")]
        class OpenCam
        {
            static bool Prefix(dllInstance __instance, IntPtr dll)
            {
                return false;
            }
        }

        [HarmonyPatch(typeof(dllInstance), "SetAtomizerOpen")]
        class SetAtomizerOpen
        {
            static bool Prefix(dllInstance __instance, IntPtr dll, int nparam)
            {
                UShoot_Plugin.PrintStackTrace();
                return false;
            }
        }

        [HarmonyPatch(typeof(dllInstance), "SetIsSetting")]
        class SetIsSetting
        {
            static bool Prefix(dllInstance __instance, IntPtr dll, int isStop)
            {
                UShoot_Plugin.PrintStackTrace();
                return false;
            }
        }

        [HarmonyPatch(typeof(dllInstance), "SetLightbelt1")]
        class SetLightbelt1
        {
            static bool Prefix(dllInstance __instance, IntPtr dll, int nparam)
            {
                UShoot_Plugin.OutputData.LightBelt1 = nparam;
                return false;
            }
        }
        [HarmonyPatch(typeof(dllInstance), "SetLightbelt2")]
        class SetLightbelt2
        {
            static bool Prefix(dllInstance __instance, IntPtr dll, int nparam)
            {
                UShoot_Plugin.OutputData.LightBelt2 = nparam;
                return false;
            }
        }
        [HarmonyPatch(typeof(dllInstance), "SetLightbelt3")]
        class SetLightbelt3
        {
            static bool Prefix(dllInstance __instance, IntPtr dll, int nparam)
            {
                UShoot_Plugin.OutputData.LightBelt3 = nparam;
                return false;
            }
        }
        [HarmonyPatch(typeof(dllInstance), "SetLightbelt4")]
        class SetLightbelt4
        {
            static bool Prefix(dllInstance __instance, IntPtr dll, int nparam)
            {
                UShoot_Plugin.OutputData.LightBelt4 = nparam;
                return false;
            }
        }

        [HarmonyPatch(typeof(dllInstance), "SetOpenLed")]
        class SetOpenLed
        {
            static bool Prefix(dllInstance __instance, IntPtr dll, int nparam)
            {
                UShoot_Plugin.PrintStackTrace();
                return false;
            }
        }

        [HarmonyPatch(typeof(dllInstance), "SetP1Eggnum")]
        class SetP1Eggnum
        {
            static bool Prefix(dllInstance __instance, IntPtr dll, int nparam, bool bfugai)
            {
                UShoot_Plugin.PrintStackTrace();
                return false;
            }
        }
        [HarmonyPatch(typeof(dllInstance), "SetP1ElecShake")]
        class SetP1ElecShake
        {
            static bool Prefix(dllInstance __instance, IntPtr dll, int nparam)
            {
                UShoot_Plugin.OutputData.ChairMotor[0] = (byte)nparam;
                return false;
            }
        }
        [HarmonyPatch(typeof(dllInstance), "SetP1GiveSpeed")]
        class SetP1GiveSpeed
        {
            static bool Prefix(dllInstance __instance, IntPtr dll, int nparam)
            {
                UShoot_Plugin.OutputData.GunMotor[0] = (byte)nparam;
                return false;
            }
        }
        [HarmonyPatch(typeof(dllInstance), "SetP1GiveSpeed2")]
        class SetP1GiveSpeed2
        {
            static bool Prefix(dllInstance __instance, IntPtr dll, int nparam)
            {
                UShoot_Plugin.PrintStackTrace();
                return false;
            }
        }
        [HarmonyPatch(typeof(dllInstance), "SetP1PrintTicket")]
        class SetP1PrintTicket
        {
            static bool Prefix(dllInstance __instance, IntPtr dll, int count)
            {
                UShoot_Plugin.PrintStackTrace();
                return false;
            }
        }
        [HarmonyPatch(typeof(dllInstance), "SetP1RollBall")]
        class SetP1RollBall
        {
            static bool Prefix(dllInstance __instance, IntPtr dll, int nparam)
            {
                UShoot_Plugin.PrintStackTrace();
                return false;
            }
        }
        [HarmonyPatch(typeof(dllInstance), "SetP1ShootBall")]
        class SetP1ShootBall
        {
            static bool Prefix(dllInstance __instance, IntPtr dll, int nparam)
            {
                UShoot_Plugin.PrintStackTrace();
                return false;
            }
        }
        [HarmonyPatch(typeof(dllInstance), "SetP1WaterElecValue1")]
        class SetP1WaterElecValue1
        {
            static bool Prefix(dllInstance __instance, IntPtr dll, bool nparam)
            {
                UShoot_Plugin.OutputData.SmallGun[0] = nparam ? (byte)1 : (byte)0;
                return false;
            }
        }
        [HarmonyPatch(typeof(dllInstance), "SetP1WaterElecValue2")]
        class SetP1WaterElecValue2
        {
            static bool Prefix(dllInstance __instance, IntPtr dll, bool nparam)
            {
                UShoot_Plugin.OutputData.BigGun[0] = nparam ? (byte)1 : (byte)0;
                return false;
            }
        }
        [HarmonyPatch(typeof(dllInstance), "SetP1WaterRotatingmotor")]
        class SetP1WaterRotatingmotor
        {
            static bool Prefix(dllInstance __instance, IntPtr dll, int nparam)
            {
                UShoot_Plugin.PrintStackTrace();
                return false;
            }
        }

        [HarmonyPatch(typeof(dllInstance), "SetP2Eggnum")]
        class SetP2Eggnum
        {
            static bool Prefix(dllInstance __instance, IntPtr dll, int nparam, bool bfugai)
            {
                UShoot_Plugin.PrintStackTrace();
                return false;
            }
        }
        [HarmonyPatch(typeof(dllInstance), "SetP2ElecShake")]
        class SetP2ElecShake
        {
            static bool Prefix(dllInstance __instance, IntPtr dll, int nparam)
            {
                UShoot_Plugin.OutputData.ChairMotor[1] = (byte)nparam;
                return false;
            }
        }
        [HarmonyPatch(typeof(dllInstance), "SetP2GiveSpeed")]
        class SetP2GiveSpeed
        {
            static bool Prefix(dllInstance __instance, IntPtr dll, int nparam)
            {
                UShoot_Plugin.OutputData.GunMotor[1] = (byte)nparam;
                return false;
            }
        }
        [HarmonyPatch(typeof(dllInstance), "SetP2GiveSpeed2")]
        class SetP2GiveSpeed2
        {
            static bool Prefix(dllInstance __instance, IntPtr dll, int nparam)
            {
                UShoot_Plugin.PrintStackTrace();
                return false;
            }
        }
        [HarmonyPatch(typeof(dllInstance), "SetP2PrintTicket")]
        class SetP2PrintTicket
        {
            static bool Prefix(dllInstance __instance, IntPtr dll, int count)
            {
                UShoot_Plugin.PrintStackTrace();
                return false;
            }
        }
        [HarmonyPatch(typeof(dllInstance), "SetP2RollBall")]
        class SetP2RollBall
        {
            static bool Prefix(dllInstance __instance, IntPtr dll, int nparam)
            {
                UShoot_Plugin.PrintStackTrace();
                return false;
            }
        }
        [HarmonyPatch(typeof(dllInstance), "SetP2ShootBall")]
        class SetP2ShootBall
        {
            static bool Prefix(dllInstance __instance, IntPtr dll, int nparam)
            {
                UShoot_Plugin.PrintStackTrace();
                return false;
            }
        }
        [HarmonyPatch(typeof(dllInstance), "SetP2WaterElecValue1")]
        class SetP2WaterElecValue1
        {
            static bool Prefix(dllInstance __instance, IntPtr dll, bool nparam)
            {
                UShoot_Plugin.OutputData.SmallGun[1] = nparam ? (byte)1 : (byte)0;
                return false;
            }
        }
        [HarmonyPatch(typeof(dllInstance), "SetP2WaterElecValue2")]
        class SetP2WaterElecValue2
        {
            static bool Prefix(dllInstance __instance, IntPtr dll, bool nparam)
            {
                UShoot_Plugin.OutputData.BigGun[1] = nparam ? (byte)1 : (byte)0;
                return false;
            }
        }
        [HarmonyPatch(typeof(dllInstance), "SetP2WaterRotatingmotor")]
        class SetP2WaterRotatingmotor
        {
            static bool Prefix(dllInstance __instance, IntPtr dll, int nparam)
            {
                UShoot_Plugin.PrintStackTrace();
                return false;
            }
        }

        [HarmonyPatch(typeof(dllInstance), "SetP3ElecShake")]
        class SetP3ElecShake
        {
            static bool Prefix(dllInstance __instance, IntPtr dll, int nparam)
            {
                UShoot_Plugin.OutputData.ChairMotor[2] = (byte)nparam;
                return false;
            }
        }
        [HarmonyPatch(typeof(dllInstance), "SetP3GiveSpeed")]
        class SetP3GiveSpeed
        {
            static bool Prefix(dllInstance __instance, IntPtr dll, int nparam)
            {
                UShoot_Plugin.OutputData.GunMotor[2] = (byte)nparam;
                return false;
            }
        }
        [HarmonyPatch(typeof(dllInstance), "SetP3PrintTicket")]
        class SetP3PrintTicket
        {
            static bool Prefix(dllInstance __instance, IntPtr dll, int count)
            {
                UShoot_Plugin.PrintStackTrace();
                return false;
            }
        }
        [HarmonyPatch(typeof(dllInstance), "SetP3WaterElecValue1")]
        class SetP3WaterElecValue1
        {
            static bool Prefix(dllInstance __instance, IntPtr dll, bool nparam)
            {
                UShoot_Plugin.OutputData.SmallGun[2] = nparam ? (byte)1 : (byte)0;
                return false;
            }
        }
        [HarmonyPatch(typeof(dllInstance), "SetP3WaterElecValue2")]
        class SetP3WaterElecValue2
        {
            static bool Prefix(dllInstance __instance, IntPtr dll, bool nparam)
            {
                UShoot_Plugin.OutputData.BigGun[2] = nparam ? (byte)1 : (byte)0;
                return false;
            }
        }
        [HarmonyPatch(typeof(dllInstance), "SetP3WaterRotatingmotor")]
        class SetP3WaterRotatingmotor
        {
            static bool Prefix(dllInstance __instance, IntPtr dll, int nparam)
            {
                UShoot_Plugin.PrintStackTrace();
                return false;
            }
        }

        [HarmonyPatch(typeof(dllInstance), "SetP4ElecShake")]
        class SetP4ElecShake
        {
            static bool Prefix(dllInstance __instance, IntPtr dll, int nparam)
            {
                UShoot_Plugin.OutputData.ChairMotor[3] = (byte)nparam;
                return false;
            }
        }
        [HarmonyPatch(typeof(dllInstance), "SetP4GiveSpeed")]
        class SetP4GiveSpeed
        {
            static bool Prefix(dllInstance __instance, IntPtr dll, int nparam)
            {
                UShoot_Plugin.OutputData.GunMotor[3] = (byte)nparam;
                return false;
            }
        }
        [HarmonyPatch(typeof(dllInstance), "SetP4PrintTicket")]
        class SetP4PrintTicket
        {
            static bool Prefix(dllInstance __instance, IntPtr dll, int count)
            {
                UShoot_Plugin.PrintStackTrace();
                return false;
            }
        }
        [HarmonyPatch(typeof(dllInstance), "SetP4WaterElecValue1")]
        class SetP4WaterElecValue1
        {
            static bool Prefix(dllInstance __instance, IntPtr dll, bool nparam)
            {
                UShoot_Plugin.OutputData.SmallGun[3] = nparam ? (byte)1 : (byte)0;
                return false;
            }
        }
        [HarmonyPatch(typeof(dllInstance), "SetP4WaterElecValue2")]
        class SetP4WaterElecValue2
        {
            static bool Prefix(dllInstance __instance, IntPtr dll, bool nparam)
            {
                UShoot_Plugin.OutputData.BigGun[3] = nparam ? (byte)1 : (byte)0;
                return false;
            }
        }
        [HarmonyPatch(typeof(dllInstance), "SetP4WaterRotatingmotor")]
        class SetP4WaterRotatingmotor
        {
            static bool Prefix(dllInstance __instance, IntPtr dll, int nparam)
            {
                UShoot_Plugin.PrintStackTrace();
                return false;
            }
        }

        [HarmonyPatch(typeof(dllInstance), "SetStartButtomLight1p")]
        class SetStartButtonLight1p
        {
            static bool Prefix(dllInstance __instance, IntPtr dll, int nparam)
            {
                UShoot_Plugin.OutputData.StartLight[0] = (byte)nparam;
                return false;
            }
        }
        [HarmonyPatch(typeof(dllInstance), "SetStartButtomLight2p")]
        class SetStartButtonLight2p
        {
            static bool Prefix(dllInstance __instance, IntPtr dll, int nparam)
            {
                UShoot_Plugin.OutputData.StartLight[1] = (byte)nparam;
                return false;
            }
        }
        [HarmonyPatch(typeof(dllInstance), "SetStartButtomLight3p")]
        class SetStartButtonLight3p
        {
            static bool Prefix(dllInstance __instance, IntPtr dll, int nparam)
            {
                UShoot_Plugin.OutputData.StartLight[2] = (byte)nparam;
                return false;
            }
        }
        [HarmonyPatch(typeof(dllInstance), "SetStartButtomLight4p")]
        class SetStartButtonLight4p
        {
            static bool Prefix(dllInstance __instance, IntPtr dll, int nparam)
            {
                UShoot_Plugin.OutputData.StartLight[3] = (byte)nparam;
                return false;
            }
        }

        [HarmonyPatch(typeof(dllInstance), "SetWaterAtomizer")]
        class SetWaterAtomizer
        {
            static bool Prefix(dllInstance __instance, IntPtr dll, bool nparam)
            {
                UShoot_Plugin.OutputData.Atomizer = 1;
                return false;
            }
        }
        [HarmonyPatch(typeof(dllInstance), "SetWaterAtomizer2")]
        class SetWaterAtomizer2
        {
            static bool Prefix(dllInstance __instance, IntPtr dll, bool nparam)
            {
                UShoot_Plugin.OutputData.BoxLight = 1;
                return false;
            }
        }

        [HarmonyPatch(typeof(dllInstance), "SetWaterpump")]
        class SetWaterpump
        {
            static bool Prefix(dllInstance __instance, IntPtr dll, bool nparam)
            {
                UShoot_Plugin.OutputData.WaterPump = nparam ? (byte)1 : (byte)0;
                return false;
            }
        }

        [HarmonyPatch(typeof(dllInstance), "ShowCamWnd")]
        class ShowCamWnd
        {
            static bool Prefix(dllInstance __instance, IntPtr dll, bool nparam)
            {
                return false;
            }
        }

        [HarmonyPatch(typeof(dllInstance), "SubBufferCoin1p")]
        class SetBufferCoin1p
        {
            static bool Prefix(dllInstance __instance, IntPtr dll, int count)
            {
                UShoot_Plugin.Coin[0] -= count;
                if (UShoot_Plugin.Coin[0] < 0)
                    UShoot_Plugin.Coin[0] = 0;
                return false;
            }
        }
        [HarmonyPatch(typeof(dllInstance), "SubBufferCoin2p")]
        class SetBufferCoin2p
        {
            static bool Prefix(dllInstance __instance, IntPtr dll, int count)
            {
                UShoot_Plugin.Coin[1] -= count;
                if (UShoot_Plugin.Coin[1] < 0)
                    UShoot_Plugin.Coin[1] = 0;
                return false;
            }
        }
        [HarmonyPatch(typeof(dllInstance), "SubBufferCoin3p")]
        class SetBufferCoin3p
        {
            static bool Prefix(dllInstance __instance, IntPtr dll, int count)
            {
                UShoot_Plugin.Coin[2] -= count;
                if (UShoot_Plugin.Coin[2] < 0)
                    UShoot_Plugin.Coin[2] = 0;
                return false;
            }
        }
        [HarmonyPatch(typeof(dllInstance), "SubBufferCoin4p")]
        class SetBufferCoin4p
        {
            static bool Prefix(dllInstance __instance, IntPtr dll, int count)
            {
                UShoot_Plugin.Coin[3] -= count;
                if (UShoot_Plugin.Coin[3] < 0)
                    UShoot_Plugin.Coin[3] = 0;
                return false;
            }
        }



        #endregion
    }
}
