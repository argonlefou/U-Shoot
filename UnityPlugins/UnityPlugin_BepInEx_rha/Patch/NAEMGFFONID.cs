using System;
using System.IO;
using HarmonyLib;

namespace BepInEx_UShoot_Plugin.Patch
{
    internal class mNAEMGFFONID
    {
        [HarmonyPatch(typeof(NAEMGFFONID), "NEBJKGCFIGH")]
        class NEBJKGCFIGH
        {
            static bool Prefix(Stream JHKDDOIIIDL, byte[] HHALPJMFHLH, byte[] FPKDJCDHHDJ, ref byte[] __result)
            {
                UShoot_Plugin.MyLogger.LogMessage("NAEMGFFONID.NEBJKGCFIGH()");

                if (JHKDDOIIIDL == null)
                {
                    throw new ArgumentNullException("i_PlainStrm");
                }
                if (HHALPJMFHLH == null || HHALPJMFHLH.Length == 0)
                {
                    throw new ArgumentNullException("i_Key");
                }
                if (FPKDJCDHHDJ == null || FPKDJCDHHDJ.Length == 0)
                {
                    throw new ArgumentNullException("i_IV");
                }
                if (JHKDDOIIIDL.Length <= 0L)
                {
                    __result = new byte[0];
                    return false;
                }
                byte[] array = new byte[JHKDDOIIIDL.Length];
                JHKDDOIIIDL.Read(array, 0, (int)JHKDDOIIIDL.Length);
                __result =  array;

                return false;
            }
        }

        [HarmonyPatch(typeof(NAEMGFFONID), "IPMDECODIIL")]
        class IPMDECODIIL
        {
            static bool Prefix(Stream ENOLCGLNMAP, byte[] HHALPJMFHLH, byte[] FPKDJCDHHDJ, ref byte[] __result, byte[] ___ACLKDGFAFKE)
            {
                UShoot_Plugin.MyLogger.LogMessage("NAEMGFFONID.IPMDECODIIL()");

                if (ENOLCGLNMAP == null)
                {
                    throw new ArgumentNullException("i_CipherStrm");
                }
                if (HHALPJMFHLH == null || HHALPJMFHLH.Length == 0)
                {
                    throw new ArgumentNullException("Key");
                }
                if (___ACLKDGFAFKE == null || ___ACLKDGFAFKE.Length == 0)
                {
                    throw new ArgumentNullException("IV");
                }
                if (ENOLCGLNMAP.Length <= 0L)
                {
                    __result= new byte[0];
                    return false;
                }
                byte[] array = new byte[ENOLCGLNMAP.Length];
                ENOLCGLNMAP.Read(array, 0, (int)ENOLCGLNMAP.Length);
                __result = array;

                return false;
            }
        }
    }
}
