using UnityPlugin_BepInEx.Core;

namespace BepInEx_UShoot_Plugin
{
    public class TcpInputData : TcpData
    {
        public float[] Axis_X = null;
        public float[] Axis_Y = null;
        public byte[] Coin = null;
        public byte[] Start = null;
        public byte[] Trigger = null;
        public byte[] Reload = null;
        public byte[] ChangeWeapon = null;
        public byte Exit;
        public byte Test;
        public byte MenuDown;
        public byte MenuLeft;
        public byte MenuRight;

        public byte HideCrosshairs;
        public byte EnableInputsHack;

        public TcpInputData(int PlayerNumer) : base(PlayerNumer) { }
    }
}

