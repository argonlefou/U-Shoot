using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Forms;
using UsCore.Config;
using UsCore.RawInput;

namespace U_Shoot.GUI.Controls
{
    public partial class Ctrl_InputAxis : UserControl
    {
        private const int FLP_ITEM_HEIGHT = 26;
        private const int FLP_WIDTH_MARGIN = 2;

        public bool _Expanded = false;

        [Description("Button Name"), Category("Data")]
        public string ButtonName
        {
            get => Lbl_ButtonName.Text;
            set => Lbl_ButtonName.Text = value;
        }

        public Ctrl_InputAxis()
        {
            InitializeComponent();
        }
        public Ctrl_InputAxis(List<InputAxesEntry> Keys)
        {
            InitializeComponent();

            foreach (InputAxesEntry k in Keys)
            {
                Ctrl_InputAxisItem a = new Ctrl_InputAxisItem(k);                
                AddEntry(a);
            }
        }

        private void Ctrl_InputAxis_Load(object sender, EventArgs e)
        {
            FLP_Input.Height = FLP_Input.Controls.Count * FLP_ITEM_HEIGHT;
            if (FLP_Input.Controls.Count > 0)
                SetExpandState(true);
            else
                SetExpandState(false);
        }

        public void ShowDefaultIcon(bool Visibility)
        {
            Pnl_DefaultConfig.Visible = Visibility;
        }

        private void Btn_Expand_Click(object sender, EventArgs e)
        {
            if (FLP_Input.Controls.Count > 0)
            {
                if (!_Expanded)
                {
                    SetExpandState(true);
                }
                else
                {
                    SetExpandState(false);
                }
            }
        }

        /// <summary>
        /// Resize the control FlowLayout panel based on the added items
        /// and set the display mode (Expanded/Collapsed)
        /// and set the Expand Icon
        /// </summary>
        private void SetExpandState(bool Expand)
        {
            if (Expand)
            {
                _Expanded = true;
                FLP_Input.Visible = true;
                this.Height = Lbl_ButtonName.Height + FLP_Input.Height;
                SetExpandButtonIcon();
            }
            else
            {
                _Expanded = false;
                FLP_Input.Visible = false;
                this.Height = Lbl_ButtonName.Height;
                SetExpandButtonIcon();
            }
        }

        /// <summary>
        /// The icon will either be 
        /// 1) a clickable '+'
        /// 2) a clickable '-' 
        /// 3) a grayed '+' if no entry are in the list
        /// </summary>
        private void SetExpandButtonIcon()
        {
            if (_Expanded)
            {
                Btn_Expand.BackgroundImage = Properties.Resources.CollapseMinus_16px;
            }
            else
            {
                if (FLP_Input.Controls.Count > 0)
                    Btn_Expand.BackgroundImage = Properties.Resources.ExpandPlus_16px;
                else
                    Btn_Expand.BackgroundImage = Properties.Resources.ExpandPlusGrey_16px;
            }

        }

        private void Btn_AddEntry_Click(object sender, EventArgs e)
        {
            Wnd_InputListener w = new Wnd_InputListener(true);
            if (w.ShowDialog() == DialogResult.OK)
            {
                FLP_Input.Controls.Clear();
                FLP_Input.Height -= FLP_ITEM_HEIGHT;

                InputAxesEntry aEntry = new InputAxesEntry();
                aEntry.DevicePath = w.DeviceId;
                aEntry.DeviceName = w.DeviceName;
                aEntry.IsAbsolute = w.IsAbsoluteAxis;
                aEntry.DeviceType = w.DeviceType.ToString();
                aEntry.OverrideCalibration = false;
                aEntry.X = new InputAxis();
                aEntry.X.ID = 0x30;
                aEntry.X.IsInverted = false;
                aEntry.X.MinValue = RawInputManager.GetInstance().GetControllerFromDevicePath(w.DeviceId).Axes[aEntry.X.ID].MinValue;
                aEntry.X.MaxValue = RawInputManager.GetInstance().GetControllerFromDevicePath(w.DeviceId).Axes[aEntry.X.ID].MaxValue;

                aEntry.Y = new InputAxis();
                aEntry.Y.ID = 0x31;
                aEntry.Y.IsInverted = false;
                aEntry.Y.MinValue = RawInputManager.GetInstance().GetControllerFromDevicePath(w.DeviceId).Axes[aEntry.Y.ID].MinValue;
                aEntry.Y.MaxValue = RawInputManager.GetInstance().GetControllerFromDevicePath(w.DeviceId).Axes[aEntry.Y.ID].MaxValue;

                Ctrl_InputAxisItem a = new Ctrl_InputAxisItem(aEntry);

                /*foreach (Control c in FLP_Input.Controls)
                {
                    FLP_Input.Height -= FLP_ITEM_HEIGHT;
                }
                FLP_Input.Controls.Clear();*/

                AddEntry(a);
                SetExpandState(true);

                w.Close();

                //Unfocus clicked button or ENTER / SPACE key will trigger new listener
                Lbl_ButtonName.Focus();
            }
        }

        private void AddEntry(Ctrl_InputAxisItem Item)
        {
            Item.Width = FLP_Input.Width - FLP_WIDTH_MARGIN;
            FLP_Input.Controls.Add(Item);
            FLP_Input.Height += FLP_ITEM_HEIGHT;
            Item.ButtonClick += Btn_Remove_Click;
        }

        public ControlCollection GetInputItems()
        {
            return FLP_Input.Controls;
        }

        /// <summary>
        /// Bubbled event from any entry "Remove" Button
        /// </summary>
        private void Btn_Remove_Click(object sender, EventArgs e)
        {
            foreach (Control c in FLP_Input.Controls)
            {
                if (c == (Control)sender)
                {
                    FLP_Input.Controls.Remove(c);
                    FLP_Input.Height -= FLP_ITEM_HEIGHT;
                    if (FLP_Input.Controls.Count > 0)
                        SetExpandState(_Expanded);
                    else
                        SetExpandState(false);
                    break;
                }
            }
        }

        private void FLP_Input_SizeChanged(object sender, EventArgs e)
        {
            FlowLayoutPanel Flp = (FlowLayoutPanel)sender;
            Flp.SuspendLayout();
            foreach (Control c in Flp.Controls)
            {
                c.Width = Flp.Width;
            }
            Flp.ResumeLayout();
        }
    }
}
