using HarmonyLib;
using UnityEngine.SceneManagement;

namespace BepInEx_UShoot_Plugin.Patch
{
    class mInputController
    {
        [HarmonyPatch(typeof(InputController), "Update")]
        class Update
        {
            static bool Prefix(InputController __instance)
            {
                for (int i = 0; i < (int)GameData.GamePlayerCount; i++)
                {                   
                    if (UShoot_Plugin.PlayerControllers[i].GetButtonDown("COIN"))
                    {
                        if (__instance.InCoinAction != null)
                            __instance.InCoinAction((PlayerType)i, 1);
                    }

                    if (UShoot_Plugin.PlayerControllers[i].GetButtonDown("TRIGGER"))
                    {                        
                        if (__instance.ShootDownAction != null)
                            __instance.ShootDownAction((PlayerType)i);
                    }
                    if (UShoot_Plugin.PlayerControllers[i].GetButton("TRIGGER"))
                    {
                        if (__instance.ShootAction != null)
                            __instance.ShootAction((PlayerType)i, UShoot_Plugin.PlayerControllers[i].GetAimingPosition());
                    }
                    if (UShoot_Plugin.PlayerControllers[i].GetButtonUp("TRIGGER"))
                    {
                        if (__instance.ShootUpAction != null)
                            __instance.ShootUpAction((PlayerType)i);
                    }

                    if (UShoot_Plugin.PlayerControllers[i].GetButtonDown("CHANGE_WEAPON"))
                    {
                        if (GameData.GetPlayerData((PlayerType)i).playing)
                        {
                            if (__instance.ChangeAction != null)
                                __instance.ChangeAction((PlayerType)i);
                        }
                    }

                    if (UShoot_Plugin.PlayerControllers[i].GetButtonDown("RELOAD"))
                    {
                        if (GameData.GetPlayerData((PlayerType)i).playing)
                        {
                            if (__instance.AddBulletAction != null)
                                __instance.AddBulletAction((PlayerType)i);
                        }
                    }
                }

                if (UShoot_Plugin.GameController.GetButtonDown("TEST"))
                    __instance.IntoSetting();

                if (SceneManager.GetActiveScene().name.Equals("Setting") || SceneManager.GetActiveScene().name.Equals("LoadSetting"))
                {
                    //Up, down, select, etc...

                    if (UShoot_Plugin.GameController.GetButtonDown("MENU_RIGHT"))
                    {
                        if (__instance.SettingMenuAction != null)
                            __instance.SettingMenuAction();
                    }
                    if (UShoot_Plugin.GameController.GetButtonDown("MENU_LEFT"))
                    {
                        if (__instance.SettingUpAction != null)
                            __instance.SettingUpAction();
                    }
                    if (UShoot_Plugin.GameController.GetButtonDown("MENU_DOWN"))
                    {
                        if (__instance.SettingAction != null)
                            __instance.SettingAction();
                    }
                }

                return false;
            }
        }
    }
}
