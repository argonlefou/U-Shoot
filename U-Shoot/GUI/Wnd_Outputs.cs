using System;
using System.Windows.Forms;
using UsCore.Config;

namespace U_Shoot.GUI
{
    public partial class Wnd_Outputs : Form
    {
        public Wnd_Outputs()
        {
            InitializeComponent();

            Rdo_OutputsWm.Checked = ProgramConfig.GetInstance().GetBoolOption(ProgramConfig.WINDOWS_OUTPUTS);
            Rdo_OutputsNetwork.Checked = ProgramConfig.GetInstance().GetBoolOption(ProgramConfig.NETWORK_OUTPUTS);
            Txt_OutputDelay.Value = ProgramConfig.GetInstance().GetNumericOption(ProgramConfig.OUTPUTS_POLLING_DELAY);
            Txt_OutputDamaged.Value = ProgramConfig.GetInstance().GetNumericOption(ProgramConfig.OUTPUTS_DAMAGED_DELAY);
            Txt_OutputRecoilOn.Value = ProgramConfig.GetInstance().GetNumericOption(ProgramConfig.OUTPUTS_RECOIL_ON_DELAY);
            Txt_OutputRecoilOff.Value = ProgramConfig.GetInstance().GetNumericOption(ProgramConfig.OUTPUTS_RECOIL_OFF_DELAY);
        }

        private void Rdo_Outputs_CheckedChanged(object sender, EventArgs e)
        {
            ProgramConfig.GetInstance().SetBoolOption(ProgramConfig.WINDOWS_OUTPUTS, Rdo_OutputsWm.Checked);
            ProgramConfig.GetInstance().SetBoolOption(ProgramConfig.NETWORK_OUTPUTS, Rdo_OutputsNetwork.Checked);
        }

        private void Txt_OutputDelay_ValueChanged(object sender, EventArgs e)
        {
            ProgramConfig.GetInstance().SetNumericOption(ProgramConfig.OUTPUTS_POLLING_DELAY, (int)Txt_OutputDamaged.Value);
        }

        private void Txt_OutputDamaged_ValueChanged(object sender, EventArgs e)
        {
            ProgramConfig.GetInstance().SetNumericOption(ProgramConfig.OUTPUTS_DAMAGED_DELAY, (int)Txt_OutputDamaged.Value);
        }

        private void Txt_OutputRecoilOn_ValueChanged(object sender, EventArgs e)
        {
            ProgramConfig.GetInstance().SetNumericOption(ProgramConfig.OUTPUTS_RECOIL_ON_DELAY, (int)Txt_OutputRecoilOn.Value);
        }

        private void Txt_OutputRecoilOff_ValueChanged(object sender, EventArgs e)
        {
            ProgramConfig.GetInstance().SetNumericOption(ProgramConfig.OUTPUTS_RECOIL_OFF_DELAY, (int)Txt_OutputRecoilOff.Value);
        }

        private void Btn_Save_Click(object sender, EventArgs e)
        {
            if (!ProgramConfig.GetInstance().SaveConfig())
                MessageBox.Show("Error saving Config.xml", this.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);

            this.Close();
        }
    }
}
