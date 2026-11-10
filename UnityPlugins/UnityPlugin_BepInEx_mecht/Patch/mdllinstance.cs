using HarmonyLib;
using UnityEngine;
using System;

namespace BepInEx_UShoot_Plugin.Patch
{
    /// <summary>
    /// Patch a LOT of native DLL calls
    /// </summary>
    class mdllinstance
    {
        #region Native DLL calls

        [HarmonyPatch(typeof(dllInstance), "ClearBufferTicketCoin")]
        class ClearBufferTicketCoin
        {
            static bool Prefix(IntPtr dll)
            {
                UShoot_Plugin.BufferCoin = new int[4];
                UShoot_Plugin.BufferTicket = new int[4];
                return false;
            }
        }

        /// <summary>
        /// Patching here allow to prevent the following calls :
        ///  SetP1Light
        ///  SetP2Light
        ///  SetP3Light
        ///  SetP4Light
        /// </summary>
        [HarmonyPatch(typeof(dllInstance), "dll_set_dizuo_light")]
        class dll_set_dizuo_light
        {
            static bool Prefix(int i_playerid, int nparam)
            {
                UShoot_Plugin.OutputData.PlayerLight[i_playerid] = (byte)nparam;
                return false;
            }
        }

        /// <summary>
        /// Patching here allow to prevent the following calls :
        ///  SetP1WaterRotatingmotor
        ///  SetP2WaterRotatingmotor
        ///  SetP3WaterRotatingmotor
        ///  SetP4WaterRotatingmotor
        /// </summary>
        [HarmonyPatch(typeof(dllInstance), "dll_setwater_Rotatingmotor")]
        class dll_setwater_Rotatingmotor
        {
            static bool Prefix(int playerid, int nparam)
            {
                //Value may be "2", changing it to 1 as we
                if (nparam > 0)
                    UShoot_Plugin.OutputData.RotatingMotor[playerid] = 1;
                else
                    UShoot_Plugin.OutputData.RotatingMotor[playerid] = (byte)nparam;
                return false;
            }
        }
        

        [HarmonyPatch(typeof(dllInstance), "dll_setwater_speed")]
        class dll_setwater_speed
        {
            static bool Prefix(int playerid, int i_speed)
            {
                UShoot_Plugin.OutputData.WaterPower[playerid] = (byte)i_speed;
                return false;
            }
        }


        /// <summary>
        /// Patching here allow to prevent the following calls :
        ///  GetBufferCoin1p
        ///  GetBufferCoin2p
        ///  GetBufferCoin3p
        ///  GetBufferCoin4p
        /// </summary>
        [HarmonyPatch(typeof(dllInstance), "getCoin")]
        class getCoin
        {
            static bool Prefix(int id, ref int __result)
            {
                __result = UShoot_Plugin.BufferCoin[id];
                return false;
            }
        }

        /// <summary>
        /// Patching here allow to prevent the following calls :
        ///  GetBufferTicket1p
        ///  GetBufferTicket2p
        ///  GetBufferTicket3p
        ///  GetBufferTicket4p
        /// </summary>
        [HarmonyPatch(typeof(dllInstance), "getTicket")]
        class getTicket
        {
            static bool Prefix(int id, ref int __result)
            {
                __result = UShoot_Plugin.BufferTicket[id];
                return false;
            }
        }

        [HarmonyPatch(typeof(dllInstance), "GetComErr")]
        class GetComErr
        {
            static bool Prefix(IntPtr dll, ref bool __result)
            {
                __result = false;
                return false;
            }
        }

        [HarmonyPatch(typeof(dllInstance), "GetDownWaterWarning")]
        class GetDownWaterWarning
        {
            static bool Prefix(IntPtr dll, ref bool __result)
            {
                __result = false;
                return false;
            }
        }

