using GameBase;
using HarmonyLib;

namespace BepInEx_USoot_Plugin.Patch
{
    internal class mIOManager
    {
        /// <summary>
        /// Setting DOG Ok once and for all instead of doing it in the threaded loop
        /// </summary>
        [HarmonyPatch(typeof(GameBase.IO.IOManager), "Open")]
        class Open
        {
            static void Postfix(ref bool ___isDogError, GameBase.IO.IOManager __instance)
            {
                UShoot_Plugin.MyLogger.LogMessage("GameBase.IO.IOManager.Open()");
                ___isDogError = false;

                __instance.IOController.keyBus.keyController.SetKeyState(SysDefine.key.DOG_ERROR, 0);
                __instance.IOController.keyBus.keyController.SetKeyState(SysDefine.key.DOG_OK, 1);
            }
        }

        /// <summary>
        /// Removing guts of the trhead so that it can terminate and not use useless resources
        /// </summary>
        [HarmonyPatch(typeof(GameBase.IO.IOManager), "ThreadCheckDog")]
        class ThreadCheckDog
        {
            static bool Prefix()
            {
                return false;
            }
        }
    }
}
