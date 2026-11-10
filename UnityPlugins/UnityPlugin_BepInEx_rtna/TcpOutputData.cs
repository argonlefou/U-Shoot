using System;

using UnityPlugin_BepInEx.Core;

namespace BepInEx_UShoot_Plugin
{
    public class TcpOutputData : TcpData
    {
        public byte[] IsPlaying = null;
        public byte[] Recoil = null;
        public UInt32[] Credits = null;
        public UInt16 P1_Lmp_Start = 0;
        public UInt16 P1_Lmp_SeatPuck = 0;
        public UInt16 P1_Lmp_SeatMarquee = 0;
        public UInt16 P1_Lmp_SeatRear_R = 0;
        public UInt16 P1_Lmp_SeatRear_O = 0;
        public UInt16 P1_Lmp_SeatRear_B = 0;
        public UInt16 P2_Lmp_Start = 0;
        public UInt16 P2_Lmp_SeatPuck = 0;
        public UInt16 P2_Lmp_SeatMarquee = 0;
        public UInt16 P2_Lmp_SeatRear_R = 0;
        public UInt16 P2_Lmp_SeatRear_O = 0;
        public UInt16 P2_Lmp_SeatRear_B = 0;
        public UInt16 Cab_Lmp_R = 0;
        public UInt16 Cab_Lmp_G = 0;
        public UInt16 Cab_Lmp_B = 0;
        public UInt16 Cab_Lmp_RearSeat = 0;

        public TcpOutputData(int PlayerNumer) : base(PlayerNumer) { }
    }
}

