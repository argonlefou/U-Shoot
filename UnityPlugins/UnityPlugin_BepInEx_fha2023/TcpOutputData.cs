using UnityPlugin_BepInEx.Core;

namespace BepInEx_UShoot_Plugin
{
    public class TcpOutputData : TcpData
    {
        public byte[] IsPlaying = null;
        public byte[] ChairMotor = null;
        public byte[] GunMotor = null;
        public byte[] SmallGun = null;
        public byte[] BigGun = null;
        public byte[] StartLight = null;        
        public int[] Credits = null;
        public byte WaterPump = 0;
        public byte Atomizer = 0; 
        public int BoxLight = 0;
        public int LightBelt1 = 0;
        public int LightBelt2 = 0;
        public int LightBelt3 = 0;
        public int LightBelt4 = 0;

        public TcpOutputData(int PlayerNumer) : base(PlayerNumer) { }
    }
}
