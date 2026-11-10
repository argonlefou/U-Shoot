using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using UsCore.Config;
using UsCore.Logging;

namespace U_Shoot.GUI.Controls
{
    public partial class Ctrl_TargetPath : UserControl
    {
        private string _TargetFileName = string.Empty;

        public Ctrl_TargetPath() : this (string.Empty, string.Empty, string.Empty)
        {
        }
        public Ctrl_TargetPath(string TargetFileName, string TargetFilePath, string Category)
        {
            InitializeComponent();
            _TargetFileName = TargetFileName;
            Lbl_Text.Text = "Select '" + _TargetFileName + "' :";
            Txt_Value.Text = TargetFilePath;

            //For some categories, there can be a default path set in ProgramConfig.
            //Displaying it as a readonly text to make it different
            string DefaultPath = string.Empty;
            switch (Category)
            {
                case "Flycast": DefaultPath = ProgramConfig.GetInstance().GetStringOption(Properties.Resources.DEFAULT_FLYCAST_PATH_OPTION); break;
                case "Lindbergh": DefaultPath = ProgramConfig.GetInstance().GetStringOption(Properties.Resources.DEFAULT_LINUXLOADER_PATH_OPTION); break;
                case "System 2X6": DefaultPath = ProgramConfig.GetInstance().GetStringOption(Properties.Resources.DEFAULT_PCSX2X6EMULATOR_PATH_OPTION); break;
                case "System 357": DefaultPath = ProgramConfig.GetInstance().GetStringOption(Properties.Resources.DEFAULT_RPCS3EMULATOR_PATH_OPTION); break;
                default: break;
            }

            if (!string.IsNullOrEmpty(DefaultPath) && string.IsNullOrEmpty(TargetFilePath))
            {
                Txt_Value.Text = DefaultPath;
                Txt_Value.Font = new Font(Txt_Value.Font, Txt_Value.Font.Style | FontStyle.Italic);
                Txt_Value.ReadOnly = true;
            }
        }

        /// <summary>
        /// Disable saving TextBox content when it's displaying some default values read from ProgramConfig
        /// </summary>
        public string GetOptionValue()
        {
            if (!Txt_Value.ReadOnly)
                return Txt_Value.Text;
            else
                return string.Empty;
        }

        private void Btn_Open_Click(object sender, EventArgs e)
        {
            openFileDialog1.Title = "Select '" + _TargetFileName + "' :";
            openFileDialog1.FileName = "";

            string TargetExtension = Path.GetExtension(_TargetFileName);
            if (TargetExtension != string.Empty)
                //openFileDialog1.Filter = _TargetFileName + "|" + _TargetFileName + "|" + TargetExtension + " files (*" + TargetExtension + ")|*" + TargetExtension + "|All files (*.*)|*.*";
                openFileDialog1.Filter = TargetExtension + " files (*" + TargetExtension + ")|*" + TargetExtension + "|All files (*.*)|*.*";
            else
                openFileDialog1.Filter = _TargetFileName + "|" + _TargetFileName + "|All files (*.*)|*.*";

            if (Txt_Value.Text.Length > 0 && Directory.Exists(Path.GetDirectoryName(Txt_Value.Text)))
                openFileDialog1.InitialDirectory = Path.GetDirectoryName(Txt_Value.Text);

            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                Txt_Value.Text = openFileDialog1.FileName;
                Txt_Value.ReadOnly = false;
                Txt_Value.Font = new Font(Txt_Value.Font, Txt_Value.Font.Style & FontStyle.Regular);
            }
        }

        /// <summary>
        /// By default, TextBox has whole text selected and highlighted, setting that to none
        /// </summary>
        private void Ctrl_TargetPath_Load(object sender, EventArgs e)
        {
            Txt_Value.SelectionStart = 0;
            Txt_Value.SelectionLength = 0;
        }

        private void Ctrl_TargetPath_SizeChanged(object sender, EventArgs e)
        {
            Btn_Open.Location = new System.Drawing.Point(this.Width - Btn_Open.Width - 10, Btn_Open.Location.Y);
            Txt_Value.Width = this.Width -Btn_Open.Width - 10 - 10 - 10;    //10px left, 10px right, 10px between TextBox and button
        }
    }
}