        /// <summary>
        /// Returning a random IntPtr
        /// that way the dll is never loaded at all for any request
        /// </summary>
        [HarmonyPatch(typeof(dllInstance), "GetInstance")]
        class GetInstance
        {
            static bool Prefix(ref IntPtr __result)
            {
                __result = new IntPtr(0xCAFEBABE);
                return false;
            }
        }
        

        [HarmonyPatch(typeof(dllInstance), "GetLeftKey")]
        class GetLeftKey
        {
            static bool Prefix(IntPtr dll, ref bool __result)
            {
                __result = UShoot_Plugin.GameController.GetButton("MENU_LEFT");
                return false;
            }
        }

        /// <summary>
        /// Unused ?
        /// </summary>
        [HarmonyPatch(typeof(dllInstance), "GetP1EggEmpty")]
        class GetP1EggEmpty
        {
            static bool Prefix(IntPtr dll, ref bool __result)
            {
                __result = false;
                return false;
            }
        }
        /// <summary>
        /// Unused ?
        /// </summary>
        [HarmonyPatch(typeof(dllInstance), "GetP2EggEmpty")]
        class GetP2EggEmpty
        {
            static bool Prefix(IntPtr dll, ref bool __result)
            {
                __result = false;
                return false;
            }
        }        

        [HarmonyPatch(typeof(dllInstance), "GetP1Frie1")]
        class GetP1Frie1
        {
            static bool Prefix(IntPtr dll, ref bool __result)
            {
                __result = UShoot_Plugin.PlayerControllers[0].GetButton("TRIGGER");
                return false;
            }
        }
        [HarmonyPatch(typeof(dllInstance), "GetP2Frie1")]
        class GetP2Frie1
        {
            static bool Prefix(IntPtr dll, ref bool __result)
            {
                __result = UShoot_Plugin.PlayerControllers[1].GetButton("TRIGGER");
                return false;
            }
        }
        [HarmonyPatch(typeof(dllInstance), "GetP3Frie1")]
        class GetP3Frie1
        {
            static bool Prefix(IntPtr dll, ref bool __result)
            {
                __result = UShoot_Plugin.PlayerControllers[2].GetButton("TRIGGER");
                return false;
            }
        }
        [HarmonyPatch(typeof(dllInstance), "GetP4Frie1")]
        class GetP4Frie1
        {
            static bool Prefix(IntPtr dll, ref bool __result)
            {
                __result = UShoot_Plugin.PlayerControllers[3].GetButton("TRIGGER");
                return false;
            }
        }

        [HarmonyPatch(typeof(dllInstance), "GetP1Frie2")]
        class GetP1Frie2
        {
            static bool Prefix(IntPtr dll, ref bool __result)
            {
                __result = UShoot_Plugin.PlayerControllers[0].GetButton("TRIGGER");
                return false;
            }
        }
        [HarmonyPatch(typeof(dllInstance), "GetP2Frie2")]
        class GetP2Frie2
        {
            static bool Prefix(IntPtr dll, ref bool __result)
            {
                __result = UShoot_Plugin.PlayerControllers[1].GetButton("TRIGGER");
                return false;
            }
        }
        [HarmonyPatch(typeof(dllInstance), "GetP3Frie2")]
        class GetP3Frie2
        {
            static bool Prefix(IntPtr dll, ref bool __result)
            {
                __result = UShoot_Plugin.PlayerControllers[2].GetButton("TRIGGER");
                return false;
            }
        }
        [HarmonyPatch(typeof(dllInstance), "GetP4Frie2")]
        class GetP4Frie2
        {
            static bool Prefix(IntPtr dll, ref bool __result)
            {
                __result = UShoot_Plugin.PlayerControllers[3].GetButton("TRIGGER");
                return false;
            }
        }

