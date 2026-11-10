using System;
using System.Reflection;
using HarmonyLib;
using UnityPlugin_BepInEx.Core;
using Virtuallyz.VRShooter.IO;

namespace BepInEx_UShoot_Plugin
{    
    class mScreenDuckHunt_InOutSystem
    {
        /// <summary>
        /// Replacing the Cursor Locked command by a flag of our own
        /// Cursor will stay unlocked and info is stored for the mouse update function
        /// </summary>
        [HarmonyPatch(typeof(ScreenDuckHunt_InOutSystem), "ResetCursorNormalState")]
        class ResetCursorNormalState
        {
            static bool Prefix(ref bool ___UIMode)
            {
                UShoot_Plugin.MyLogger.LogMessage("mScreenDuckHunt_InOutSystem.ResetCursorNormalState()");
                ___UIMode = false;
                //Cursor.lockState = CursorLockMode.Locked;
                UShoot_Plugin.IsMouseLockedRequired = true;
                return false;
            }
        }

        /// <summary>
        /// Replacing the Cursor Locked command by a flag of our own
        /// Cursor will stay unlocked and info is stored for the mouse update function
        /// </summary>
        [HarmonyPatch(typeof(ScreenDuckHunt_InOutSystem), "UnlockCursor")]
        class UnlockCursor
        {
            static bool Prefix(ref bool ___UIMode, bool keepWeapon, bool withPointers = true)
            {
                UShoot_Plugin.MyLogger.LogMessage("mScreenDuckHunt_InOutSystem.UnlockCursor()");
                ___UIMode = true;
                //Cursor.lockState = CursorLockMode.None;
                UShoot_Plugin.IsMouseLockedRequired = false;
                return false;
            }
        }

