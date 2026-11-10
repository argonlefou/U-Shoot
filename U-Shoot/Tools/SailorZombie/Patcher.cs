using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using UsCore.Config;

namespace U_Shoot.Tools.SailorZombie
{
    public partial class Patcher : Form
    {
        private string _GameFolderPath = string.Empty;

        public Patcher() : this("", null)
        { }

        public Patcher(string GameFolderPath, GameConfig Config)
        {
            InitializeComponent();

            Cbox_Crosshair.SelectedIndex = 0;
            _GameFolderPath = GameFolderPath;

            string TmpFolderPath = Path.GetTempPath();
        }

        private void Btn_Patch_Click(object sender, EventArgs e)
        {
            string DataFilePath = Path.Combine(_GameFolderPath, "dev_hdd0", "game", "SCEEXE000", "USRDIR", "BootRaw", "Common", "2D", "texture_2D.ntp");
            if (File.Exists(DataFilePath))
            {
                try
                {
                    using (BinaryWriter bw = new BinaryWriter(File.Open(DataFilePath, FileMode.Open, FileAccess.ReadWrite)))
                    {
                        if (Cbox_Crosshair.SelectedIndex == 0)
                        {
                            bw.BaseStream.Seek(0, SeekOrigin.Begin);
                            bw.Write(Properties.Resources.texture_2D_off);
                        }
                        else
                        {
                            bw.BaseStream.Seek(0, SeekOrigin.Begin);
                            bw.Write(Properties.Resources.texture_2D_on);
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
