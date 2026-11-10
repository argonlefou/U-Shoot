using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;

namespace BepInEx_UShoot_Plugin.Patch
{
    internal class mBlasterWeapon
    {
        /// <summary>
        /// Intercepting Fire event when the gun shoot a bullet to create recoil
        /// </summary>
        [HarmonyPatch]
        class Fire
        {
            static MethodBase TargetMethod()
            {
                MethodInfo[] mis = AccessTools.TypeByName("Assets.Scripts.BlasterWeapon.BlasterWeapon").GetMethods(BindingFlags.Instance | BindingFlags.NonPublic);
                foreach (MethodInfo mi in mis)
                {
                    if (mi.Name.Equals("Fire"))
                        return mi;
                }
                return null;
            }
            static bool Prefix(int ____PlayerID, bool ____isEnabled, Vector3 pos, bool pot)
            {
                //UShoot_Plugin.MyLogger.LogMessage("Assets.Scripts.BlasterWeapon.Fire(): id=" + ____PlayerID + ", pot=" + pot);
                if (____isEnabled)
                    UShoot_Plugin.OutputData.Recoil[____PlayerID] = 1;
                return true;
            }
        }

        /// <summary>
        /// Update() method is looking for Input.getMouseButtonDown() to fire both players 
        /// Noping these call so that only out input method is using the mouse buttons
        /// </summary>
        [HarmonyPatch]
        class Update
        {
            static MethodBase TargetMethod()
            {
                MethodInfo[] mis = AccessTools.TypeByName("Assets.Scripts.BlasterWeapon.BlasterWeapon").GetMethods(BindingFlags.Instance | BindingFlags.NonPublic);
                foreach (MethodInfo mi in mis)
                {
                    if (mi.Name.Equals("Update"))
                        return mi;
                }
                return null;
            }
            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
            {
                var code = new List<CodeInstruction>(instructions);

                for (int i = 0; i < code.Count; i++)
                {
                    if (code[i].opcode == OpCodes.Call && code[i].operand.ToString().Contains("GetMouseButtonDown"))
                    {
                        if (code[i - 1].opcode == OpCodes.Ldfld)
                        {
                            code[i - 2].opcode = OpCodes.Nop;
                            code[i - 1].opcode = OpCodes.Nop;
                            code[i].opcode = OpCodes.Nop;
                            code[i + 1].opcode = OpCodes.Nop;
                            break;
                        }
                    }
                }
                return code;
            }
        }
    }
}