        /// <summary>
        /// Use Demulshooter command to control the players actions
        /// </summary>
        [HarmonyPatch(typeof(Virtuallyz.VRShooter.IO.ScreenDuckHunt_InOutSystem), "OnUpdate")]
        class OnUpdate
        {
            static bool Prefix(ScreenDuckHunt_InOutSystem __instance)
            {
                /* Custom Inputs for Player 1 */
                if (UShoot_Plugin.PlayerControllers[(int)UShoot_Plugin.PlayerType.Player1].GetButtonDown("TRIGGER"))
                {
                    UseMethodFromName(__instance, "FirePressed", new object[] { 0 });
                }
                if (UShoot_Plugin.PlayerControllers[(int)UShoot_Plugin.PlayerType.Player1].GetButton("TRIGGER"))
                {
                    UseMethodFromName(__instance, "FireHold", new object[] { 0 });
                }
                if (UShoot_Plugin.PlayerControllers[(int)UShoot_Plugin.PlayerType.Player1].GetButtonUp("TRIGGER"))
                {
                    UseMethodFromName(__instance, "FireReleased", new object[] { 0 });
                }

                if (UShoot_Plugin.PlayerControllers[(int)UShoot_Plugin.PlayerType.Player1].GetButtonDown("RELOAD"))
                {
                    UseMethodFromName(__instance, "Reload", new object[] { 0 });
                }
                if (UShoot_Plugin.PlayerControllers[(int)UShoot_Plugin.PlayerType.Player1].GetButtonUp("RELOAD"))
                {
                    UseMethodFromName(__instance, "ReloadReleased", new object[] { 0 });
                }

                if (UShoot_Plugin.PlayerControllers[(int)UShoot_Plugin.PlayerType.Player1].GetButtonDown("CHANGE_WEAPON"))
                {
                    UseMethodFromName(__instance, "SelectNextWeapon", new object[] { 0 });
                }

                /* Custom Inputs for Player 2 */
                if (UShoot_Plugin.PlayerControllers[(int)UShoot_Plugin.PlayerType.Player2].GetButtonDown("TRIGGER"))
                {
                    UseMethodFromName(__instance, "FirePressed", new object[] { 1 });
                }
                if (UShoot_Plugin.PlayerControllers[(int)UShoot_Plugin.PlayerType.Player2].GetButton("TRIGGER"))
                {
                    UseMethodFromName(__instance, "FireHold", new object[] { 1 });
                }
                if (UShoot_Plugin.PlayerControllers[(int)UShoot_Plugin.PlayerType.Player2].GetButtonUp("TRIGGER"))
                {
                    UseMethodFromName(__instance, "FireReleased", new object[] { 1 });
                }


                if (UShoot_Plugin.PlayerControllers[(int)UShoot_Plugin.PlayerType.Player2].GetButtonDown("RELOAD"))
                {
                    UseMethodFromName(__instance, "Reload", new object[] { 1 });
                }
                if (UShoot_Plugin.PlayerControllers[(int)UShoot_Plugin.PlayerType.Player2].GetButtonUp("RELOAD"))
                {
                    UseMethodFromName(__instance, "ReloadReleased", new object[] { 1 });
                }

                if (UShoot_Plugin.PlayerControllers[(int)UShoot_Plugin.PlayerType.Player2].GetButtonDown("CHANGE_WEAPON"))
                {
                    UseMethodFromName(__instance, "SelectNextWeapon", new object[] { 1 });
                }

                //Adding a keyboard key for Grenade P1
                if (UShoot_Plugin.PlayerControllers[0].GetButtonDown("GRENADE"))
                {
                    UseMethodFromName(__instance, "SelectGrenade", new object[] { 0 });
                }
                if (UShoot_Plugin.PlayerControllers[0].GetButton("GRENADE"))
                {
                    UseMethodFromName(__instance, "HoldGrenade", new object[] { 0 });
                }
                if (UShoot_Plugin.PlayerControllers[0].GetButtonUp("GRENADE"))
                {
                    UseMethodFromName(__instance, "UnSelectAnyGrenade", new object[] { 0 });
                }

                //Adding a keyboard key for Grenade P2
                if (UShoot_Plugin.PlayerControllers[1].GetButtonDown("GRENADE"))
                {
                    UseMethodFromName(__instance, "SelectGrenade", new object[] { 1 });
                }
                if (UShoot_Plugin.PlayerControllers[1].GetButton("GRENADE"))
                {
                    UseMethodFromName(__instance, "HoldGrenade", new object[] { 1 });
                }
                if (UShoot_Plugin.PlayerControllers[1].GetButtonUp("GRENADE"))
                {
                    UseMethodFromName(__instance, "UnSelectAnyGrenade", new object[] { 1 });
                }

                return true;
            }
        }
        private static void UseMethodFromName(ScreenDuckHunt_InOutSystem Instance, string MethodName, object[] Parameters)
        {
            //OpWolf_Plugin.MyLogger.LogMessage("Searching for the following method : " + MethodName);
            try
            {
                MethodInfo m = Instance.GetType().GetMethod(MethodName, BindingFlags.Instance | BindingFlags.NonPublic);
                if (m != null)
                {
                    //OpWolf_Plugin.MyLogger.LogMessage("Found " + MethodName + ", Invoking... ");
                    m.Invoke(Instance, Parameters);
                }
            }
            catch (Exception Ex)
            {
                UShoot_Plugin.MyLogger.LogError("Error trying to run method " + MethodName + " from " + Instance.GetType().ToString() + " : " + Ex.Message.ToString());
            }
        }

