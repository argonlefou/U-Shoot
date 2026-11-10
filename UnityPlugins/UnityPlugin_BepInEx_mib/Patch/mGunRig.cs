using HarmonyLib;
using MIB;
using UnityEngine;

namespace BepInEx_UShoot_Plugin.Patch
{
    internal class mGunRig
    {
        [HarmonyPatch(typeof(GunRig), "Update")]
        class Update
        {
            static bool Prefix(GunRig __instance, ref float ___RecoilTimer, ref bool ___RecoilActive, Gun ___Current, Crosshair ___PlayerCrosshair)
            {
                bool flag = false;
                if (__instance.PlayerIndex == 0 && MIBCore.Instance.Players != 2)
                {
                    if (__instance.CanRecoil)
                    {
                        if (!MIBCore.DisableInputs)
                        {
                            if (UShoot_Plugin.PlayerControllers[0].GetButton("TRIGGER"))
                            {
                                flag = true;
                                ___RecoilTimer = 0.25f;
                            }
                            else if (!UShoot_Plugin.PlayerControllers[0].GetButton("TRIGGER"))
                            {
                                flag = false;
                            }
                        }
                    }
                    else
                    {
                        flag = false;
                    }
                    if (flag)
                    {
                        if (!___RecoilActive)
                        {
                            Lights.Play((__instance.PlayerIndex == 0) ? "GUN1_FIRE" : "GUN2_FIRE");
                            if (!___Current.FinishRecoil)
                            {
                                MIBCore.Instance.SendMessage("TriggerRecoil", new MIBData_Recoil((byte)(__instance.PlayerIndex + 1), ___Current.FinishRecoil, true, (byte)___Current.GunRecoil));
                            }
                            ___RecoilActive = true;
                        }
                    }
                    else
                    {
                        if (___RecoilActive && ___RecoilTimer <= 0f)
                        {
                            Lights.Play((__instance.PlayerIndex == 0) ? "GUN1_OFF" : "GUN2_OFF");
                            if (!___Current.FinishRecoil)
                            {
                                MIBCore.Instance.SendMessage("TriggerRecoil", new MIBData_Recoil((byte)(__instance.PlayerIndex + 1), ___Current.FinishRecoil, false, (byte)___Current.GunRecoil));
                            }
                            ___RecoilActive = false;
                        }
                        ___RecoilTimer -= Time.deltaTime;
                    }
                    if (___RecoilActive)
                    {
                        ___Current.DoFire(false);
                        ___PlayerCrosshair.StopSpinning();
                        //DemulShooter_Plugin.MyLogger.LogMessage("GunRig.Update(): DoFire P1");
                    }
                    else
                    {
                        ___PlayerCrosshair.StartSpinning();
                    }
                }

                //Player 2
                if (__instance.PlayerIndex == 1 && MIBCore.Instance.Players != 1)
                {
                    if (__instance.CanRecoil)
                    {
                        if (!MIBCore.DisableInputs)
                        {
                            if (UShoot_Plugin.PlayerControllers[1].GetButton("TRIGGER"))
                            {
                                flag = true;
                                ___RecoilTimer = 0.25f;
                            }
                            else if (!UShoot_Plugin.PlayerControllers[1].GetButton("TRIGGER"))
                            {
                                flag = false;
                            }
                        }
                    }
                    else
                    {
                        flag = false;
                    }
                    if (flag)
                    {
                        if (!___RecoilActive)
                        {
                            Lights.Play("GUN2_FIRE");
                            if (!___Current.FinishRecoil)
                            {
                                MIBCore.Instance.SendMessage("TriggerRecoil", new MIBData_Recoil((byte)(__instance.PlayerIndex + 1), ___Current.FinishRecoil, true, (byte)___Current.GunRecoil));
                            }
                            ___RecoilActive = true;
                        }
                    }
                    else
                    {
                        if (___RecoilActive && ___RecoilTimer <= 0f)
                        {
                            Lights.Play("GUN2_OFF");
                            if (!___Current.FinishRecoil)
                            {
                                MIBCore.Instance.SendMessage("TriggerRecoil", new MIBData_Recoil((byte)(__instance.PlayerIndex + 1), ___Current.FinishRecoil, false, (byte)___Current.GunRecoil));
                            }
                            ___RecoilActive = false;
                        }
                        ___RecoilTimer -= Time.deltaTime;
                    }
                    if (___RecoilActive)
                    {
                        ___Current.DoFire(false);
                        ___PlayerCrosshair.StopSpinning();
                        //DemulShooter_Plugin.MyLogger.LogMessage("GunRig.Update(): DoFire P2");
                        return false;
                    }
                    ___PlayerCrosshair.StartSpinning();
                }
                return false;
            }
        }
    }

    [HarmonyPatch(typeof(GunRig), "ShowGunUI")]
    class ShowGunUI
    {
        static void Postfix(ref bool ___HideUi, ref bool ___GunLower, GunRig __instance)
        {
            if (Input.GetKeyDown(KeyCode.H))
            {
                UShoot_Plugin.MyLogger.LogMessage("GunRig.ShowGunUI()");
                ___HideUi = false;
                ___GunLower = true;
            }
        }
    }
}
