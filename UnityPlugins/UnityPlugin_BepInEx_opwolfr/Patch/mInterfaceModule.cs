using HarmonyLib;
using Virtuallyz.VRShooter.Characters;
using Virtuallyz.VRShooter.Modules;
using Virtuallyz.VRShooter.Weapons;


namespace BepInEx_UShoot_Plugin
{
    class mInterfaceModule
    {
        /// <summary>
        /// Intercapting call to LifeBar update to get Life value of player
        /// </summary>
        [HarmonyPatch(typeof(InterfaceLifeModule), "UpdateLifebars")]
        class UpdateLifebars
        {
            static bool Prefix(Character character)
            {
                //OpWolf_Plugin.MyLogger.LogMessage("Virtuallyz.Modules.InterfaceLifeModule.UpdateLifebars() => " + character.ToString());
                //OpWolf_Plugin.MyLogger.LogMessage("Virtuallyz.Modules.InterfaceLifeModule.UpdateLifebars() => currentHP: " + character.CurrentHP + ", isFine: " + character.IsFine + ", is BadlyHurt: " + character.IsBadlyHurt + ", isDead: " + character.IsDead);

                if (character.ToString().Equals("Player"))
                {
                    UShoot_Plugin.OutputData.Life[0] = (byte)character.CurrentHP;
                    UShoot_Plugin.OutputData.Life[1] = (byte)character.CurrentHP;
                }
                return true;
            }
        }

        /// <summary>
        /// Intercapting call to Weapon UI update to get Ammo value of player weapon
        /// </summary>
        [HarmonyPatch(typeof(InterfaceWeaponModule), "UpdateWeaponUIInformation")]
        class UpdateWeaponUIInformation
        {
            static bool Prefix(Weapon weapon)
            {
                if (weapon != null && weapon.Owner.ToString().Equals("Player"))
                {
                    ushort Ammo = 0;
                    if (weapon is ProjectileWeapon )
                    {
                        //OpWolf_Plugin.MyLogger.LogMessage("Virtuallyz.Modules.InterfaceWeaponModule.UpdateWeaponUIInformation(): weapon.Name: " + weapon.name + ", weapon.ControllerId: " + weapon.ControllerId);                
                        //OpWolf_Plugin.MyLogger.LogMessage("Virtuallyz.Modules.InterfaceLifeModule.UpdateWeaponUIInformation() => Projectile");
                        ProjectileWeapon w = weapon as ProjectileWeapon;
                        Ammo = (ushort)w.CurrentChargerAmmosInt;
                        //OpWolf_Plugin.MyLogger.LogMessage("Virtuallyz.Modules.InterfaceLifeModule.UpdateWeaponUIInformation() => CurrentChargerAmmosInt: " + w.CurrentChargerAmmosInt);                        
                    }

                    if (weapon.ControllerId == 0)
                        UShoot_Plugin.OutputData.Ammo[0] = Ammo;
                    else if (weapon.ControllerId == 1)
                        UShoot_Plugin.OutputData.Ammo[1] = Ammo;
                }
                return true;
            }
        }
    }
}
