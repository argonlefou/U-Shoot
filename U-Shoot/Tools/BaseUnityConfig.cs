using System;
using System.IO;
using System.Windows.Forms;
using UsCore.Config;

namespace U_Shoot.Tools
{
    public partial class BaseUnityConfig : Form
    {
        protected string _GameFolderPath = string.Empty;
        protected string _LogFilePath = string.Empty;

        protected INIFile _Plugin_IniFile;

        protected GameConfig _Config;

        //Most Common Options
        protected bool _ForceResolution = false;
        protected int _ResWidth = 1920;
        protected int _ResHeight = 1080;
        protected bool _FullScreen = false;
        protected bool _LocalSave = true;

        public BaseUnityConfig() : this("", null)
        { }

        public BaseUnityConfig(string GameFolderPath, GameConfig Config)
        {
            InitializeComponent();
            _GameFolderPath = GameFolderPath;
            _Config = Config;

            if (Config != null)
            {
                switch (Config.RomName)
                {
                    case "dcop":
                    case "drk":
                    case "fha2023":
                    case "hwspr2":
                    case "hwwar2":
                    case "mechad":
                    case "onept":
                    case "rha":
                    case "skullos":
                    case "topgun":
                    case "topgun2":
                    case "tra":
                    case "wzombies":
                        {
                            Lbl_LocalSave.Enabled = true;
                            Chk_LocalSave.Enabled = true;
                        }
                        break;
                    default: break;
                }
            }
        }

        protected void BaseUnityConfig_Load(object sender, EventArgs e)
        {
            if (_Config != null)
                this.Text = _Config.Title + " | Plugin Option";
            else
                this.Text = this.GetType().ToString();

            if (_GameFolderPath != string.Empty)
            {
                try
                {
                    _Plugin_IniFile = new INIFile(Path.Combine(_GameFolderPath, @"BepInEx\config\UShoot_Plugin.ini"));

                    if (File.Exists(_Plugin_IniFile.FInfo.FullName))
                    {
                        LoadData();
                    }
                    else
                    {
                        MessageBox.Show(_Plugin_IniFile.FInfo.FullName + " not found", this.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
                        Close();
                    }
                }
                catch (Exception Ex)
                {
                    MessageBox.Show("Error loading " + _Plugin_IniFile.FInfo.FullName + ":\n" + Ex.Message.ToString(), this.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void Txt_Res_Enter(object sender, EventArgs e)
        {
            ((TextBox)sender).Select(0, ((TextBox)sender).TextLength);
        }        

        protected virtual void SaveData()
        {
            _Plugin_IniFile.IniWriteValue("Video", "WIDTH", Txt_ResWidth.Text);
            _Plugin_IniFile.IniWriteValue("Video", "HEIGHT", Txt_ResHeight.Text);

            _Plugin_IniFile.IniWriteValue("Video", "FULLSCREEN", Cbox_FullScreen.Checked ? "1" : "0");
            _Plugin_IniFile.IniWriteValue("Video", "FORCE_RESOLUTION", Cbox_ForceResolution.Checked ? "1" : "0");

            if (Chk_LocalSave.Enabled)
            {
                _Plugin_IniFile.IniWriteValue("System", "LOCAL_SAVE", Chk_LocalSave.Checked ? "1" : "0");
            }
        }

        protected virtual void LoadData()
        {
            _Plugin_IniFile.IniReadIntValue("Video", "WIDTH", ref _ResWidth);
            _Plugin_IniFile.IniReadIntValue("Video", "HEIGHT", ref _ResHeight);

            _Plugin_IniFile.IniReadBoolValue("Video", "FULLSCREEN", ref _FullScreen);
            _Plugin_IniFile.IniReadBoolValue("Video", "FORCE_RESOLUTION", ref _ForceResolution);            

            Cbox_ForceResolution.Checked = _ForceResolution;
            Txt_ResWidth.Text = _ResWidth.ToString();
            Txt_ResHeight.Text = _ResHeight.ToString();
            Cbox_FullScreen.Checked = _FullScreen;

            if (Chk_LocalSave.Enabled)
            {
                _Plugin_IniFile.IniReadBoolValue("System", "LOCAL_SAVE", ref _LocalSave);
                Chk_LocalSave.Checked = _LocalSave;
            }
        }

        private void Btn_Save_Click(object sender, EventArgs e)
        {
            try
            {
                SaveData();
                MessageBox.Show(_Plugin_IniFile.FInfo.FullName + " successfully saved !", this.Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception Ex)
            {
                MessageBox.Show("Error saving " + _Plugin_IniFile.FInfo.FullName + ":\n" + Ex.Message.ToString(), this.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            Close();
        }
    }
}
