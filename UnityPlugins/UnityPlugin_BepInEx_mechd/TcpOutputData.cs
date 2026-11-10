using System;

using UnityPlugin_BepInEx.Core;

namespace BepInEx_UShoot_Plugin
{
    public class TcpOutputData : TcpData
    {
        public byte[] IsPlaying = null;
        public byte[] BallMotor = null;
        public byte[] StartLamp = null;
        public byte[] PlayerBonusWeaponLamp = null;
        public byte[] SpineMotor = null;
        public byte BonusWeaponLamp = 0;
        public byte SeatVibrationLamp = 0;
        public byte WaterFallLamp = 0;
        public byte WaterLevelLamp = 0;
        public float[] Life = null;
        public byte[] Damaged = null;
        public int[] Credits = null;

        public TcpOutputData(int PlayerNumer) : base(PlayerNumer) { }
    }
}

