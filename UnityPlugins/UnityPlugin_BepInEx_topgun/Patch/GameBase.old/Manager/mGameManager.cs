using GameBase;
using HarmonyLib;

namespace TopGun_BepInEx_DemulShooter_Plugin
{
    internal class mGameManager
    {
        [HarmonyPatch(typeof(GameBase.Manager.GameManager), "ReadConfig")]
        class ReadConfig
        {
            static void Postfix(ref SysDefine.Language ___defaultLang)
            {
                DemulShooter_Plugin.MyLogger.LogMessage("GameBase.Manager.GameManager.ReadConfig() : ___defaultlang=" + ___defaultLang.ToString());
                //___defaultLang = SysDefine.Language.English;
            }
        }
    }
}
