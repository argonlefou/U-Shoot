using System;
using System.Windows.Forms;
using UsCore.Config;
using UsCore.RawInput;

namespace U_Shoot.GUI
{
    public partial class Wnd_AxisOptions : Form
    {
        private InputAxesEntry _AxesEntry;
        private RawInputController _Controller;

        public Wnd_AxisOptions(InputAxesEntry AxesEntry)
        {
            InitializeComponent();

            _AxesEntry = AxesEntry;
            _Controller = RawInputManager.GetInstance().GetControllerFromDevicePath(_AxesEntry.DevicePath);

            if (_Controller == null)
            {
                if (MessageBox.Show("Cannot find corresponding RawInputController", this.Text, MessageBoxButtons.OK, MessageBoxIcon.Error) == DialogResult.OK)
                {
                    DialogResult = DialogResult.Cancel;
                }
            }
            else
            {
                try
                {
                    Lbl_Manufacturer.Text = _Controller.ManufacturerName;
                    Lbl_Product.Text = _Controller.ProductName;

                    Cbox_HID_XAxis.SelectedIndex = _AxesEntry.X.ID - 0x30;
                    Chk_InvertX.Checked = _AxesEntry.X.IsInverted;
                    Cbox_HID_YAxis.SelectedIndex = _AxesEntry.Y.ID - 0x30;
                    Chk_InvertY.Checked = _AxesEntry.Y.IsInverted;

                    Chk_Calibration.Checked = _AxesEntry.OverrideCalibration;
                    UpdateCalibrationGUI();
                }
                catch
                {
                    if (MessageBox.Show("Invalid config value found, closing...", this.Text, MessageBoxButtons.OK, MessageBoxIcon.Error) == DialogResult.OK)
                    {
                        DialogResult = DialogResult.Cancel;
                    }
                }
            }
        }

        #region GUI

        private void Chk_Calibration_CheckedChanged(object sender, EventArgs e)
        {
            UpdateCalibrationGUI();
        }

        private void Btn_Init_Calib_Click(object sender, EventArgs e)
        {
            Btn_Start_Calib.Enabled = true;
            Btn_Stop_Calib.Enabled = false;
            Txt_Calib_Xmin.Text = _Controller.Axes[GetAxisId(Cbox_HID_XAxis)].MinValue.ToString();
            Txt_Calib_Xmax.Text = _Controller.Axes[GetAxisId(Cbox_HID_XAxis)].MaxValue.ToString();
            Txt_Calib_Ymin.Text = _Controller.Axes[GetAxisId(Cbox_HID_YAxis)].MinValue.ToString();
            Txt_Calib_Ymax.Text = _Controller.Axes[GetAxisId(Cbox_HID_YAxis)].MaxValue.ToString();
        }

        private void Btn_Start_Calib_Click(object sender, EventArgs e)
        {
            Btn_Init_Calib.Enabled = false;
            Btn_Start_Calib.Enabled = false;
            Btn_Stop_Calib.Enabled = true;
            Txt_Calib_Xmin.Text = string.Empty;
            Txt_Calib_Xmax.Text = string.Empty;
            Txt_Calib_Ymin.Text = string.Empty;
            Txt_Calib_Ymax.Text = string.Empty;
            _Controller.Axes[GetAxisId(Cbox_HID_XAxis)].OnValueChanged += Axis_OnXValueChanged;
            _Controller.Axes[GetAxisId(Cbox_HID_YAxis)].OnValueChanged += Axis_OnYValueChanged;
        }

       

        private void Btn_Stop_Calib_Click(object sender, EventArgs e)
        {
            StopCalib();
        }

        private void Btn_Save_Click(object sender, EventArgs e)
        {
            StopCalib();

            _AxesEntry.X.ID = GetAxisId(Cbox_HID_XAxis);
            _AxesEntry.X.IsInverted = Chk_InvertX.Checked;
            _AxesEntry.Y.ID = GetAxisId(Cbox_HID_YAxis);
            _AxesEntry.Y.IsInverted = Chk_InvertY.Checked;

            _AxesEntry.OverrideCalibration = Chk_Calibration.Checked;
            if (_AxesEntry.OverrideCalibration)
            {
                _AxesEntry.X.MinValue = int.Parse(Txt_Calib_Xmin.Text);
                _AxesEntry.X.MaxValue = int.Parse(Txt_Calib_Xmax.Text);
                _AxesEntry.Y.MinValue = int.Parse(Txt_Calib_Ymin.Text);
                _AxesEntry.Y.MaxValue = int.Parse(Txt_Calib_Ymax.Text);
            }
            else
            {
                _AxesEntry.X.MinValue = _Controller.Axes[GetAxisId(Cbox_HID_XAxis)].MinValue;
                _AxesEntry.X.MaxValue = _Controller.Axes[GetAxisId(Cbox_HID_XAxis)].MaxValue;
                _AxesEntry.Y.MinValue = _Controller.Axes[GetAxisId(Cbox_HID_YAxis)].MinValue;
                _AxesEntry.Y.MaxValue = _Controller.Axes[GetAxisId(Cbox_HID_YAxis)].MaxValue;
            }

            Close();
        }