        [HarmonyPatch(typeof(dllInstance), "GetP1StartKey")]
        class GetP1StartKey
        {
            static bool Prefix(IntPtr dll, ref bool __result)
            {
                __result = UShoot_Plugin.PlayerControllers[0].GetButton("START");
                return false;
            }
        }
        [HarmonyPatch(typeof(dllInstance), "GetP2StartKey")]
        class GetP2StartKey
        {
            static bool Prefix(IntPtr dll, ref bool __result)
            {
                __result = UShoot_Plugin.PlayerControllers[1].GetButton("START");
                return false;
            }
        }
        [HarmonyPatch(typeof(dllInstance), "GetP3StartKey")]
        class GetP3StartKey
        {
            static bool Prefix(IntPtr dll, ref bool __result)
            {
                __result = UShoot_Plugin.PlayerControllers[2].GetButton("START");
                return false;
            }
        }
        [HarmonyPatch(typeof(dllInstance), "GetP4StartKey")]
        class GetP4StartKey
        {
            static bool Prefix(IntPtr dll, ref bool __result)
            {
                __result = UShoot_Plugin.PlayerControllers[3].GetButton("START");
                return false;
            }
        }

        /// <summary>
        /// Patching here allow to prevent the following calls :
        ///  GetResponcoin1p
        ///  GetResponcoin2p
        ///  GetResponcoin3p
        ///  GetResponcoin4p
        [HarmonyPatch(typeof(dllInstance), "getResponcoin")]
        class getResponcoin
        {
            static bool Prefix(int id, ref bool __result)
            {
                __result = true;
                return false; ;
            }
        }

        [HarmonyPatch(typeof(dllInstance), "GetRightKey")]
        class GetRightKey
        {
            static bool Prefix(IntPtr dll, ref bool __result)
            {
                __result = UShoot_Plugin.GameController.GetButton("MENU_RIGHT");
                return false;
            }
        }

        [HarmonyPatch(typeof(dllInstance), "GetSettingKey")]
        class GetSettingKey
        {
            static bool Prefix(IntPtr dll, ref bool __result)
            {
                __result = UShoot_Plugin.GameController.GetButton("TEST");
                return false;
            }
        }

        /// <summary>
        /// Patching here allow to prevent the following calls :
        ///  GetTicketEmpty1p
        ///  GetTicketEmpty2p
        ///  GetTicketEmpty3p
        ///  GetTicketEmpty4p
        [HarmonyPatch(typeof(dllInstance), "getTicketEmpty")]
        class getTicketEmpty
        {
            static bool Prefix(int id, ref bool __result)
            {
                __result = false;
                return false;
            }
        }

        /// <summary>
        /// Unused ?
        /// </summary>
        [HarmonyPatch(typeof(dllInstance), "GetTicketKey")]
        class GetTicketKey
        {
            static bool Prefix(IntPtr dll, ref bool __result)
            {
                __result = false;
                return false;
            }
        }

        [HarmonyPatch(typeof(dllInstance), "GetUpWaterWarning")]
        class GetUpWaterWarning
        {
            static bool Prefix(IntPtr dll, ref bool __result)
            {
                __result = false;
                return false;
            }
        }

        /// <summary>
        /// Patching here allw to skip the numeros calls to 
        /// GetWadc1pX, GetWadc1pY, Get1PWadcX, etc..
        /// </summary>
        [HarmonyPatch(typeof(dllInstance), "get_gun_pos")]
        class get_gun_pos
        {
            static bool Prefix(int id, bool ___b_test, ref Vector2 __result)
            {
                Vector2 v = UShoot_Plugin.PlayerControllers[id].GetAimingPosition();
                v.x =  v.x / (float)Screen.width;
                v.y = v.y / (float)Screen.height;
                __result = v;
                return false;
            }
        }

        /// <summary>
        /// Replace Axis data
        /// </summary>
        [HarmonyPatch(typeof(dllInstance), "get_gun_pos_01")]
        class get_gun_pos_01
        {
            static bool Prefix(int id, bool ___b_test, ref Vector2 __result)
            {
                Vector2 v = UShoot_Plugin.PlayerControllers[id].GetAimingPosition();
                v.x = v.x / (float)Screen.width;
                v.y = v.y / (float)Screen.height;
                __result = v;
                return false;
            }
        }

