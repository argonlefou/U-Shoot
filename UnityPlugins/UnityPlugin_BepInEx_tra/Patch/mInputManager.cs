using HarmonyLib;
using SBK;
using SBK.Audio;
using TRA.Arcade.Manager;
using UnityEngine;

namespace BepInEx_UShoot_Plugin
{
    class mInputManager
    {
        [HarmonyPatch(typeof(InputManager), "MouseControl")]
        class MouseControl
        {
            static bool Prefix(bool ___m_AreCrosshairsEnabled, bool[] ___m_HoldTrigger, ID[] ___m_IDResolution)
            {
                Vector3 i_Arg = new Vector3();
                Player[] playerList = Singleton<PlayerManager>.Instance.m_PlayerList;

                if (!Singleton<GameplayManager>.Instance.IsDemo)
                {
                    for (int i = 0; i < playerList.Length; i++)
                    {
                        i_Arg = UShoot_Plugin.PlayerControllers[i].GetAimingPosition();
                        i_Arg.x = i_Arg.x / (float)Screen.width;
                        i_Arg.y = i_Arg.y / (float)Screen.height;

                        /*if (i == 0)
                        {
                            if ((Singleton<ArcadeManager>.Instance.GunEnabled(i) && playerList[i].IsPlaying()) || Singleton<WindowManager>.Instance.GetWindow(WindowID.ID.MainMenuWindow))
                            {
                                Singleton<PlayerManager>.Instance.PlayerMoveCrosshair(i_Arg, ID.One);
                            }
                        }
                        else
                        {
                            if (Singleton<ArcadeManager>.Instance.GunEnabled(i) && playerList[i].IsPlaying())
                            {
                                Singleton<PlayerManager>.Instance.PlayerMoveCrosshair(i_Arg, (ID)i);
                            }
                        }*/

                        //Enabling selection screen for every player at the same time, if not : only P1 can choose level/mode
                        if ((Singleton<ArcadeManager>.Instance.GunEnabled(i) && playerList[i].IsPlaying()) || Singleton<WindowManager>.Instance.GetWindow(WindowID.ID.MainMenuWindow))
                        {
                            Singleton<PlayerManager>.Instance.PlayerMoveCrosshair(i_Arg, (ID)i);
                        }
                    }
                }

                for (int i = 0; i < 4; i++)
                {
                    i_Arg = UShoot_Plugin.PlayerControllers[i].GetAimingPosition();
                    i_Arg.x = i_Arg.x / (float)Screen.width;
                    i_Arg.y = i_Arg.y / (float)Screen.height;

                    if (Singleton<ArcadeManager>.Instance.GunEnabled(i))
                    {
                        if (UShoot_Plugin.PlayerControllers[i].GetButton("TRIGGER") && Singleton<PlayerManager>.Instance.GetPlayerByID((ID)i).Alive)
                        {
                            Singleton<PlayerManager>.Instance.PlayerPressTrigger(i_Arg, (ID)i);
                            if (___m_AreCrosshairsEnabled)
                            {
                                Singleton<PlayerManager>.Instance.PlayerShoot(i_Arg, (ID)i, ___m_HoldTrigger[i], true);
                                if (!___m_HoldTrigger[i])
                                {
                                    ___m_HoldTrigger[i] = true;
                                }
                            }
                            else if (!___m_HoldTrigger[i])
                            {
                                ___m_HoldTrigger[i] = true;
                                if (Singleton<GameplayManager>.Instance.CurrentState == GameplayManager.State.PlayState && !Singleton<VideoPlaybackManager>.Instance.IsPlaying)
                                {
                                    Singleton<PlayerManager>.Instance.PlayPlayerSound(AudioID_Global.player_cantshoot, ___m_IDResolution[i]);
                                }
                            }
                        }
                        if (UShoot_Plugin.PlayerControllers[i].GetButton("RELOAD"))
                        {
                            Singleton<PlayerManager>.Instance.PlayerReload((ID)i);
                        }
                    }
                    if (Singleton<ArcadeManager>.Instance.GunEnabled(i) && !UShoot_Plugin.PlayerControllers[i].GetButton("TRIGGER"))
                    {
                        if (___m_AreCrosshairsEnabled)
                        {
                            Singleton<PlayerManager>.Instance.PlayerTriggerRelease((ID)i);
                        }
                        ___m_HoldTrigger[i] = false;
                    }

                    if (!___m_HoldTrigger[i])
                    {
                        Singleton<PlayerManager>.Instance.StopHoldingFire((ID)i);
                    }
                }

                return false;
            }
        }         
    }
}
