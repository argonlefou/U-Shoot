using System.Collections.Generic;

namespace UnityPlugin_BepInEx.Core
{
    public class PluginController
    {
        protected Dictionary<string, int> ButtonType { get; set; }

        protected PluginControllerButton[] _Buttons;
        public PluginControllerButton[] Buttons
        { get { return _Buttons; } }

        public PluginController(Dictionary<string, int> InputButtonsList)
        {
            ButtonType = InputButtonsList;

            _Buttons = new PluginControllerButton[ButtonType.Count];
            for (int i = 0; i < _Buttons.Length; i++)
            {
                _Buttons[i] = new PluginControllerButton();
            }
        }

        public void SetButtonKeyCode(string ButtonName, int iValue)
        {
            if (ButtonType.ContainsKey(ButtonName))
            {
                _Buttons[ButtonType[ButtonName]].SetKeyCode(iValue);
            }
        }
        public void SetButtonKeyCode(string ButtonName, string StrValue)
        {
            if (ButtonType.ContainsKey(ButtonName))
            {
                int iResult;
                if (int.TryParse(StrValue, out iResult))
                    _Buttons[ButtonType[ButtonName]].SetKeyCode(iResult);
            }
        }
        public int GetButtonKeyCode(string ButtonName)
        {
            if (ButtonType.ContainsKey(ButtonName))
                return _Buttons[ButtonType[ButtonName]].KeyCode;
            else
                return 0;
        }

        public void SetRawButtonState(string ButtonName, byte Value)
        {
            if (ButtonType.ContainsKey(ButtonName))
                _Buttons[ButtonType[ButtonName]].SetRawState(Value == 1 ? true : false);
        }

        public bool GetButton(string ButtonName)
        {
            if (ButtonType.ContainsKey(ButtonName))
                return _Buttons[ButtonType[ButtonName]].GetButton();
            else
                return false;
        }

        public bool GetButtonDown(string ButtonName)
        {
            if (ButtonType.ContainsKey(ButtonName))
                return _Buttons[ButtonType[ButtonName]].GetButtonDown();
            else
                return false;
        }

        public bool GetButtonUp(string ButtonName)
        {
            if (ButtonType.ContainsKey(ButtonName))
                return _Buttons[ButtonType[ButtonName]].GetButtonUp();
            else
                return false;
        }

        public bool GetRawButtonDown(string ButtonName)
        {
            if (ButtonType.ContainsKey(ButtonName))
                return _Buttons[ButtonType[ButtonName]].GetRawButtonDown();
            else
                return false;
        }

        public bool GetRawButtonUp(string ButtonName)
        {
            if (ButtonType.ContainsKey(ButtonName))
                return _Buttons[ButtonType[ButtonName]].GetRawButtonUp();
            else
                return false;
        }

        public void UpdateFrameInputStates()
        {
            foreach (PluginControllerButton b in _Buttons)
            {
                b.SetFrameState();
            }
        }
    }
}