        [HarmonyPatch(typeof(dllInstance), "init")]
        class init
        {
            static bool Prefix(IntPtr dll, int nparam, ref int __result)
            {
                __result = 1;
                return false; ;
            }
        }

        [HarmonyPatch(typeof(dllInstance), "SetAtomizerOpen")]
        class SetAtomizerOpen
        {
            static bool Prefix(IntPtr dll, bool nparam)
            {
                return false;
            }
        }

        [HarmonyPatch(typeof(dllInstance), "SetIsSetting")]
        class SetIsSetting
        {
            static bool Prefix(IntPtr dll, int isStop)
            {
                return false;
            }
        }

        [HarmonyPatch(typeof(dllInstance), "SetLetfLight")]
        class SetLetfLight
        {
            static bool Prefix(IntPtr dll, int nparam)
            {
                return false;
            }
        }

        [HarmonyPatch(typeof(dllInstance), "SetLightbelt1")]
        class SetLightbelt1
        {
            static bool Prefix(IntPtr dll, int nparam)
            {
                UShoot_Plugin.OutputData.LightBelt[0] = (byte)nparam;
                return false;
            }
        }
        [HarmonyPatch(typeof(dllInstance), "SetLightbelt2")]
        class SetLightbelt2
        {
            static bool Prefix(IntPtr dll, int nparam)
            {
                UShoot_Plugin.OutputData.LightBelt[1] = (byte)nparam;
                return false;
            }
        }
        [HarmonyPatch(typeof(dllInstance), "SetLightbelt3")]
        class SetLightbelt3
        {
            static bool Prefix(IntPtr dll, int nparam)
            {
                 UShoot_Plugin.OutputData.LightBelt[2] = (byte)nparam;
                return false;
            }
        }
        [HarmonyPatch(typeof(dllInstance), "SetLightbelt4")]
        class SetLightbelt4
        {
            static bool Prefix(IntPtr dll, int nparam)
            {
                UShoot_Plugin.OutputData.LightBelt[3] = (byte)nparam;
                return false;
            }
        }

        [HarmonyPatch(typeof(dllInstance), "SetMidLight")]
        class SetMidLight
        {
            static bool Prefix(IntPtr dll, int nparam)
            {
                return false;
            }
        }

        /// <summary>
        /// Unused ?
        /// </summary>
        [HarmonyPatch(typeof(dllInstance), "SetP1Eggnum")]
        class SetP1Eggnum
        {
            static bool Prefix(IntPtr dll, int nparam, bool bfugai)
            {
                return false;
            }
        }
        /// <summary>
        /// Unused ?
        /// </summary>
        [HarmonyPatch(typeof(dllInstance), "SetP2Eggnum")]
        class SetP2Eggnum
        {
            static bool Prefix(IntPtr dll, int nparam, bool bfugai)
            {
                return false;
            }
        }

        [HarmonyPatch(typeof(dllInstance), "SetP1ElecShake")]
        class SetP1ElecShake
        {
            static bool Prefix(IntPtr dll, int nparam)
            {
                UShoot_Plugin.OutputData.Shake[0] = (byte)nparam;
                return false;
            }
        }
        [HarmonyPatch(typeof(dllInstance), "SetP2ElecShake")]
        class SetP2ElecShake
        {
            static bool Prefix(IntPtr dll, int nparam)
            {
                UShoot_Plugin.OutputData.Shake[1] = (byte)nparam;
                return false;
            }
        }
        [HarmonyPatch(typeof(dllInstance), "SetP3ElecShake")]
        class SetP3ElecShake
        {
            static bool Prefix(IntPtr dll, int nparam)
            {
                UShoot_Plugin.OutputData.Shake[2] = (byte)nparam;
                return false;
            }
        }
        [HarmonyPatch(typeof(dllInstance), "SetP4ElecShake")]
        class SetP4ElecShake
        {
            static bool Prefix(IntPtr dll, int nparam)
            {
                UShoot_Plugin.OutputData.Shake[3] = (byte)nparam;
                return false;
            }
        }

