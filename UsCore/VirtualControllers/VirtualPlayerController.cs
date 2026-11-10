using System.Collections.Generic;
using UsCore.Config;
using UsCore.Logging;
using UsCore.RawInput;

namespace UsCore.VirtualControllers
{
    public class VirtualPlayerController : VirtualController
    {       
        public int RawX;
        public int RawY;
        public int Computed_X;
        public int Computed_Y;

        private BaseConfig_Player _PlayerConfig;

        /// <summary>
        /// Constructor
        /// </summary>
        public VirtualPlayerController(BaseConfig_Player PlayerConfig) : base()
        {
            _PlayerConfig = PlayerConfig;
            PlayerId = (PlayerID)(PlayerConfig.Id - 1);
            Logger.WriteLog("[" + this.GetType().Name + "] Binding Player" + PlayerConfig.Id + " Buttons :");
            BindButtons(_PlayerConfig.Inputs);
            BindAxes(_PlayerConfig.Inputs);
        }

        private void BindAxes(List<BaseConfig_Input> Inputs)
        {
            foreach (BaseConfig_Input Input in Inputs)
            {
                if (Input is InputAxes)
                {
                    foreach (InputAxesEntry Key in ((InputAxes)Input).Keys)
                    {
                        RawInputController RiController = RawInputManager.GetInstance().GetControllerFromDevicePath(Key.DevicePath);
                        if (RiController != null)
                        {
                            AxisController = RiController;
                            RiController.Axes[Key.X.ID].OnValueChanged += VirtualPlayerController_OnAxisXChanged;
                            RiController.Axes[Key.Y.ID].OnValueChanged += VirtualPlayerController_OnAxisYChanged;
                            Logger.WriteLog(" + [" + RiController.ComputedDeviceName + "] Axis 0x" + Key.X.ID.ToString("X2") + " -> " + Input.Name + " X");
                            Logger.WriteLog(" + [" + RiController.ComputedDeviceName + "] Axis 0x" + Key.Y.ID.ToString("X2") + " -> " + Input.Name + " Y");
                        }
                    }
                }
            }
        }

        private void VirtualPlayerController_OnAxisXChanged(object sender, RawInputAxis.ValueChangedEventArgs e)
        {
            RawX = e.Value;
            Raise_AxisChangedEvent();
        }

        private void VirtualPlayerController_OnAxisYChanged(object sender, RawInputAxis.ValueChangedEventArgs e)
        {
            RawY = e.Value;
            Raise_AxisChangedEvent();
        }
    }
}
