using System;
using System.Collections.Generic;
using UsCore.Config;
using UsCore.RawInput;

namespace UsCore.VirtualControllers
{
    public class VirtualController
    {
        #region Events

        public delegate void ButtonDownEventHandler(object sender, ButtonDownEventArgs e);
        public event ButtonDownEventHandler OnButtonDown;

        public delegate void ButtonUpEventHandler(object sender, ButtonDownEventArgs e);
        public event ButtonUpEventHandler OnButtonUp;

        public class ButtonDownEventArgs : EventArgs
        {
            public PlayerID Id { get; private set; }
            public string ButtonName { get; private set; }

            public ButtonDownEventArgs(PlayerID Id, string ButtonName)
            {
                this.Id = Id;
                this.ButtonName = ButtonName;
            }
        }

        public delegate void AxisChangedEventHandler(object sender, AxisChangedEventArgs e);
        public event AxisChangedEventHandler OnAxisChanged;

        public class AxisChangedEventArgs : EventArgs
        {
            public PlayerID Id { get; private set; }

            public AxisChangedEventArgs(PlayerID Id)
            {
                this.Id = Id;
            }
        }

        #endregion

        public PlayerID PlayerId
        { get; protected set; }

        public Dictionary<string, VirtualButton> Buttons
        { get; protected set; }

        public RawInputController AxisController
        { get; protected set; }

        public VirtualController()
        {
            PlayerId = PlayerID.None;
            Buttons = new Dictionary<string, VirtualButton>();
        }

        /// <summary>
        /// Looking for all different inputs for a same button and bind their events
        /// </summary>
        protected void BindButtons(List<BaseConfig_Input> Inputs)
        {
            foreach (BaseConfig_Input Input in Inputs)
            {
                if (Input is InputButton)
                {
                    if (!Buttons.ContainsKey(Input.Name))
                    {
                        VirtualButton VButton = new VirtualButton((InputButton)Input);
                        VButton.OnButtonDown += VirtualButton_OnButtonDown;
                        VButton.OnButtonUp += VirtualButton_OnButtonUp;
                        Buttons.Add(Input.Name, VButton);
                    }
                }
            }
        }


        /// <summary>
        /// Using a procedure to get the Button state in the Dictionnary to prevent any NULL exception
        /// </summary>
        /// <param name="ButtonName">Button name</param>
        /// <returns>Required Button pressed sate TRUE or FALSE. FALSE is Button is not found</returns>
        public bool GetButtonState(string ButtonName)
        {
            if (Buttons.ContainsKey(ButtonName))
                return Buttons[ButtonName].IsPressed;

            return false;
        }

        protected virtual void VirtualButton_OnButtonDown(object sender, VirtualButton.StateChangedEventArgs e)
        {
            if (OnButtonDown != null)
                OnButtonDown(this, new ButtonDownEventArgs(PlayerId, e.ButtonName));
        }

        protected virtual void VirtualButton_OnButtonUp(object sender, VirtualButton.StateChangedEventArgs e)
        {
            if (OnButtonUp != null)
                OnButtonUp(this, new ButtonDownEventArgs(PlayerId, e.ButtonName));
        }

        protected virtual void VirtualController_OnAxisXChanged(object sender, EventArgs e)
        {
            Raise_AxisChangedEvent();
        }
        protected virtual void VirtualController_OnAxisYChanged(object sender, EventArgs e)
        {
            Raise_AxisChangedEvent();
        }

        protected void Raise_AxisChangedEvent()
        {
            if (OnAxisChanged != null)
                OnAxisChanged(this, new AxisChangedEventArgs(PlayerId));
        }
    }

    public enum PlayerID : int
    {
        None = -1,
        Player1 = 0,
        Player2,
        Player3,
        Player4
    }
}
