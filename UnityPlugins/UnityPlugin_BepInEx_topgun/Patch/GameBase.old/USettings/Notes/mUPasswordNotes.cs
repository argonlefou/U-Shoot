using HarmonyLib;

namespace TopGun_BepInEx_DemulShooter_Plugin.Patch
{
    internal class mUPasswordNotes
    {
        /// <summary>
        /// Bypassing password protected fields by changing the inner password to the one typed in the window when the game asks for check
        /// </summary>
        [HarmonyPatch(typeof(GameBase.USettings.Notes.UPasswordNotes), "OnConfirm")]
        class OnConfirm
        {
            static void Prefix(ref int ___checkCode, GameBase.USettings.UPassword ___m_password)
            {
                DemulShooter_Plugin.MyLogger.LogWarning("GameBase.USettings.Notes.UPasswordNotes.OnConfirm(): ___checkCode=" + ___checkCode);
                ___checkCode = ___m_password.GetCode();
            }
        }

        /// <summary>
        /// For Debug
        /// </summary>
        [HarmonyPatch(typeof(GameBase.USettings.Notes.UPasswordNotes), "CheckSuccess")]
        class CheckSuccess
        {
            static void Postfix()
            {
                DemulShooter_Plugin.MyLogger.LogWarning("GameBase.USettings.Notes.UPasswordNotes.CheckSuccess()");
            }
        }
        [HarmonyPatch(typeof(GameBase.USettings.Notes.UPasswordNotes), "Code")]
        class code
        {
            static void Postfix(ref int __result)
            {
                DemulShooter_Plugin.MyLogger.LogWarning("GameBase.USettings.Notes.UPasswordNotes.Code(): result=" + __result);
            }
        }
        [HarmonyPatch(typeof(GameBase.USettings.Notes.UPasswordNotes), "Correct")]
        class Correct
        {
            static void Prefix()
            {
                DemulShooter_Plugin.MyLogger.LogWarning("GameBase.USettings.Notes.UPasswordNotes.Correct()");
            }
        }
        [HarmonyPatch(typeof(GameBase.USettings.Notes.UPasswordNotes), "Incorrect")]
        class Incorrect
        {
            static void Prefix()
            {
                DemulShooter_Plugin.MyLogger.LogWarning("GameBase.USettings.Notes.UPasswordNotes.Incorrect()");
            }
        }
        [HarmonyPatch(typeof(GameBase.USettings.Notes.UPasswordNotes), "StartNotes")]
        class StartNotes
        {
            static void Prefix()
            {
                DemulShooter_Plugin.MyLogger.LogWarning("GameBase.USettings.Notes.UPasswordNotes.StartNotes():");
            }
        }
    }
}