        private void Btn_Cancel_Click(object sender, EventArgs e)
        {
            StopCalib();
            Close();
        }

        private void UpdateCalibrationGUI()
        {
            if (Chk_Calibration.Checked)
            {
                Txt_Calib_Xmin.Enabled = true;
                Txt_Calib_Xmax.Enabled = true;
                Txt_Calib_Ymin.Enabled = true;
                Txt_Calib_Ymax.Enabled = true;
                Btn_Init_Calib.Enabled = true;
                Btn_Start_Calib.Enabled = true;
                Txt_Calib_Xmin.Text = _AxesEntry.X.MinValue.ToString();
                Txt_Calib_Ymin.Text = _AxesEntry.Y.MinValue.ToString();
                Txt_Calib_Xmax.Text = _AxesEntry.X.MaxValue.ToString();
                Txt_Calib_Ymax.Text = _AxesEntry.Y.MaxValue.ToString();
            }
            else
            {
                Txt_Calib_Xmin.Enabled = false;
                Txt_Calib_Xmax.Enabled = false;
                Txt_Calib_Ymin.Enabled = false;
                Txt_Calib_Ymax.Enabled = false;
                Txt_Calib_Xmin.Text = String.Empty;
                Txt_Calib_Ymin.Text = String.Empty;
                Txt_Calib_Xmax.Text = String.Empty;
                Txt_Calib_Ymax.Text = String.Empty;
                Btn_Init_Calib.Enabled = false;
                Btn_Start_Calib.Enabled = false;
                Btn_Stop_Calib.Enabled = false;
            }
        }

        #endregion
                
        private void StopCalib()
        {
            Btn_Init_Calib.Enabled = true;
            Btn_Start_Calib.Enabled = true;
            Btn_Stop_Calib.Enabled = false;
            _Controller.Axes[GetAxisId(Cbox_HID_XAxis)].OnValueChanged -= Axis_OnXValueChanged;
            _Controller.Axes[GetAxisId(Cbox_HID_YAxis)].OnValueChanged -= Axis_OnYValueChanged;
        }

        private int GetAxisId(ComboBox CboBox)
        {
            return CboBox.SelectedIndex + 0x30;
        }


        private void Axis_OnXValueChanged(object sender, RawInputAxis.ValueChangedEventArgs e)
        {
            if (Txt_Calib_Xmin.Text.Length == 0)
            {
                Txt_Calib_Xmin.Text = e.Value.ToString();
            }
            else
            {
                if (e.Value < int.Parse(Txt_Calib_Xmin.Text))
                    Txt_Calib_Xmin.Text = e.Value.ToString();
            }

            if (Txt_Calib_Xmax.Text.Length == 0)
            {
                Txt_Calib_Xmax.Text = e.Value.ToString();
            }
            else
            {
                if (e.Value > int.Parse(Txt_Calib_Xmax.Text))
                    Txt_Calib_Xmax.Text = e.Value.ToString();
            }
        }

        private void Axis_OnYValueChanged(object sender, RawInputAxis.ValueChangedEventArgs e)
        {
            if (Txt_Calib_Ymin.Text.Length == 0)
            {
                Txt_Calib_Ymin.Text = e.Value.ToString();
            }
            else
            {
                if (e.Value < int.Parse(Txt_Calib_Ymin.Text))
                    Txt_Calib_Ymin.Text = e.Value.ToString();
            }

            if (Txt_Calib_Ymax.Text.Length == 0)
            {
                Txt_Calib_Ymax.Text = e.Value.ToString();
            }
            else
            {
                if (e.Value > int.Parse(Txt_Calib_Ymax.Text))
                    Txt_Calib_Ymax.Text = e.Value.ToString();
            }
        }
    }
}
