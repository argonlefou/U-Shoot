using UnityPlugin_BepInEx.Core;

namespace BepInEx_UShoot_Plugin
{
    public class TcpOutputData : TcpData
    {
        public byte[] IsPlaying = null;
        public byte[] Recoil = null;
        public byte[] Damaged = null;
        public int[] Life = null;
        public int[] GunType = null;
        public int Credits = 0;

        public TcpOutputData(int PlayerNumer) : base(PlayerNumer) { }
    }
}
