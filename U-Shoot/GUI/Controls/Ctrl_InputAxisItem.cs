using System;
using System.ComponentModel;
using System.Windows.Forms;
using UsCore.Config;
using UsCore.RawInput;

namespace U_Shoot.GUI.Controls
{
    public partial class Ctrl_InputAxisItem : UserControl
    {
        private string _DeviceName;
        public string DeviceName
        {
            get { return _DeviceName; }
            set 
            {
                Lbl_DeviceName.Text = value;
                _DeviceName = value;
            }
        }

        [Browsable(true)]
        [Category("Action")]
        [Description("Invoked when user clicks button")]
        public event EventHandler ButtonClick;

        public string DevicePath
        { get; set; }

        public RawInputDeviceType DeviceType
        { get; set; }

        public bool IsAbsoluteAxis
        { get; set; }

        public InputAxesEntry AxesEntry
        { get; private set; }

        public Ctrl_InputAxisItem()
        {
            InitializeComponent();
        }

        public Ctrl_InputAxisItem(InputAxesEntry Entry)
        {
            InitializeComponent();

            AxesEntry = Entry;
            DevicePath = AxesEntry.DevicePath;
            DeviceName = AxesEntry.DeviceName;
            IsAbsoluteAxis = AxesEntry.IsAbsolute;

            RawInputDeviceType DeviceType = RawInputDeviceType.RIM_TYPEMOUSE;
            Enum.TryParse(AxesEntry.DeviceType, out DeviceType);
                SetIcon(DeviceType);

            if (DeviceType == RawInputDeviceType.RIM_TYPEMOUSE)
            {
                Btn_Options.Visible = false;
            }
        }

        public void SetIcon(RawInputDeviceType dType)
        {
            DeviceType = dType;

            if (dType == RawInputDeviceType.RIM_TYPEKEYBOARD)
                Pnl_Icon.BackgroundImage = Properties.Resources.keyboard;
            else if (dType == RawInputDeviceType.RIM_TYPEMOUSE)
            {
                if (IsAbsoluteAxis)
                    Pnl_Icon.BackgroundImage = Properties.Resources.pistol;
                else
                    Pnl_Icon.BackgroundImage = Properties.Resources.cursor;
            }
            else if (dType == RawInputDeviceType.RIM_TYPEHID)
                Pnl_Icon.BackgroundImage = Properties.Resources.game_controller;
        }

        private void Btn_Remove_Click(object sender, EventArgs e)
        {
            //bubble the event up to the parent
            if (ButtonClick != null)
                ButtonClick(this, e);
        }

        private void Btn_Options_Click(object sender, EventArgs e)
        {
            Wnd_AxisOptions w = new Wnd_AxisOptions(AxesEntry);
            if (w.DialogResult != DialogResult.Cancel)
            {
                w.ShowDialog();
            }
            else
            {
                w.Close();
            }
           

        }
    }
}
