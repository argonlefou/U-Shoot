using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using UsCore.RawInput;

namespace U_Shoot.GUI.Controls
{
    public partial class Ctrl_InputButtonItem : UserControl
    { 
        private string _DevicePath;

        [Description("Device ID"), Category("Data")]
        public string DeviceName
        {
            get => Lbl_DeviceName.Text;
            set => Lbl_DeviceName.Text = value;
        }

        [Description("Button Name"), Category("Data")]
        public string ButtonName
        {
            get => Lbl_ButtonName.Text;
            set => Lbl_ButtonName.Text = value;
        }

        [Browsable(true)]
        [Category("Action")]
        [Description("Invoked when user clicks button")]
        public event EventHandler ButtonClick;

        public string DevicePath
        { 
            get { return _DevicePath; }
            set 
            {
                //Graying unplugged devices so we can see it better 
                _DevicePath = value;
                if (!RawInputManager.GetInstance().IsControllerPlugged(_DevicePath))
                {
                    Lbl_DeviceName.ForeColor = Color.Gray;
                    Lbl_DeviceName.Font = new Font(Lbl_DeviceName.Font, Lbl_DeviceName.Font.Style | FontStyle.Italic | FontStyle.Strikeout);
                    Lbl_ButtonName.ForeColor = Color.Gray;
                    Lbl_ButtonName.Font = new Font(Lbl_ButtonName.Font, Lbl_ButtonName.Font.Style | FontStyle.Italic | FontStyle.Strikeout);
                }
            }
        }

        public int ButtonId
        { get; set; }

        public RawInputDeviceType DeviceType 
        { get; set; }


        public Ctrl_InputButtonItem()
        {
            InitializeComponent();
        }

        public void SetIcon(RawInputDeviceType dType)
        {
            DeviceType = dType;

            if (dType == RawInputDeviceType.RIM_TYPEKEYBOARD)
                Pnl_Icon.BackgroundImage = Properties.Resources.keyboard;
            else if (dType == RawInputDeviceType.RIM_TYPEMOUSE)
                Pnl_Icon.BackgroundImage = Properties.Resources.pistol;
            else if (dType == RawInputDeviceType.RIM_TYPEHID)
                Pnl_Icon.BackgroundImage = Properties.Resources.game_controller;
        }

        private void Btn_Remove_Click(object sender, System.EventArgs e)
        {
            //bubble the event up to the parent
            if (ButtonClick != null)
                ButtonClick(this, e);
        }
    }
}
