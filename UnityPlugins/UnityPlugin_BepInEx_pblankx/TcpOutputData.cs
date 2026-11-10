using UnityPlugin_BepInEx.Core;

namespace BepInEx_UShoot_Plugin
{
    public class TcpOutputData : TcpData
    {
        public byte[] Recoil = null;
        public byte[] StartLED = null;
        public byte[] PlayerLED = null;
        public byte[] Life = null;
        public int[] Ammo = null;
        public byte Credits = 0;

        public TcpOutputData(int PlayerNumer) : base(PlayerNumer) { }
    }
}