        [HarmonyPatch(typeof(dllInstance), "SetP1GiveSpeed")]
        class SetP1GiveSpeed
        {
            static bool Prefix(IntPtr dll, int nparam)
            {
                return false;
            }
        }
        [HarmonyPatch(typeof(dllInstance), "SetP2GiveSpeed")]
        class SetP2GiveSpeed
        {
            static bool Prefix(IntPtr dll, int nparam)
            {
                return false;
            }
        }
        [HarmonyPatch(typeof(dllInstance), "SetP3GiveSpeed")]
        class SetP3GiveSpeed
        {
            static bool Prefix(IntPtr dll, int nparam)
            {
                return false;
            }
        }
        [HarmonyPatch(typeof(dllInstance), "SetP4GiveSpeed")]
        class SetP4GiveSpeed
        {
            static bool Prefix(IntPtr dll, int nparam)
            {
                return false;
            }
        }

        [HarmonyPatch(typeof(dllInstance), "SetP1GiveSpeed2")]
        class SetP1GiveSpeed2
        {
            static bool Prefix(IntPtr dll, int nparam)
            {
                return false;
            }
        }
        [HarmonyPatch(typeof(dllInstance), "SetP2GiveSpeed2")]
        class SetP2GiveSpeed2
        {
            static bool Prefix(IntPtr dll, int nparam)
            {
                return false;
            }
        }
        
        [HarmonyPatch(typeof(dllInstance), "SetP1WaterElecValue1")]
        class SetP1WaterElecValue1
        {
            static bool Prefix(IntPtr dll, bool nparam)
            {
                return false;
            }
        }
        [HarmonyPatch(typeof(dllInstance), "SetP2WaterElecValue1")]
        class SetP2WaterElecValue1
        {
            static bool Prefix(IntPtr dll, bool nparam)
            {
                return false;
            }
        }
        [HarmonyPatch(typeof(dllInstance), "SetP3WaterElecValue1")]
        class SetP3WaterElecValue1
        {
            static bool Prefix(IntPtr dll, bool nparam)
            {
                return false;
            }
        }
        [HarmonyPatch(typeof(dllInstance), "SetP4WaterElecValue1")]
        class SetP4WaterElecValue1
        {
            static bool Prefix(IntPtr dll, bool nparam)
            {
                return false;
            }
        }

        [HarmonyPatch(typeof(dllInstance), "SetP1WaterElecValue2")]
        class SetP1WaterElecValue2
        {
            static bool Prefix(IntPtr dll, bool nparam)
            {
                return false;
            }
        }
        [HarmonyPatch(typeof(dllInstance), "SetP2WaterElecValue2")]
        class SetP2WaterElecValue2
        {
            static bool Prefix(IntPtr dll, bool nparam)
            {
                return false;
            }
        }
        [HarmonyPatch(typeof(dllInstance), "SetP3WaterElecValue2")]
        class SetP3WaterElecValue2
        {
            static bool Prefix(IntPtr dll, bool nparam)
            {
                return false;
            }
        }
        [HarmonyPatch(typeof(dllInstance), "SetP4WaterElecValue2")]
        class SetP4WaterElecValue2
        {
            static bool Prefix(IntPtr dll, bool nparam)
            {
                return false;
            }
        }

        

        /// <summary>
        ///Patching here allow to prevent the following calls :
        /// SetP1RollBall
        /// SetP2RollBall
        /// </summary>
        [HarmonyPatch(typeof(dllInstance), "setroll")]
        class setroll
        {
            static bool Prefix(int id, bool i_b)
            {
                return false;
            }
        }

