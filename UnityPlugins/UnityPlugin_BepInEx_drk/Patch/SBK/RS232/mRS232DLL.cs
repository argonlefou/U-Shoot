using HarmonyLib;
using Il2CppInterop.Runtime.InteropTypes.Arrays;

namespace BepInEx_UShoot_Plugin.Patch.SBK_RS232
{
    class mRS232DLL
    {
        [HarmonyPatch(typeof(SBK.RS232.RS232DLL), "GetActivePortsIndex")]
        class GetActivePortsIndex
        {
            static bool Prefix(/*ref int[] __result*/)
            {
                //Drakon_Plugin.MyLogger.LogMessage("GetActivePortsIndex Prefix");
                return true;
            }

            static void Postfix(ref Il2CppStructArray<int> __result)
            {
                /*__result = new Il2CppStructArray<int>(0);
                Drakon_Plugin.MyLogger.LogMessage("GetActivePortsIndex Postfix, __result.length = " + __result.Length.ToString());
                if (__result.Length > 0)
                {
                    for (int i = 0; i < __result.Length; i++)
                    {
                        Drakon_Plugin.MyLogger.LogMessage("-> COM Index " + __result[i].ToString() + " is active : " );
                    }
                }*/
            }
        }

        [HarmonyPatch(typeof(SBK.RS232.RS232DLL), "GetActivePortsNames")]
        class GetActivePortsNames
        {
            static bool Prefix()
            {
                //Drakon_Plugin.MyLogger.LogMessage("GetActivePortsNames Prefix");
                return true;
            }

            static void Postfix (ref Il2CppStringArray __result)
            {
                __result = new Il2CppStringArray(0);
                /*Drakon_Plugin.MyLogger.LogMessage("GetActivePortsName Postfix, __result.length = " + __result.Length.ToString());
                if (__result.Length > 0)
                {
                    for (int i = 0; i < __result.Length; i++)
                    {
                        Drakon_Plugin.MyLogger.LogMessage("-> PortName " + __result[i].ToString() + " is active : ");
                    }
                }*/
            }
        }

        /*[HarmonyPatch(typeof(SBK.RS232.RS232DLL), "GetPortIndexByName")]
        class GetPortIndexByName
        {
            static bool Prefix(string Name)
            {
                Drakon_Plugin.MyLogger.LogMessage("GetPortIndexByName prefix: Name=" + Name);
                return true;
            }

            static void Postfix(string Name, ref int __result)
            {
                Drakon_Plugin.MyLogger.LogMessage("GetPortIndexByName Postfix, index= " + __result.ToString());
                __result = 1;
            }
        }*/


        /*[HarmonyPatch(typeof(SBK.RS232.RS232DLL), "Open", new Type[] { typeof(int), typeof(int) })]
        class Open1
        {
            static bool Prefix(int comport_number, int baudrate = 115200)
            {
                Plugin.myLogger.LogMessage("mRS232DLL.Open1() : comport_number=" + comport_number.ToString() + ", baudrate=" + baudrate.ToString());
                return true;
            }
        }*/

        /*[HarmonyPatch(typeof(SBK.RS232.RS232DLL), "Open", new Type[] { typeof(int), typeof(int), typeof(byte[]), typeof(int) })]
        class Open2
        {
            static bool Prefix(int comport_number, int baudrate, byte[] mode, int flowctrl)
            {
                Plugin.myLogger.LogMessage("mRS232DLL.Open2() : comport_number=" + comport_number.ToString() + ", baudrate=" + baudrate.ToString() + ", mode.length=" + mode.Length.ToString() + ", flowctrl=" + flowctrl.ToString());
                return true;
            }
        }*/

    }
}