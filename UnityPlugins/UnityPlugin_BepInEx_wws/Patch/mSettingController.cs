using System;
using HarmonyLib;
using MVSDK;
using UnityEngine;
using UnityEngine.UI;

namespace BepInEx_UShoot_Plugin.Patch
{
    internal class mSettingController
    {
        /// <summary>
        /// Remove Keyboard
        /// </summary>
        [HarmonyPatch(typeof(SettingController), "Update")]
        class Update
        {
            static bool Prefix(Text ___dateTxt, bool ___ShowRealPointCount, ref float ___lastCountTime, ref int ___realPointCount)
            {
                ___dateTxt.text = DateTime.Now.AddSeconds(GameData.DateTimeOffset).ToString("yyyy-MM-dd HH:mm:ss");
                
                if (___ShowRealPointCount)
                {
                    if (Time.time - ___lastCountTime <= 2f)
                    {
                        if (BaseGun.Instance != null && BaseGun.Instance.GetGunRealPixPos(0) == BaseGun.InvalidPoint)
                        {
                            ___realPointCount++;
                        }
                    }
                    else
                    {
                        if (MVSettings.Instance != null)
                        {
                            MVSettings.Instance.textOut.text = ___realPointCount.ToString();
                        }
                        if (LightColorController.HadInstance())
                        {
                            LightColorController.Instance().realCountTxt.text = ___realPointCount.ToString();
                        }
                        ___lastCountTime = Time.time;
                        ___realPointCount = 0;
                    }
                }
                return false;
            }
        }
    }
}
