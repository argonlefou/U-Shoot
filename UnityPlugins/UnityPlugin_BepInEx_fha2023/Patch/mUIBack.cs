using System.Collections.Generic;
using System.IO;
using System.Reflection.Emit;
using HarmonyLib;

namespace BepInEx_UShoot_Plugin.Patch
{
    public class mUIBack
    {
        /// <summary>
        /// Change Settings file from hardcoded d:/ to local dir
        /// </summary>
        [HarmonyPatch(typeof(UIback), MethodType.Constructor)]
        class CCtor
        {
            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
            {
                var code = new List<CodeInstruction>(instructions);
                for (int i = 0; i < code.Count; i++)
                {
                    if (code[i].opcode == OpCodes.Ldstr && (string)code[i].operand == "d:/xiaofang.ini")
                    {
                        code[i].operand = Path.Combine(BepInEx.Paths.GameRootPath, "xiaofang.ini");
                    }
                }
                return code;
            }
        }

        /// <summary>
        /// Change language setting, because it is forced in the hacked files
        /// </summary>
        [HarmonyPatch(typeof(UIback), "readvalue")]
        class readvalue
        {
            static void Postfix(UIback __instance)
            {
                UShoot_Plugin.MyLogger.LogMessage("UIBack.readvalue()");
                switch (UShoot_Plugin.GameLanguage) 
                {
                    case "CH": UIback.nlang = 0; break;
                    case "EN": 
                    default: UIback.nlang = 1; break;
                }

                //Cabinet Type:
                //0: Ball and gun outputs (??)
                //1: Simple water spray
                //2: Multi water spray, limited to 2 players, change weapon by turning controller, fewer outputs
                //3: Multi water spray, limited to 2 Players, change weapon by press start, full outputs
                switch (UShoot_Plugin.CabinetType)
                {
                    case "A": UIback.jitai = 1; break;
                    case "B":
                    default: UIback.jitai = 2; break;
                }
                UIback.autoshoot = 0;
                UIback.playernum = UShoot_Plugin.PlayerCount;

                if (UIback.jitai != 1)
                    UIback.playernum = 2;

                UIback.check_mode();
            }
        }
    }
}
