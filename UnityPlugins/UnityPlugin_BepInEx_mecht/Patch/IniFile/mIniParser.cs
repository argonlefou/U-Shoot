using HarmonyLib;
using System.Reflection;


namespace BepInEx_UShoot_Plugin.Patch
{
    class mIniParser
    {
        /// <summary>
        /// Config file is HArdcoded on a static class, we can't change it with BepInEx directy in the owner class
        /// Changing it when the IniFile constructor gets the parameter filename, so that the save file can be in the local folder
        /// </summary>
        [HarmonyPatch]
        class CCtor
        {
            static MethodBase TargetMethod()
            {
                foreach (ConstructorInfo ci in AccessTools.TypeByName("IniFile.IniParser").GetConstructors())
                {
                    if (ci.GetParameters().Length == 1)
                    {
                        return ci;
                    }
                }
                return null;

            }
            static bool Prefix(ref string t_filename)
            {
                UShoot_Plugin.MyLogger.LogWarning("IniFile.IniParser.Cctor(): t_filename= " + t_filename);
                if (t_filename.Equals("d:/jijiasheqiu.ini"))
                {
                    t_filename = BepInEx.Paths.GameRootPath + @"\jijiasheqiu.ini";
                }
                return true;
            }
        }

        /// <summary>
        /// When the Consturctor can't be modified (why ??)
        /// Also changing calls to read/write methods to change the file afterwards
        /// </summary>
        [HarmonyPatch]
        class readIntData
        {
            static MethodBase TargetMethod()
            {
                foreach (MethodInfo mi in AccessTools.TypeByName("IniFile.IniParser").GetMethods())
                {
                    if (mi.Name.Equals("readIntData"))
                    {
                        return mi;
                    }
                }
                return null;

            }
            static bool Prefix(ref string ____file_path, string t_section, string t_key, int t_default = 0)
            {
                ____file_path = BepInEx.Paths.GameRootPath + @"\jijiasheqiu.ini";
                return true;
            }
        }

        [HarmonyPatch]
        class readStringData
        {
            static MethodBase TargetMethod()
            {
                foreach (MethodInfo mi in AccessTools.TypeByName("IniFile.IniParser").GetMethods())
                {
                    if (mi.Name.Equals("readStringData"))
                    {
                        return mi;
                    }
                }
                return null;

            }
            static bool Prefix(ref string ____file_path, string t_section, string t_key, string t_default = "")
            {
                ____file_path = BepInEx.Paths.GameRootPath + @"\jijiasheqiu.ini";
                return true;
            }
        }

        [HarmonyPatch]
        class writeData
        {
            static MethodBase TargetMethod()
            {
                foreach (MethodInfo mi in AccessTools.TypeByName("IniFile.IniParser").GetMethods())
                {
                    if (mi.Name.Equals("writeData"))
                    {
                        return mi;
                    }
                }
                return null;
            }
            static bool Prefix(ref string ____file_path, string t_section, string t_key, string t_data)
            {
                ____file_path = BepInEx.Paths.GameRootPath + @"\jijiasheqiu.ini";
                return true;
            }
        }
    }
}
