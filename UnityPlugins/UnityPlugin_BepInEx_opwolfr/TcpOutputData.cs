using UnityPlugin_BepInEx.Core;

namespace BepInEx_UShoot_Plugin
{
    public class TcpOutputData : TcpData
    {
        public byte[] IsPlaying = null;
        public byte[] Recoil = null;
        public byte[] Damaged = null;
        public byte[] Life = null;
        public int[] Ammo = null;

        public TcpOutputData(int PlayerNumer) : base(PlayerNumer) { }
    }
}
