using System;

using UnityPlugin_BepInEx.Core;

namespace BepInEx_UShoot_Plugin
{
    public class TcpOutputData : TcpData
    {
        public byte Atomizer = 0;
        public byte[] IsPlaying = null;
        public byte[] Shake = null;
        public byte[] RotatingMotor = null;
        public byte[] WaterPower = null;
        public byte[] StartLight = null;
        public byte[] PlayerLight = null;
        public byte[] LightBelt = null;
        public byte[] Damaged = null;
        public byte[] Recoil = null;
        public float[] Life = null;
        public int[] Credits = null;

        public TcpOutputData(int PlayerNumer) : base(PlayerNumer) { }
    }
}

