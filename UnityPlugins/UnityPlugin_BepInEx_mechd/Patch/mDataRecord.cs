using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;

namespace BepInEx_UShoot_Plugin.Patch
{
    class mDataRecord
    {
        /// <summary>
        /// Save data next in exe folder instead of separate hard disk
        /// </summary>
        [HarmonyPatch(typeof(DataRecord), "ReadAcountData")]
        class ReadAcountData
        {
            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
            {
                var code = new List<CodeInstruction>(instructions);
                if (UShoot_Plugin.LocalSave)
                {
                    for (int i = 0; i < code.Count; i++)
                    {
                        if (code[i].opcode == OpCodes.Ldstr && (string)code[i].operand == "D:/AcountData.db")
                        {
                            code[i].operand = BepInEx.Paths.GameRootPath + "/AcountData.db";
                        }
                    }
                }
                return code;
            }
        }

        /// <summary>
        /// Save data next in exe folder instead of separate hard disk
        /// </summary>
        [HarmonyPatch(typeof(DataRecord), "ReadRankData")]
        class ReadRankData
        {
            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
            {
                var code = new List<CodeInstruction>(instructions);
                if (UShoot_Plugin.LocalSave)
                {
                    for (int i = 0; i < code.Count; i++)
                    {
                        if (code[i].opcode == OpCodes.Ldstr && (string)code[i].operand == "D:/RankData.db")
                        {
                            code[i].operand = BepInEx.Paths.GameRootPath + "/RankData.db";
                        }
                    }
                }
                return code;
            }
        }

        /// <summary>
        /// Save data next in exe folder instead of separate hard disk
        /// </summary>
        [HarmonyPatch(typeof(DataRecord), "ReadSettingData")]
        class ReadSettingData
        {
            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
            {
                var code = new List<CodeInstruction>(instructions);
                if (UShoot_Plugin.LocalSave)
                {
                    for (int i = 0; i < code.Count; i++)
                    {
                        if (code[i].opcode == OpCodes.Ldstr && (string)code[i].operand == "D:/SettingData.db")
                        {
                            code[i].operand = BepInEx.Paths.GameRootPath + "/SettingData.db";
                        }
                    }
                }
                return code;
            }
        }

        /// <summary>
        /// If no settings are present, initializing with good display value
        /// </summary>
        [HarmonyPatch(typeof(DataRecord), "InitSettingData")]
        class InitSettingData
        {
            static void Postfix(DataRecord __instance)
            {
                __instance.Width = 1920;
                __instance.Height = 1080;
                UShoot_Plugin.MyLogger.LogMessage("DataRecord.InitSettingData()");
            }
        }

        /// <summary>
        /// Sale thing if Loaded from previous wrong data
        /// </summary>
        [HarmonyPatch(typeof(DataRecord), "LoadSettingData")]
        class LoadSettingData
        {
            static void Postfix(DataRecord __instance)
            {
                __instance.Width = 1920;
                __instance.Height = 1080;
                UShoot_Plugin.MyLogger.LogMessage("DataRecord.LoadSettingData(): Width=" + __instance.Width + ", Height=" + __instance.Height);
            }
        }        
    }
}