        /// <summary>
        ///Patching here allow to prevent the following calls : 
        /// SetP1ShootBall
        /// SetP2ShootBall
        /// </summary>
        [HarmonyPatch(typeof(dllInstance), "setshoot")]
        class setshoot
        {
            static bool Prefix(int id, bool i_b)
            {
                return false;
            }
        }  
      
        /// <summary>
        ///Patching here allow to prevent the following calls :
        /// SetP1PrintTicket
        /// SetP2PrintTicket
        /// SetP3PrintTicket
        /// SetP4PrintTicket
        /// </summary>
        [HarmonyPatch(typeof(dllInstance), "setTicket")]
        class setTicket
        {
            static bool Prefix(int id, int num)
            {
                if (UIback.noticket == 0)
                {                    
                    UIback.saveaddpiao(num);
                }
                return false;
            }
        }        

        [HarmonyPatch(typeof(dllInstance), "SetWaterAtomizer")]
        class SetWaterAtomizer
        {
            static bool Prefix(IntPtr dll, bool nparam)
            {
                UShoot_Plugin.OutputData.Atomizer = nparam ? (byte)1 : (byte)0;
                return false;
            }
        }
        
        [HarmonyPatch(typeof(dllInstance), "SetWaterpump")]
        class SetWaterpump
        {
            static bool Prefix(IntPtr dll, bool nparam)
            {
                //DemulShooter_Plugin.MyLogger.LogMessage("dllInstance.SetWaterpump(): nparam=" + nparam);
                return false;
            }
        }

        /// <summary>
        /// Patching here allow to prevent the following calls :
        ///  SetStartButtomLight1p
        ///  SetStartButtomLight2p
        ///  SetStartButtomLight3p
        ///  SetStartButtomLight4p
        [HarmonyPatch(typeof(dllInstance), "set_startkey_light")]
        class set_startkey_light
        {
            static bool Prefix(int id, int nparam)
            {
                UShoot_Plugin.OutputData.StartLight[id] = (byte)nparam;
                return false;
            }
        }

        /// <summary>
        /// Patching here allow to prevent the following calls :
        ///  SubBufferCoin1p
        ///  SubBufferCoin2p
        ///  SubBufferCoin3p
        ///  SubBufferCoin4p
        [HarmonyPatch(typeof(dllInstance), "subCoin")]
        class subCoin
        {
            static bool Prefix(int id, int count)
            {
                UShoot_Plugin.BufferCoin[id] -= count;
                if (UShoot_Plugin.BufferCoin[id] < 0)
                {
                    UShoot_Plugin.BufferCoin[id] = 0;
                    UShoot_Plugin.MyLogger.LogWarning("dllInstance.subCoin(" + id + ", " + count + ") : BufferCoin[id] is below Zero");  
                }
                return false;
            }
        }

        #endregion

        /// <summary>
        /// Initial function calls for mouse buttons even if Native DLL is OK
        /// This causes issues with multiple lightguns....
        /// </summary>
        [HarmonyPatch(typeof(dllInstance), "get_player_shoot")]
        class get_player_shoot
        {
            static bool Prefix(int playerid, ref bool __result)
            {
                __result = false;

                if (dllInstance.getdlldata(playerid).biosucees)
                {
                    gamedata.m_playerdata[playerid].b_shoot = (gamedata.m_playerdata[playerid].can_shoot && (dllInstance.getdlldata(playerid).inputkey[UIback.GETBALL] || dllInstance.getdlldata(playerid).inputkey[UIback.GETJIQIANG] || UIback.autoshoot == 1));
                    if (gamedata.m_playerdata[playerid].b_shoot)
                    {
                        __result = true;
                    }
                }
                else
                {
                    if (Input.GetMouseButton(1) && playerid == 1)
                    {
                        __result = true;
                    }
                    if (Input.GetKey(KeyCode.Q))
                    {
                        __result = true;
                    }
                }
                return false;
            }
        }
    }
}
