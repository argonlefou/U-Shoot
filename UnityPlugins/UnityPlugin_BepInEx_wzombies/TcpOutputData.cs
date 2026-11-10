using System;

using UnityPlugin_BepInEx.Core;

namespace BepInEx_UShoot_Plugin
{
    public class TcpOutputData : TcpData
    {
        public byte[] IsPlaying = null;
        public byte[] StartLamp = null;
        public byte[] SmallWater = null;
        public byte[] BigWater = null;
        public byte[] GunMotor = null;
        public byte[] TicketFeeder = null;
        public byte BonusWeaponLamp = 0;
        public byte SeatVibrationMotor = 0;
        public byte WaterLevelLamp = 0;
        public byte[] Life = null;
        public byte[] Damaged = null;
        public int[] Credits = null;

        public TcpOutputData(int PlayerNumer) : base(PlayerNumer) { }
    }
}

