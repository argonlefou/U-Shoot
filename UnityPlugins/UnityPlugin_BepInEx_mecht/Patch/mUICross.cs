using HarmonyLib;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace BepInEx_UShoot_Plugin.Patch
{
    class mUICross
    {
        /// <summary>
        /// Original code has hardcoded Axis * 1920 (or 1080)
        /// This is creating an offset if we change game resolution, so changing it so that it can be multiplied by the real size        
        /// </summary>
        [HarmonyPatch(typeof(UICross), "OnUpdate")]
        class OnUpdate
        {
            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
            {
                var code = new List<CodeInstruction>(instructions);

                for (int i = 0; i < code.Count; i++)
                {
                    if (code[i].opcode == OpCodes.Ldc_R4 && (float)code[i].operand == 1920.0f)
                    {
                        code[i].operand = (float)UnityEngine.Screen.width;
                    }
                    if (code[i].opcode == OpCodes.Ldc_R4 && (float)code[i].operand == 960.0f)
                    {
                        code[i].operand = (float)(UnityEngine.Screen.width / 2);
                    }
                    if (code[i].opcode == OpCodes.Ldc_R4 && (float)code[i].operand == 1080.0f)
                    {
                        code[i].operand = (float)UnityEngine.Screen.height;
                    }
                    if (code[i].opcode == OpCodes.Ldc_R4 && (float)code[i].operand == 540.0f)
                    {
                        code[i].operand = (float)(UnityEngine.Screen.height / 2);
                    }
                }
                return code;
            }
            
            /// <summary>
            /// Remove Crosshair
            /// </summary>
            static void Postfix(UnityEngine.Transform ___tf_biao)
            {
                if (!UShoot_Plugin.CrossHairVisibility)
                    ___tf_biao.localScale = new UnityEngine.Vector3();
            }
        }
    }
}
