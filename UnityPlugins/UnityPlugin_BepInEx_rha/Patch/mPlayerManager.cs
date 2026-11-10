using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;

namespace BepInEx_UShoot_Plugin
{
    class mPlayerManager
    {
        /// <summary>
        /// Getting Damaged flag
        /// </summary>
        [HarmonyPatch(typeof(PlayerManager), "GetDamage")]
        class GetDamage
        {
            static bool Prefix(BaseDamage.Info i_Info, ID i_ID)
            {
                UShoot_Plugin.MyLogger.LogMessage("PlayerManager.GetDamage() : i_ID=" + i_ID.ToString());
                if ((int)i_ID < 4)
                {
                    UShoot_Plugin.OutputData.Damaged[(int)i_ID] = 1;
                }

                return true;
            }
        }

        /// <summary>
        /// This one uses 2 keys [G] and [K] for god mode and bots
        /// </summary>
        [HarmonyPatch(typeof(PlayerManager), "Update")]
        class Update
        {
            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                var codes = new List<CodeInstruction>(instructions);
                for (int i = 0; i < codes.Count; i++)
                {
                    if (codes[i].opcode == OpCodes.Call && codes[i].operand.ToString().Contains("GetKeyDown"))
                    {
                        if (codes[i - 1].opcode == OpCodes.Ldc_I4_S && codes[i - 1].operand.ToString().Equals("103"))
                            codes[i - 1].operand = 0;
                        if (codes[i - 1].opcode == OpCodes.Ldc_I4_S && codes[i - 1].operand.ToString().Equals("107"))
                            codes[i - 1].operand = 0;
                    }
                }

                return codes;
            }
        }
    }
}
