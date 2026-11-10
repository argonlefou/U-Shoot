using System.Collections.Generic;
using UnityEngine;

namespace UnityPlugin_BepInEx.Core
{
    public class VirtualGameController : PluginController
    {
        public VirtualGameController(Dictionary<string, int> InputButtonsList) : base(InputButtonsList) 
        {
            foreach (KeyValuePair<string, int> button in InputButtonsList)
            {
                switch (button.Key)
                {
                    case "EXIT": _Buttons[button.Value].SetKeyCode((int)KeyCode.Escape); break;
                    case "SERVICE": _Buttons[button.Value].SetKeyCode((int)KeyCode.Alpha9); break;
                    case "TEST": _Buttons[button.Value].SetKeyCode((int)KeyCode.Alpha0); break;
                    case "COIN": _Buttons[button.Value].SetKeyCode((int)KeyCode.Alpha5); break;
                    case "MENU_UP": _Buttons[button.Value].SetKeyCode((int)KeyCode.UpArrow); break;
                    case "MENU_DOWN": _Buttons[button.Value].SetKeyCode((int)KeyCode.DownArrow); break;
                    case "MENU_LEFT": _Buttons[button.Value].SetKeyCode((int)KeyCode.LeftArrow); break;
                    case "MENU_RIGHT": _Buttons[button.Value].SetKeyCode((int)KeyCode.RightArrow); break;
                    case "MENU_SELECT": _Buttons[button.Value].SetKeyCode((int)KeyCode.Return); break;
                    default: break;
                }
            }

            /*if (ButtonType.ContainsKey("EXIT"))
                _Buttons[ButtonType["EXIT"]].SetKeyCode((int)KeyCode.Escape);

            if (ButtonType.ContainsKey("SERVICE"))
                _Buttons[ButtonType["SERVICE"]].SetKeyCode((int)KeyCode.Alpha9);

            if (ButtonType.ContainsKey("TEST"))
                _Buttons[ButtonType["TEST"]].SetKeyCode((int)KeyCode.Alpha0);

            if (ButtonType.ContainsKey("COIN"))
                _Buttons[ButtonType["COIN"]].SetKeyCode((int)KeyCode.Alpha5);

            if (ButtonType.ContainsKey("MENU_UP"))
                _Buttons[ButtonType["MENU_UP"]].SetKeyCode((int)KeyCode.UpArrow);

            if (ButtonType.ContainsKey("MENU_DOWN"))
                _Buttons[ButtonType["MENU_DOWN"]].SetKeyCode((int)KeyCode.DownArrow);

            if (ButtonType.ContainsKey("MENU_SELECT"))
                _Buttons[ButtonType["MENU_SELECT"]].SetKeyCode((int)KeyCode.Return);*/
        }
    }
}
