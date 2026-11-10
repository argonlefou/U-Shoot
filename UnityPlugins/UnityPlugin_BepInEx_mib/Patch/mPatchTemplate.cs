using HarmonyLib;
using System.Reflection;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace BepInEx_DemulShooter_Plugin
{/*
    class mPatchTemplate
    {
        /// <summary>
        /// Simple Prefix / postfix patch
        /// </summary>
        [HarmonyPatch(typeof(BulletCounter), "SetBulletCount")]
        class SetBulletCount
        {
            static bool Prefix(PlayerPanel ___playerPanel, int count)
            {
                DemulShooter_Plugin.MyLogger.LogMessage("SetBulletCount: " + ___playerPanel.playerIndex + ", count=" + count);
                return true;
            }
            static void Postfix(PlayerPanel ___playerPanel, int count)
            {
                DemulShooter_Plugin.MyLogger.LogMessage("SetBulletCount: " + ___playerPanel.playerIndex + ", count=" + count);
            }
        }

        /// <summary>
        /// When easy find Class/Method is not possible, using reflexion to get them (unexposed internal class, etc...)
        /// </summary>
        [HarmonyPatch]
        class ExecuteCMD
        {
            static MethodBase TargetMethod()
            {
                MethodInfo[] mis = AccessTools.TypeByName("InputManager").GetMethods(BindingFlags.Instance | BindingFlags.NonPublic);
                foreach (MethodInfo mi in mis)
                {
                    if (mi.Name.Equals("SetInput"))
                        return mi;
                }
                return null;
            }
            static bool Prefix(InputManager __instance)
            {
                DemulShooter_Plugin("Found it !");
                return false;
            }
        }


        /// <summary>
        /// Simple Transpiler
        /// </summary>
        [HarmonyPatch(typeof(TimeAttackResult), "GetRanking")]
        class GetRanking
        {
            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
            {
                var code = new List<CodeInstruction>(instructions);
                for (int i = 0; i < code.Count; i++)
                {
                    if (code[i].opcode == OpCodes.Ldstr && (string)code[i].operand == "D:\\TimeAttackResultHolder.bin")
                    {
                        code[i].operand = "NVRAM\\TimeAttackResultHolder.bin";
                    }
                    if (code[i].opcode == OpCodes.Ldstr && (string)code[i].operand == "D:\\")
                    {
                        code[i].operand = "NVRAM\\";
                    }
                }
                return code;
            }
        }
    }
*/
}
