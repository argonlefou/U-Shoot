using BepInEx.Unity.IL2CPP.UnityEngine;
using HarmonyLib;
using System;
using UnityEngine;
using static SkyrideUtils;

namespace BepInEx_UShoot_Plugin.Patch
{
    internal class _Unity
    {
        /// <summary>
        /// Game automatically ask for set_Fullscreen() whatever we choose
        /// Disabling that function will stop the game from changing screen mode after we set our own in ApplicationManager::Awake()
        /// Note that it does not prevent us to choose full screen or ALT+RETURN during game
        /// </summary>
        [HarmonyPatch(typeof(UnityEngine.Screen), "set_fullScreen")]
        class set_fullScreen
        {
            static bool Prefix(ref bool value)
            {
                return false;
            }
        }

        /// <summary>
        /// GetKey is used to populate custom keys when UShoot is not running
        /// Original game only uses it for ALT and SHIFT keys, we can let it unfiltered
        /// </summary>
        [HarmonyPatch(typeof(UnityEngine.Input), "GetKey", new Type[] { typeof(UnityEngine.KeyCode) })]
        class GetKeyc
        {
            static void Prefix(UnityEngine.KeyCode key)
            {
                //UShoot_Plugin.MyLogger.LogMessage("GETKEY :" + key.ToString() + ")");
            }
        }

        /// <summary>
        /// Remapping call for TEST, and COINs buttons and removing all other cheat KEYS
        /// </summary>
        [HarmonyPatch(typeof(UnityEngine.Input), "GetKeyDown", new Type[] { typeof(UnityEngine.KeyCode) })]
        class GetKeyDownc
        {
            static void Postfix(UnityEngine.KeyCode key, ref bool __result)
            {
                if (key == UnityEngine.KeyCode.Alpha1)
                    __result = UShoot_Plugin.PlayerControllers[0].GetButtonDown("COIN");
                else if (key == UnityEngine.KeyCode.Alpha2)
                    __result = UShoot_Plugin.PlayerControllers[1].GetButtonDown("COIN");
                else if (key == UnityEngine.KeyCode.O)
                    __result = UShoot_Plugin.GameController.GetButtonDown("TEST");
                else if (key == UnityEngine.KeyCode.Escape)
                    __result = UShoot_Plugin.GameController.GetButtonDown("EXIT");
                else
                    __result = false;
            }
        }

        /// <summary>
        /// Some screens (LevelSelect) are still using mouse buttons, so deactivating them
        /// Custom buttons rely only on GetMouseButton, which is left untouched
        /// </summary>
        [HarmonyPatch(typeof(UnityEngine.Input), "GetMouseButtonDown")]
        class GetMouseButtonDown
        {
            static bool Prefix(int button, ref bool __result)
            {
                return false;
            }
        }

        // <summary>
        /// Change data/save folder location to game folder
        /// </summary>
        [HarmonyPatch(typeof(UnityEngine.Application), "persistentDataPath", MethodType.Getter)]
        class persistentDataPath
        {
            static void Postfix(ref string __result)
            {
                if (UShoot_Plugin.LocalSave)
                {
                    __result = BepInEx.Paths.GameRootPath + "/NVRAM/";
                }
            }
        }

    }
}
