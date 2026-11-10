using HarmonyLib;
using System.Collections.Generic;
using System.Reflection.Emit;
using UnityEngine;

namespace BepInEx_UShoot_Plugin.Patch
{
    class mbp_player
    {
        /// <summary>
        /// Original code has hardcoded Axis * 1920 (or 1080)
        /// This is creating an offset if we change game resolution, so changing it so that it can be multiplied by the real size
        /// 
        /// Can also be used to trigger RECOIL output as it's called for each shot
        /// </summary>
        [HarmonyPatch(typeof(bp_player), "getfirepos")]
        class getfirepos
        {
            static bool Prefix(bp_player __instance, ref Vector3 __result)
            {
                Vector3 vector = new Vector3();
                if (dllInstance.instance != null && dllInstance.getdlldata(__instance.playerid).biosucees)
                {
                    vector = dllInstance.instance.get_gun_pos_01(__instance.playerid);
                    vector.x *= (float)Screen.width;
                    vector.y *= (float)Screen.height;
                }
                __result = vector;

                UShoot_Plugin.OutputData.Recoil[__instance.playerid] = 1;
                //DemulShooter_Plugin.MyLogger.LogMessage("bp_player.getfirepos: id=" + __instance.playerid + ", resumt=" + __result.ToString());
                return false;
            }
        }

        /// <summary>
        /// Original function is calling for mouse funtion even if Native DLL is OK
        /// Which is creating issue with multiples lightguns....
        /// 
        /// Not the best solution but I could just replace the call of Input.MouseButton(0) / Input.MouseButton(1) to Input.MouseButton(3)
        /// It's not a proper removal but should be enough to free Left/Middle/Right clicks from most lightguns
        /// 
        /// Also, when using the IO boad the game applies 0.5sec delay between the trigger press and the first shot
        /// Maybe time for water to reach the screen ? Can be deactivated here also
        /// </summary>
        [HarmonyPatch(typeof(bp_player), "Update")]
        class Update
        {
            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
            {
                var code = new List<CodeInstruction>(instructions);

                for (int i = 0; i < code.Count - 1; i++)
                {
                    if (code[i].opcode == OpCodes.Call && code[i].operand.ToString() == "Boolean GetMouseButton(Int32)")
                    {
                        code[i - 1].opcode = OpCodes.Ldc_I4_3;
                    }
                }

                //last_noinput_time > 0.5f becomes last_noinput_time > 0.0f to remove Trigger Press delay
                if (!UShoot_Plugin.EnableTriggerWaterDelay)
                {
                    for (int i = 0; i < code.Count - 2; i++)
                    {
                        if (code[i].opcode == OpCodes.Ldfld && code[i].operand.ToString().Contains("last_noinput_time"))// && code[i].operand.ToString() == "Boolean GetMouseButton(Int32)")
                        {
                            if (code[i + 2].opcode == OpCodes.Ldc_R4 && (float)code[i + 2].operand == 0.5f)
                                code[i + 2].operand = 0.0f;
                        }
                    }
                }

                return code;
            }
        }
    }
}
