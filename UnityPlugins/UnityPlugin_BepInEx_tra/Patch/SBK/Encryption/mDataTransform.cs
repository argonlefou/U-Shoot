using System.IO;
using HarmonyLib;

namespace BepInEx_UShoot_Plugin.Patch
{
    internal class mDataTransform
    {
        /// <summary>
        /// Desactivating  Data Encryption for save files
        /// Default behavior is using AES encryption but some error happen when trying to read them, and game is not saving data correctly
        /// </summary>
        [HarmonyPatch(typeof(SBK.Encryption.DataTransform), "EncryptStreamToBytes_Aes")]
        class EncryptStreamToBytes_Aes
        {
            static bool Prefix(Stream i_PlainStrm, byte[] i_Key, byte[] i_IV, ref byte[] __result)
            {
                __result = new byte[i_PlainStrm.Length];
                i_PlainStrm.Read(__result, 0, (int)i_PlainStrm.Length);
                return false;
            }
        }
        [HarmonyPatch(typeof(SBK.Encryption.DataTransform), "DecryptStreamFromBytes_Aes")]
        class DecryptStreamFromBytes_Aes
        {
            static bool Prefix(Stream i_CipherStrm, byte[] i_Key, byte[] i_IV, ref byte[] __result)
            {
                __result = new byte[i_CipherStrm.Length];
                i_CipherStrm.Read(__result, 0, (int)i_CipherStrm.Length);
                return false;
            }
        }
    }
}
