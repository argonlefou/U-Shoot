using HarmonyLib;
using UnityEngine;

namespace BepInEx_UShoot_Plugin
{
    class mNewInputManager
    {
        /// <summary>
        /// The original function is lopp-called when gun are not detected to mouse-control P1 and P2
        /// By overriding it, we can inject data for all 4 players inside
        /// </summary>
        [HarmonyPatch(typeof(NewInputManager), "MouseControl")]
        class MouseControl
        {
            static bool Prefix (bool ___m_AllInputEnable, bool ___m_AreCrosshairsEnabled, bool[] ___m_HoldTrigger, NewInputManager __instance)
            {
                //UnityEngine.Debug.Log("mNewInputManager.MouseControl()");     
                if (___m_AllInputEnable)
                {
                    Vector3 i_Arg = new Vector3();

                    for (int i = 0; i < 4; i++)
                    {
                        if (SBK.Singleton<ArcadeManager>.Instance.GunEnabled(i))
                        {
                            i_Arg.x = UShoot_Plugin.PlayerControllers[i].Axis_X / (float)Screen.width;
                            i_Arg.y = UShoot_Plugin.PlayerControllers[i].Axis_Y / (float)Screen.height;
                            EventManager<Vector3, ID>.TriggerEvent(NewInputManager.BaseMessage.PlayerCrosshairMove.ToString(), i_Arg, (ID)i);
                        }

                        if (UShoot_Plugin.PlayerControllers[i].GetButton("TRIGGER"))
                        {
                            //Rha_Plugin.MyLogger.LogMessage("mNewInputManager.MouseControl(" + i + ") => Trigger Pushed");
                            EventManager<Vector3, ID>.TriggerEvent(NewInputManager.BaseMessage.PlayerPressTrigger.ToString(), i_Arg, (ID)i);
                            if (___m_AreCrosshairsEnabled)
                            {
                                EventManager<Vector3, ID>.TriggerEvent(NewInputManager.BaseMessage.PlayerShoot.ToString(), i_Arg, (ID)i);
                            }
                            else if (!___m_HoldTrigger[i])
                            {
                                ___m_HoldTrigger[i] = true;
                                if (SBK.Singleton<ArcadeGameplayManager>.Instance.CurrentState == ArcadeGameplayManager.State.PlayState)
                                {
                                    SBK.Singleton<SBK.Audio.AudioManager>.Instance.Play(SBK.Audio.AudioID_Global.ui_cantshoot, null);
                                }
                            }
                        }
                        else if (UShoot_Plugin.PlayerControllers[i].GetButtonUp("TRIGGER"))
                        {
                            //Rha_Plugin.MyLogger.LogMessage("mNewInputManager.MouseControl(" + i + ") => Trigger Released");
                            if (___m_AreCrosshairsEnabled)
                            {
                                EventManager<ID>.TriggerEvent(NewInputManager.BaseMessage.PlayerTriggerRelease.ToString(), (ID)i);
                            }
                            ___m_HoldTrigger[i] = false;
                        }
                    }
                }
                return false;
            }
        }        
    }
}
