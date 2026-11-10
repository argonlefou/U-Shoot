using System;
using System.IO;
using System.Windows.Forms;
using UsCore.Config;

namespace U_Shoot.GUI
{
    public partial class Wnd_DefaultPath : Form
    {
        public Wnd_DefaultPath()
        {
            InitializeComponent();
        }

        private void Wnd_DefaultPath_Load(object sender, EventArgs e)
        {
            Txt_Flycast.Text = ProgramConfig.GetInstance().GetStringOption(Properties.Resources.DEFAULT_FLYCAST_PATH_OPTION);
            Txt_LinuxLoader.Text = ProgramConfig.GetInstance().GetStringOption(Properties.Resources.DEFAULT_LINUXLOADER_PATH_OPTION);
            Txt_Pcsx6x6Emulator.Text = ProgramConfig.GetInstance().GetStringOption(Properties.Resources.DEFAULT_PCSX2X6EMULATOR_PATH_OPTION);
            Txt_Rpcs3Emulator.Text = ProgramConfig.GetInstance().GetStringOption(Properties.Resources.DEFAULT_RPCS3EMULATOR_PATH_OPTION);
        }

        private void Btn_Flycast_Click(object sender, EventArgs e)
        { 
            if (OpenFileDialog("flycast.exe") == DialogResult.OK)
            {
                Txt_Flycast.Text = openFileDialog1.FileName;
            }
        }

        private void Btn_LinuxLoader_Click(object sender, EventArgs e)
        {
            if (OpenFileDialog("linuxloader.exe") == DialogResult.OK)
            {
                Txt_LinuxLoader.Text = openFileDialog1.FileName;
            }
        }

        private void Btn_Pcsx2x6Emulator_Click(object sender, EventArgs e)
        {
            if (OpenFileDialog("pcsx2-qt.exe") == DialogResult.OK)
            {
                Txt_Pcsx6x6Emulator.Text = openFileDialog1.FileName;
            }
        }

        private void Btn_Rpcs3Emulator_Click(object sender, EventArgs e)
        {
            if (OpenFileDialog("rpcs3.exe") == DialogResult.OK)
            {
                Txt_Rpcs3Emulator.Text = openFileDialog1.FileName;
            }
        }

        private DialogResult OpenFileDialog(string TargetFileName)
        {
            openFileDialog1.Title = "";
            openFileDialog1.FileName = "";

            openFileDialog1.Title = "Select " + TargetFileName;

            string TargetExtension = Path.GetExtension(TargetFileName);
            if (TargetExtension != string.Empty)
                openFileDialog1.Filter = TargetFileName + "|" + TargetFileName + "|" + TargetExtension + " files (*" + TargetExtension + ")|*" + TargetExtension + "|All files (*.*)|*.*";
            else
                openFileDialog1.Filter = TargetFileName + "|" + TargetFileName + "|All files (*.*)|*.*";

            return openFileDialog1.ShowDialog();
        }


        private void Btn_Save_Click(object sender, EventArgs e)
        {
            ProgramConfig.GetInstance().SetTextOption(Properties.Resources.DEFAULT_FLYCAST_PATH_OPTION, Txt_Flycast.Text);
            ProgramConfig.GetInstance().SetTextOption(Properties.Resources.DEFAULT_LINUXLOADER_PATH_OPTION, Txt_LinuxLoader.Text);
            ProgramConfig.GetInstance().SetTextOption(Properties.Resources.DEFAULT_PCSX2X6EMULATOR_PATH_OPTION, Txt_Pcsx6x6Emulator.Text);
            ProgramConfig.GetInstance().SetTextOption(Properties.Resources.DEFAULT_RPCS3EMULATOR_PATH_OPTION, Txt_Rpcs3Emulator.Text);

            if (!ProgramConfig.GetInstance().SaveConfig())
                MessageBox.Show("Error saving Config.xml", this.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);

            DialogResult = DialogResult.OK;
        }

        
    }
}
