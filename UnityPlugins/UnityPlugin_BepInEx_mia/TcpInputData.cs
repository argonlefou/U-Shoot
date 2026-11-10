using UnityPlugin_BepInEx.Core;

namespace BepInEx_UShoot_Plugin
{
    public class TcpInputData : TcpData
    {
        //Game Inputs
        public float[] Axis_X = null;
        public float[] Axis_Y = null;
        public byte[] TriggerL = null;
        public byte[] TriggerR = null;
        public byte[] Start = null;
        public byte[] Coin = null;
        public byte Exit = 0;
        //Generic Inputs
        public byte HideCrosshairs = 0;
        public byte EnableInputsHack = 0;

        public TcpInputData(int PlayerNumer) : base(PlayerNumer) { }
    }
}
