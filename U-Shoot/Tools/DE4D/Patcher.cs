using System;
using System.IO;
using System.Windows.Forms;
using UsCore.Config;

namespace U_Shoot.Tools.DE4D
{
    public partial class Patcher : Form
    {
        private string _GameFolderPath = string.Empty;

        private byte[] SensorMessageOff_Data = new byte[] { 0x20, 0, 0x20, 0, 0x20, 0, 0x20, 0, 0x20, 0, 0x20, 0, 0x20, 0, 0x20, 0, 0x20, 0, 0x20, 0, 0x20, 0, 0x20, 0, 0x20, 0, 0x20, 0, 0x20, 0, 0x20, 0, 0x20, 0, 0x20, 0, 0x20, 0, 0x20, 0, 0x20, 0, 0x20, 0, 0x20, 0, 0x20, 0, 0x20, 0, 0x20, 0, 0x20, 0, 0x20, 0 };
        private byte[] SensorMessage1_Data = new byte[] { 0x33, 0x00, 0x2D, 0x00, 0x35, 0x00, 0x20, 0x00, 0x56, 0x00, 0x49, 0x00, 0x54, 0x00, 0x41, 0x00, 0x4C, 0x00, 0x20, 0x00, 0x53, 0x00, 0x49, 0x00, 0x47, 0x00, 0x4E, 0x00, 0x53, 0x00, 0x20, 0x00, 0x43, 0x00, 0x45, 0x00, 0x4E, 0x00, 0x53, 0x00, 0x4F, 0x00, 0x52, 0x00, 0x20, 0x00, 0x45, 0x00, 0x52, 0x00, 0x52, 0x00, 0x4F, 0x00, 0x52, 0x00 };
        private byte[] SensorMessage2_Data = new byte[] { 0x33, 0x00, 0x2D, 0x00, 0x35, 0x00, 0x20, 0x00, 0x56, 0x00, 0x49, 0x00, 0x54, 0x00, 0x41, 0x00, 0x4C, 0x00, 0x20, 0x00, 0x53, 0x00, 0x49, 0x00, 0x47, 0x00, 0x4E, 0x00, 0x53, 0x00, 0x20, 0x00, 0x43, 0x00, 0x45, 0x00, 0x4E, 0x00, 0x53, 0x00, 0x4F, 0x00, 0x52, 0x00, 0x20, 0x00, 0x45, 0x00, 0x52, 0x00, 0x52, 0x00, 0x4F, 0x00, 0x52, 0x00 };
        
        public Patcher() : this("", null)
        { }

        public Patcher(string GameFolderPath, GameConfig Config)
        {
            InitializeComponent();

            Cbox_Crosshair.SelectedIndex = 0;
            Cbox_ErroMsg.SelectedIndex = 0;

            _GameFolderPath = GameFolderPath;

            string TmpFolderPath = Path.GetTempPath();
        }

        private void Btn_Patch_Click(object sender, EventArgs e)
        {
            string DataFilePath = Path.Combine(_GameFolderPath, "dev_hdd0", "game", "SCEEXE000", "USRDIR", "BootPack", "Data.npk");
            if (File.Exists(DataFilePath))
            {
                try
                {                    
                    using (BinaryWriter bw = new BinaryWriter(File.Open(DataFilePath, FileMode.Open, FileAccess.ReadWrite)))
                    {
                        if (Cbox_ErroMsg.SelectedIndex == 0)
                        {
                            bw.BaseStream.Seek(0x2E202716, SeekOrigin.Begin);
                            bw.Write(SensorMessageOff_Data);

                            bw.BaseStream.Seek(0x2E202C58, SeekOrigin.Begin);
                            bw.Write(SensorMessageOff_Data);
                        }
                        else
                        {
                            bw.BaseStream.Seek(0x2E202716, SeekOrigin.Begin);
                            bw.Write(SensorMessage1_Data);

                            bw.BaseStream.Seek(0x2E202C58, SeekOrigin.Begin);
                            bw.Write(SensorMessage2_Data);
                        }

                        if (Cbox_Crosshair.SelectedIndex == 0)
                        {
                            bw.BaseStream.Seek(0x2E1C3000, SeekOrigin.Begin);
                            bw.Write(Properties.Resources.texture_sight_off);
                        }
                        else
                        {
                            bw.BaseStream.Seek(0x2E1C3000, SeekOrigin.Begin);
                            bw.Write(Properties.Resources.texture_sight_on);
                        }
                    }
                }
                catch (Exception Ex)
                {
                    MessageBox.Show("Error patching file: " + DataFilePath + "\n\n" + Ex.Message.ToString(), this.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
                }

                MessageBox.Show("Patch complete !", this.Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
                DialogResult = DialogResult.OK;
            }
            else
            {
                MessageBox.Show("File not found: " + DataFilePath, this.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void Btn_Close_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
        }
    }
}