        /// <summary>
        /// Disable base controll for some actions in the game, to replace them later by DemulShooter
        /// Putting back the other one that we need to be enabled
        /// </summary>
        [HarmonyPatch(typeof(Virtuallyz.VRShooter.IO.ScreenDuckHunt_InOutSystem), "HookInput")]
        class HookInput
        {
            static bool Prefix(ScreenDuckHunt_InOutSystem __instance, ScreenView ___CurrentView)
            {
                AddEvent(__instance, ___CurrentView, "InvokeAnyButtonPushed", "AnyButtonPushed");
                AddEvent2(__instance, ___CurrentView, "InvokeOnSkipButtonDown", "SkipButtonDown");
                AddEvent2(__instance, ___CurrentView, "InvokeOnSkipButtonUp", "SkipButtonUp");
                AddEvent(__instance, ___CurrentView, "InvokeOnSkipButtonHold", "SkipButtonHold");
                //AddEvent(__instance, ___CurrentView, "InvokeOnResetButtonDown", "ResetButtonDown");
                //AddEvent(__instance, ___CurrentView, "InvokeOnResetButtonUp", "ResetButtonUp");
                AddEvent(__instance, ___CurrentView, "UpdateDone", "UpdateDone");
                //AddEvent(__instance, ___CurrentView, "MouseLeftButtonPressed", "FirePressed");
                //AddEvent(__instance, ___CurrentView, "MouseLeftButtonHold", "FireHold");
                //AddEvent(__instance, ___CurrentView, "MouseLeftButtonReleased", "FireReleased");
                //AddEvent(__instance, ___CurrentView, "OnMissingControllerTriggered", "MissingControllerTriggered");
                //AddEvent(__instance, ___CurrentView, "SelectGrenade", "MouseRightButtonPressed");
                //AddEvent(__instance, ___CurrentView, "HoldGrenade", "MouseRightButtonHold");
                //AddEvent(__instance, ___CurrentView, "UnSelectAnyGrenade", "MouseRightButtonReleased");
                AddEvent(__instance, ___CurrentView, "WeaponSelectInterface", "WeaponNumberUpdate");
                AddEvent(__instance, ___CurrentView, "UIMove", "DirectionalArrowsUpdate");
                //AddEvent(__instance, ___CurrentView, "SelectGrenade", "GButtonPressed");
                //AddEvent(__instance, ___CurrentView, "HoldGrenade", "GButtonHold");
                //AddEvent(__instance, ___CurrentView, "UnSelectAnyGrenade", "GButtonReleased");
                AddEvent(__instance, ___CurrentView, "OnUISelectReleased", "SelectButtonReleased");
                AddEvent(__instance, ___CurrentView, "OnUISelectPushed", "SelectButtonPressed");
                AddEvent(__instance, ___CurrentView, "OnUISelectBack", "BackButtonReleased");

                AddEvent(__instance, ___CurrentView, "UseHeal", "HealButtonPressed");
                AddEvent(__instance, ___CurrentView, "InvokeCoverPushed", "CrouchKeyPressed");
                AddEvent(__instance, ___CurrentView, "InvokeCoverHold", "CrouchKeyHold");
                AddEvent(__instance, ___CurrentView, "InvokeCoverReleased", "CrouchKeyReleased");
                AddEvent(__instance, ___CurrentView, "OnSettingsButtonPushed", "SettingsButtonClicked");
                AddEvent(__instance, ___CurrentView, "OnStartModeController", "StartModeController");

                return false;
            }
        }
        public static void AddEvent(ScreenDuckHunt_InOutSystem Instance, ScreenView CurrentView, string Method, string Event)
        {
            PlayerInOutSystem playerSystem = Instance as PlayerInOutSystem;

            MethodInfo mInfo = playerSystem.GetType().GetMethod(Method, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            if (mInfo != null)
            {
                EventInfo eventInfo = CurrentView.GetType().GetEvent(Event);
                if (eventInfo != null)
                {
                    Type eventType = eventInfo.EventHandlerType;
                    Delegate del = Delegate.CreateDelegate(eventType, Instance, mInfo);
                    eventInfo.AddEventHandler(CurrentView, del);
                    UShoot_Plugin.MyLogger.LogMessage("Succesfully added handler '" + Method + "' to event '" + Event + "'");
                }
                else
                    UShoot_Plugin.MyLogger.LogError("Impossible to find Event " + Event);
            }
            else
                UShoot_Plugin.MyLogger.LogError("Impossible to find Method " + Method);
        }

        //Multiple MethodDefinition, can cause bugs
        public static void AddEvent2(ScreenDuckHunt_InOutSystem Instance, ScreenView CurrentView, string Method, string Event)
        {
            PlayerInOutSystem playerSystem = Instance as PlayerInOutSystem;

            MethodInfo[] mInfo = playerSystem.GetType().GetMethods(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            foreach (MethodInfo m in mInfo)
            {
                if (m.Name.Equals(Method))
                {
                    try
                    {
                        EventInfo eventInfo = CurrentView.GetType().GetEvent(Event);
                        if (eventInfo != null)
                        {
                            Type eventType = eventInfo.EventHandlerType;
                            UShoot_Plugin.MyLogger.LogMessage("Found " + m + " of Type " + eventType.ToString());
                            Delegate del = Delegate.CreateDelegate(eventType, Instance, m);
                            eventInfo.AddEventHandler(CurrentView, del);
                            UShoot_Plugin.MyLogger.LogMessage("Succesfully added handler '" + Method + "' to event '" + Event + "'");
                        }
                        else
                            UShoot_Plugin.MyLogger.LogError("Impossible to find Event " + Event);
                    }
                    catch { }
                }
            }

        }
    }
}
