using System;
using System.Collections.Generic;
using System.Windows.Forms;
using UsCore.RawInput;

namespace U_Shoot.GUI
{
    public partial class Wnd_InputListener : Form
    {
        private const string MESSAGE_BODY_BUTTON_SELECTION = "Press a key/button to select ...";
        private const string MESSAGE_BODY_AXIS_SELECTION = "Move a controller to select ...";

        private bool _IsForAxis;
        private int _AutoCloseDelay_Seconds = 5;

        private RawInputDeviceType _RawInputDeviceType;
        private int _ButtonId;
        private string _ButtonName;
        private string _DeviceId;
        private string _DeviceName;
        private bool _IsAbsoluteAxis;

        private List<RecordedAxis> _RecordedAxisList;

        #region Accessors

        public RawInputDeviceType DeviceType
        { get { return _RawInputDeviceType; } }

        public int ButtonId
        { get { return _ButtonId; } }

        public string ButtonName
        { get { return _ButtonName; } }

        public string DeviceId
        { get { return _DeviceId; } }

        public string DeviceName
        { get { return _DeviceName; } }

        public bool IsAbsoluteAxis
        {  get { return _IsAbsoluteAxis; } }

        #endregion

        public Wnd_InputListener(bool IsForAxis = false)
        {
            InitializeComponent();
            _IsForAxis = IsForAxis;

            _RawInputDeviceType = RawInputDeviceType.RIM_TYPEMOUSE;
            _ButtonId = 0;
            _ButtonName = string.Empty;
            _DeviceId = string.Empty;

            if (_IsForAxis)
            {
                _RecordedAxisList = new List<RecordedAxis>();
                foreach (RawInputController c in RawInputManager.GetInstance().RawInputControllers)
                {
                    c.OnAnyAxisChanged_WithDeadzone += OnAnyAxisChanged;
                }
            }
            else
            {
                foreach (RawInputController c in RawInputManager.GetInstance().RawInputControllers)
                {
                    c.OnAnyKeyPressed += OnAnyKeyPressed;                    
                }
            }

            Tmr_AutoClose.Start();
            UpdateMessage();
        }

        private void OnAnyAxisChanged(object sender, RawInputController.AnyAxisChangedEventArgs e)
        {
            RawInputController c = (RawInputController)sender;
            if (c.DeviceType == RawInputDeviceType.RIM_TYPEMOUSE)
            {
                RecordAxis(c);
            }
            else if (c.DeviceType == RawInputDeviceType.RIM_TYPEHID)
            {
                bool AxisAlreadyRecorded = false;
                foreach (RecordedAxis Axis in _RecordedAxisList)
                {
                    if (Axis.DevicePath == c.DevicePath && Axis.AxisId == e.AxisId)
                    {
                        AxisAlreadyRecorded = true;
                    }
                }
                
                if (!AxisAlreadyRecorded)
                {
                    _RecordedAxisList.Add(new RecordedAxis(c,e.AxisId, e.Value));
                }
                else
                {
                    foreach (RecordedAxis Axis in _RecordedAxisList)
                    {
                        if (Axis.DevicePath == c.DevicePath && Axis.AxisId == e.AxisId)
                        {
                            Axis.SetNewValue(e.Value);
                            if (Axis.IsRecordValid())
                            {
                                RecordAxis(c);                                
                            }
                            break;
                        }
                    }
                }
            } 
        }
        private void RecordAxis(RawInputController RiController)
        {
            _DeviceId = RiController.DevicePath;
            _DeviceName = RiController.ComputedDeviceName;
            _RawInputDeviceType = RiController.DeviceType;
            _IsAbsoluteAxis = RiController.IsAbsoluteAxis;
            DialogResult = DialogResult.OK;
        }

        private void OnAnyKeyPressed(object sender, RawInputController.AnyKeyPressedEventArgs e)
        {
            RawInputController c = (RawInputController)sender;
            _DeviceId = c.DevicePath;
            _DeviceName = c.ComputedDeviceName;
            _ButtonId = e.KeyId;
            _ButtonName = e.KeyName;
            _RawInputDeviceType = c.DeviceType;
            
            DialogResult = DialogResult.OK;
        }

        private void Tmr_AutoClose_Tick(object sender, System.EventArgs e)
        {
            _AutoCloseDelay_Seconds -= 1;
            UpdateMessage();
            if (_AutoCloseDelay_Seconds <= 0)
                CloseWindow();
        }

        private void UpdateMessage()
        {
            if (_IsForAxis)
                Lbl_Message.Text = MESSAGE_BODY_AXIS_SELECTION + "\n\n" + _AutoCloseDelay_Seconds.ToString();            
            else
                Lbl_Message.Text = MESSAGE_BODY_BUTTON_SELECTION + "\n\n" + _AutoCloseDelay_Seconds.ToString();
        }

        private void CloseWindow()
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        private void Wnd_InputListener_FormClosed(object sender, FormClosedEventArgs e)
        {
            Tmr_AutoClose.Stop();

            if (_IsForAxis)
            {
                foreach (RawInputController c in RawInputManager.GetInstance().RawInputControllers)
                {
                    c.OnAnyAxisChanged_WithDeadzone -= OnAnyAxisChanged;
                }
            }
            else
            {
                foreach (RawInputController c in RawInputManager.GetInstance().RawInputControllers)
                {
                    c.OnAnyKeyPressed -= OnAnyKeyPressed;                    
                }
            }
        }
    }

    public class RecordedAxis
    {
        private const float TARGET_AXIS_RANGE_PERCENT = 0.2f;

        private RawInputController _Controller;
        public int AxisId;
        private int _FirstValue;
        private int _AxisValue;
        private int _TargetAxisRange;
        private bool _RecordValid;

        public string DevicePath;
        
       

        public RecordedAxis(RawInputController RiController, int AxisId, int FirstValue)
        {
            _Controller = RiController;
            this.AxisId = AxisId;
            _FirstValue = FirstValue;
            _AxisValue = FirstValue;
            _RecordValid = false;
            DevicePath = RiController.DevicePath;
            _TargetAxisRange = (int)(TARGET_AXIS_RANGE_PERCENT * (float)_Controller.Axes[AxisId].MaxValue);
        }

        public void SetNewValue(int AxisValue)
        {
            _AxisValue = AxisValue;

            int RecordedAxisRange = Math.Abs(_AxisValue - _FirstValue);
            if (RecordedAxisRange > _TargetAxisRange)
                _RecordValid = true;
        }

        public bool IsRecordValid()
        {
            return _RecordValid;
        }
    }
}
