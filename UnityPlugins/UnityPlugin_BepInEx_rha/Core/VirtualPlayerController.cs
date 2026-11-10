using System.Collections.Generic;
using UnityEngine;

namespace UnityPlugin_BepInEx.Core
{
    public class VirtualPlayerController : PluginController
    {
        private int _ID = 0;
        public int ID
        { get { return _ID; } }

        public float Axis_X { get; private set; }
        public float Axis_Y { get; private set; }

        public VirtualPlayerController(int ID, Dictionary<string, int> InputButtonsList) : base (InputButtonsList) 
        {
            _ID = ID;

            foreach (KeyValuePair<string, int> button in InputButtonsList)
            {
                switch (button.Key)
                {
                    case "START": _Buttons[button.Value].SetKeyCode(49 + ID); break;
                    case "COIN": _Buttons[button.Value].SetKeyCode(53 + ID); break;
                    default: break;
                }
            }
        }

        public void SetAimingValues(Vector3 Position)
        {
            Axis_X = Position.x;
            Axis_Y = Position.y;
        }

        public Vector3 GetAimingPosition()
        {
            return new Vector3(Axis_X, Axis_Y);
        }
    }
}
