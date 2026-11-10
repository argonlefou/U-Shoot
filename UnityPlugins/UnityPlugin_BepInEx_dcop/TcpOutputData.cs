using UnityPlugin_BepInEx.Core;

namespace BepInEx_UShoot_Plugin
{
    public class TcpOutputData : TcpData
    {
        public byte GunRecoil = 0;
        public byte DirectHit = 0;
        public byte Police_LightBar = 0;
        public byte GreenTestLight = 0;
        public byte RedLight = 0;
        public byte WhiteStrobe = 0;
        public byte GunLight = 0;
        public byte P1_Life = 0;
        public byte P1_Ammo = 0;

        public TcpOutputData(int PlayerNumer) : base(PlayerNumer) { }
    }
}
