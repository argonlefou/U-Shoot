using HarmonyLib;

namespace BepInEx_UShoot_Plugin.Patch
{
    internal class mCreditsUI
    {
        [HarmonyPatch(typeof(CreditsUI), "Start")]
        class Start
        {
            static void Postfix(CreditsUI __instance, ref int ___UniversalCredit)
            {
                UShoot_Plugin.MyLogger.LogMessage("CreditsUI.Start()");
                ___UniversalCredit = 0;
            }
        }

        [HarmonyPatch(typeof(CreditsUI), "Update")]
        class Update
        {
            static void Postfix(CreditsUI __instance, int ___UniversalCredit)
            {
                UShoot_Plugin.OutputData.Credits = ___UniversalCredit;
            }
        }

        /*[HarmonyPatch(typeof(CreditsUI), "CheckCreditTaken")]
        class CheckCreditTaken
        {
            static bool Prefix(CreditsUI __instance)
            {
                if (!MIBCore.DisableInputs)
                {
                    if (Input.GetKeyDown(KeyCode.Alpha1))
                    {
                        __instance.StartCoroutine(mWaitToUse.WaitToUse(__instance, 1));
                    }
                    if (Input.GetKeyDown(KeyCode.Alpha2))
                    {
                        base.StartCoroutine(this.WaitToUse(2));
                    }
                }

                DemulShooter_Plugin.MyLogger.LogMessage("CreditsUI.Start()");
                foreach (MethodInfo method in typeof(UnityEngine.Input).GetMethods(BindingFlags.Static | BindingFlags.Public))
                {
                    if (method.Name.Equals("GetButtonDown"))
                    {

                    }
                }
                return true;
            }
        }*/

        /*[HarmonyPatch(typeof(CreditsUI), "WaitToUse")]
        class mWaitToUse
        {
            [HarmonyReversePatch]
            [MethodImpl(MethodImplOptions.NoInlining)]
            public static void WaitToUse(object instance, int p)
            {
                //Used to call the private method    
            }
        }*/
    }
}
