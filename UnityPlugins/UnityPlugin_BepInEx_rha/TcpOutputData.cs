using UnityPlugin_BepInEx.Core;

namespace BepInEx_UShoot_Plugin
{
    public class TcpOutputData : TcpData
    {
        public byte[] StartLamp = null;
        public byte[] Recoil = null;
        public byte[] Damaged = null;
        public float[] Life = null;
        public int[] Ammo = null;
        public int[] Credits = null;

        public TcpOutputData(int PlayerNumer) : base(PlayerNumer) { }
    }
}
