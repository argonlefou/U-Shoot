using System;
using System.Collections.Generic;
using UsCore.Config;
using UsCore.Logging;
using UsCore.RawInput;

namespace UsCore.VirtualControllers
{
    /// <summary>
    /// Virtual button class will have a unique Text identifier (SERVICE, COIN, etc....) for ONE button,
    /// and can contain multiple RawInputButtons entries (based on config file)
    /// Each and every RawInputButton will trigger OnKeyDown / OnKeyUp event, allowing to trigger the VirtualButton event and change it's state with any of these ones
    /// </summary>
    public class VirtualButton
    {
        #region Events

        public delegate void ButtonDownEventHandler(object sender, StateChangedEventArgs e);
        public event ButtonDownEventHandler OnButtonDown;

        public delegate void ButtonUpEventHandler(object sender, StateChangedEventArgs e);
        public event ButtonUpEventHandler OnButtonUp;

        public class StateChangedEventArgs : EventArgs
        {
            public string ButtonName { get; private set; }

            public StateChangedEventArgs(string ButtonName)
            {
                this.ButtonName = ButtonName;
            }
        }

        #endregion

        public string ButtonName
        { get; private set; }

        public bool IsPressed 
        { get; private set; }

        private List<RawInputButton> _RiButtons;

        public VirtualButton(InputButton iButton)
        {
            ButtonName = iButton.Name;
            IsPressed = false;
            _RiButtons = new List<RawInputButton>();

            BindRawInputButton(iButton);
        }

        /// <summary>
        /// Reading the InputButton config node, each Key entry will be added as reference to get the KeyDown/KeyUp event
        /// </summary>
        /// <param name="iButton"></param>
        public void BindRawInputButton(InputButton iButton)
        {
            ButtonName = iButton.Name;

            foreach (InputButtonEntry Key in iButton.Keys)
            {
                RawInputController RiController = RawInputManager.GetInstance().GetControllerFromDevicePath(Key.DevicePath);
                if (RiController != null)
                {
                    if (RiController is RawInput_Keyboard)
                    {
                        ((RawInput_Keyboard)RiController).RegisterButton(Key.ButtonId);
                    }

                    RawInputButton RiButton = RiController.GetButton(Key.ButtonId);
                    if (RiButton != null)
                    {
                        if (!_RiButtons.Contains(RiButton))
                        {
                            RiButton.OnKeyDown += RawInputButton_KeyDown;
                            RiButton.OnKeyUp += RawInputButton_KeyUp;
                            _RiButtons.Add(RiButton);
                            Logger.WriteLog(" + [" + RiController.ComputedDeviceName + "] " + Key.ButtonName + " -> " + ButtonName);
                        }
                    }
                }
            }
        }

        private void RawInputButton_KeyDown(object sender, EventArgs e)
        {
            IsPressed = true;
            if (OnButtonDown != null)
                OnButtonDown(this, new StateChangedEventArgs(ButtonName));
        }

        private void RawInputButton_KeyUp(object sender, EventArgs e)
        {
            IsPressed = false;
            if (OnButtonUp != null)
                OnButtonUp(this, new StateChangedEventArgs(ButtonName));
        }


    }
}
