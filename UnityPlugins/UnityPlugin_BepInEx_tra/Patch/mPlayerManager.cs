using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HarmonyLib;

namespace BepInEx_UShoot_Plugin.Patch
{
    internal class mPlayerManager
    {
        //Removes Cheats KEYS
        [HarmonyPatch(typeof(PlayerManager), "Update")]
        class Update
        {
            static bool Prefix(Player[] ___m_PlayerList)
            {
                for (int i = 0; i < ___m_PlayerList.Length; i++)
                {
                    if (___m_PlayerList[i].IsPlaying())
                    {
                        ___m_PlayerList[i].Update();
                    }
                }
                return false;
            }
        }
    }
}
