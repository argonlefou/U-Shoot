using HarmonyLib;
using Virtuallyz.VRShooter.Characters;
using Virtuallyz.VRShooter.Weapons;

namespace BepInEx_UShoot_Plugin
{
    class mWeapon
    {
        /// <summary>
        /// Intercept the shoot event to create Output to DemulShooter
        /// </summary>
        [HarmonyPatch(typeof(Weapon), "Shoot")]
        class Shoot
        {
            static bool Prefix(Character ___owner, Weapon __instance)
            {
                //OpWolf_Plugin.MyLogger.LogMessage("Virtuallyz.VRShooter.Weapons.Weapon.Shoot() => " + ___owner.ToString());
                //Owner of the Weapon is a character, and the Character class is overriding the ToString() method :
                //If the owner if a Player, it will return "Player"
                //Else, a NPC name
                if (___owner.ToString().Equals("Player"))
                    UShoot_Plugin.OutputData.Recoil[__instance.ControllerId] = 1;

                return true;
            }
        }
    }
}
