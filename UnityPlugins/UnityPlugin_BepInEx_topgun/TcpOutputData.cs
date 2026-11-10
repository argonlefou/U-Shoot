using System;

using UnityPlugin_BepInEx.Core;

namespace BepInEx_USoot_Plugin
{
    public class TcpOutputData : TcpData
    {
        public byte IsPlaying = 0;
        public byte Recoil = 0;
        public int Ammo = 0;
        public int Credits = 0;
        public byte[] GiftDoor = null;
        public byte LeftKeyLight = 0;
        public byte CenterKeyLight = 0;
        public byte RightKeyLight = 0;
        public byte ScreenSideLight = 0;
        public byte GiftLight = 0;
        public byte TicketFeeder = 0;

        public TcpOutputData(int PlayerNumer) : base(PlayerNumer) { }
    }
}

