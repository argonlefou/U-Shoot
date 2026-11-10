using HarmonyLib;
using Uduino;

namespace BepInEx_UShoot_Plugin
{
    class mUduinoConnection_DesktopSerial
    {
        /// <summary>
        /// disable Arduino lookups and hangs for connections failed
        /// </summary>
        [HarmonyPatch(typeof(UduinoConnection_DesktopSerial), "Discover")]
        class TurnOnPin
        {
            static bool Prefix(ref string[] portNames)
            {
                UShoot_Plugin.MyLogger.LogMessage("UduinoConnection_DesktopSerial.Discover()");
                portNames = new string[0];
                return true;
            }
        }
    }
}
