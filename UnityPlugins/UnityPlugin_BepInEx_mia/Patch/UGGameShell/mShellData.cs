using System.Collections.Generic;
using System.IO;
using System.Reflection.Emit;
using HarmonyLib;

namespace BepInEx_UShoot_Plugin.Patch
{
    class mShellData
    {
        /// <summary>
        /// Using local file from folder
        /// Game is using relative path to load a file !!
        /// </summary>
        [HarmonyPatch(typeof(UGGameShell.ShellData), "LoadFile")]
        class LoadFile
        {
            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
            {
                var code = new List<CodeInstruction>(instructions);
                for (int i = 0; i < code.Count; i++)
                {
                    if (code[i].opcode == OpCodes.Ldstr && (string)code[i].operand == "/../ShellData/ShellData.ini")
                    {
                        code[i].operand = "/NVRAM/ShellData.ini";
                    }
                }
                return code;
            }
        }
    }
}
