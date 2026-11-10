using HarmonyLib;
using Uduino;

namespace BepInEx_UShoot_Plugin
{
    class mUduinoManager
    {
        /// <summary>
        /// Trigger the siren and police light
        /// </summary>
        [HarmonyPatch(typeof(UduinoManager), "arduinoWrite")]
        class arduinoWrite
        {
            static bool Prefix(UduinoDevice target, int pin, int value, string typeOfPin, string bundle=null)
            {
                //DemulShooter_Plugin.MyLogger.LogWarning("UduinoManager.arduinoWrite() => pin=" + pin + ", value=" + value);
                /*if (bundle != null)                    
                    DemulShooter_Plugin.MyLogger.LogMessage("UduinoManager.arduinoWrite() => target=" + target.ToString() + ", pin=" + pin.ToString() + ", value=" + value.ToString() + ", typeOfPin=" + typeOfPin.ToString() + ", bundle=" + bundle.ToString());
                else      
                    DemulShooter_Plugin.MyLogger.LogMessage("UduinoManager.arduinoWrite() => target=" + target.ToString() + ", pin=" + pin.ToString() + ", value=" + value.ToString() + ", typeOfPin=" + typeOfPin.ToString() + ", bundle=null");
                */
                //DemulShooter_Plugin.MyLogger.LogMessage("UduinoManager.arduinoWrite() => pin=" + pin.ToString() + ", value=" + value.ToString() + ", typeOfPin=" + typeOfPin.ToString());

                if (value != 0)
                {
                    if (pin == (int)UShoot_Plugin.ArduinoPin.PlayerGun_Solenoid)
                        UShoot_Plugin.OutputData.GunRecoil = 1;

                    else if (pin == (int)UShoot_Plugin.ArduinoPin.DirectHit_Light)
                        UShoot_Plugin.OutputData.DirectHit = 1;

                    else if (pin == (int)UShoot_Plugin.ArduinoPin.Police_LightBar)
                        UShoot_Plugin.OutputData.Police_LightBar = 1;

                    else if (pin == (int)UShoot_Plugin.ArduinoPin.ArduinoReady_Light)
                        UShoot_Plugin.OutputData.GreenTestLight = 1;

                    else if (pin == (int)UShoot_Plugin.ArduinoPin.RedLightContinue)
                        UShoot_Plugin.OutputData.RedLight = 1;

                    else if (pin == (int)UShoot_Plugin.ArduinoPin.WhiteStrobe_Flasher)
                        UShoot_Plugin.OutputData.WhiteStrobe = 1;

                    else if (pin == (int)UShoot_Plugin.ArduinoPin.GameGun_Light)
                        UShoot_Plugin.OutputData.GunLight = 1;

                    else if (pin == (int)UShoot_Plugin.ArduinoPin.DirectHit_Light2)
                        UShoot_Plugin.MyLogger.LogWarning("UduinoManager.arduinoWrite() => DirectHit_Light2 ON");

                    else if (pin == (int)UShoot_Plugin.ArduinoPin.EnemyGun_Solenoid)
                        UShoot_Plugin.MyLogger.LogWarning("UduinoManager.arduinoWrite() => Enemy Gun Solenoid ON");

                    else if (pin == (int)UShoot_Plugin.ArduinoPin.EnemyGun2_Solenoid)
                        UShoot_Plugin.MyLogger.LogWarning("UduinoManager.arduinoWrite() => Enemy Gun Solenoid 2 ON");
                }
                else
                {
                    if (pin == (int)UShoot_Plugin.ArduinoPin.PlayerGun_Solenoid)
                        UShoot_Plugin.OutputData.GunRecoil = 0;

                    if (pin == (int)UShoot_Plugin.ArduinoPin.DirectHit_Light)
                        UShoot_Plugin.OutputData.DirectHit = 0;

                    else if (pin == (int)UShoot_Plugin.ArduinoPin.Police_LightBar)
                        UShoot_Plugin.OutputData.Police_LightBar = 0;

                    else if (pin == (int)UShoot_Plugin.ArduinoPin.ArduinoReady_Light)
                        UShoot_Plugin.OutputData.GreenTestLight = 0;

                    else if (pin == (int)UShoot_Plugin.ArduinoPin.RedLightContinue)
                        UShoot_Plugin.OutputData.RedLight = 0;

                    else if (pin == (int)UShoot_Plugin.ArduinoPin.WhiteStrobe_Flasher)
                        UShoot_Plugin.OutputData.WhiteStrobe = 0;

                    else if (pin == (int)UShoot_Plugin.ArduinoPin.GameGun_Light)
                        UShoot_Plugin.OutputData.GunLight = 0;

                    else if (pin == (int)UShoot_Plugin.ArduinoPin.DirectHit_Light2)
                        UShoot_Plugin.MyLogger.LogWarning("UduinoManager.arduinoWrite() => DirectHit_Light2 OFF");

                    else if (pin == (int)UShoot_Plugin.ArduinoPin.EnemyGun_Solenoid)
                        UShoot_Plugin.MyLogger.LogWarning("UduinoManager.arduinoWrite() => Enemy Gun Solenoid OFF");

                    else if (pin == (int)UShoot_Plugin.ArduinoPin.EnemyGun2_Solenoid)
                        UShoot_Plugin.MyLogger.LogWarning("UduinoManager.arduinoWrite() => Enemy Gun Solenoid 2 OFF");
                }

                return true;
            }
        }
    }
}
