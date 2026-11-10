using System.Collections.Generic;
using System.IO;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using System.Text;
using HarmonyLib;
using MIB;

namespace BepInEx_UShoot_Plugin.Patch
{
    internal class mMIBGameSettings
    {
        [DllImport("kernel32", CharSet = CharSet.Unicode)]
        private static extern int GetPrivateProfileString(string section, string key, string def, StringBuilder retVal, int size, string filePath);


        /// <summary>
        /// Replace the path from hardcoded "C:/Sega/ShellData" to game local folder
        /// </summary>
        [HarmonyPatch(typeof(MIBGameSettings), "LoadFile")]
        class LoadFile
        {
            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
            {
                var code = new List<CodeInstruction>(instructions);
                for (int i = 0; i < code.Count; i++)
                {
                    if (code[i].opcode == OpCodes.Ldstr && (string)code[i].operand == "C:/Sega/ShellData")
                    {
                        code[i].operand = Path.Combine(BepInEx.Paths.GameRootPath, "ShellData");
                    }
                }
                return code;
            }
        }

        /// <summary>
        /// Replacing the original DllImport function by this declaration including Unicode char for path
        /// </summary>
        [HarmonyPatch(typeof(MIBGameSettings), "GetPrivateProfileString")]
        class mGetPrivateProfileString
        {
            static bool Prefix(string lpAppName, string lpKeyName, string lpDefault, StringBuilder lpReturnedString, int nSize, string lpFileName, ref int __result)
            {
                UShoot_Plugin.MyLogger.LogMessage("MIBGameSettings.GetPrivateProfileString():");
                UShoot_Plugin.MyLogger.LogMessage("lpKeyName=" + lpKeyName + ", lpDefault=" + lpDefault + ", lpFileName=" + lpFileName);

                __result = GetPrivateProfileString(lpAppName, lpKeyName, lpDefault, lpReturnedString, nSize, lpFileName);
                return false;
            }
            static void Postfix(string lpAppName, string lpKeyName, string lpDefault, StringBuilder lpReturnedString, int nSize, string lpFileName, int __result)
            {
                UShoot_Plugin.MyLogger.LogMessage("MIBGameSettings.GetPrivateProfileString() POSTFIX:");
                UShoot_Plugin.MyLogger.LogMessage("result=" + __result + ", lpReturnedString=" + lpReturnedString.ToString());
            }
        }
    }
}
