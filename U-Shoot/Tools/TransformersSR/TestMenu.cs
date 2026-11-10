using System;
using System.IO;
using System.Windows.Forms;
using UsCore.Config;

namespace U_Shoot.Tools.TransformersSR
{
    public partial class TestMenu : Form
    {
        private const string GAMESETTINGS_INI_PATH = @"..\ShellData\GameSettings.ini";
        private const string SHELLDATA_INI_PATH = @"..\ShellData\ShellData.ini";
        private const string LAUNCHER_INI_PATH = @".\Transformers2_Launcher.ini";
        private INIFile _GameSettings_IniFile;
        private INIFile _ShellData_IniFile;

        private string _GameFolderPath;

        public TestMenu() : this("", null)
        { }

        public TestMenu(string GameFolderPath, GameConfig Config)
        {
            InitializeComponent();
            _GameFolderPath = GameFolderPath;

            this.Text = "Transformers Shadow Rising - System Menu";

            _GameSettings_IniFile = new INIFile(Path.Combine(_GameFolderPath, GAMESETTINGS_INI_PATH));
            _ShellData_IniFile = new INIFile(Path.Combine(_GameFolderPath, SHELLDATA_INI_PATH));
        }

        private void TestMenu_Load(object sender, EventArgs e)
        {
            DisplayDefaultValues();

            if (File.Exists(_GameSettings_IniFile.FInfo.FullName))
            {
                DisplayGameSettings();
            }
            else
            {
                MessageBox.Show("TEST MENU config file not found :\n\n" + _GameSettings_IniFile.FInfo.FullName, this.Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }

            if (File.Exists(_ShellData_IniFile.FInfo.FullName))
            {
                DisplayShellSettings();
            }
            else
            {
                MessageBox.Show("SHELL config file not found :\n\n" + _ShellData_IniFile.FInfo.FullName, this.Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void DisplayDefaultValues()
        {
            DisplayComboBoxValue(Cbox_Language, Cbox_Language.Items[0].ToString());
            DisplayComboBoxValue(Cbox_Difficulty, Cbox_Difficulty.Items[2].ToString());
            DisplayComboBoxValue(Cbox_Advertise, Cbox_Advertise.Items[0].ToString());
            DisplayComboBoxValue(Cbox_Revival, Cbox_Revival.Items[0].ToString());
            DisplayComboBoxValue(Cbox_P1Recoil, Cbox_P1Recoil.Items[1].ToString());
            DisplayComboBoxValue(Cbox_P2Recoil, Cbox_P2Recoil.Items[1].ToString());
            DisplayComboBoxValue(Cbox_ContinueCountdown, Cbox_ContinueCountdown.Items[1].ToString());
            DisplayComboBoxValue(Cbox_EnnemyBoost, Cbox_EnnemyBoost.Items[0].ToString());
            DisplayComboBoxValue(Cbox_FirstMn, Cbox_FirstMn.Items[0].ToString());
            DisplayComboBoxValue(Cbox_KidsMode, Cbox_KidsMode.Items[0].ToString());
            DisplayComboBoxValue(Cbox_StageSelect, Cbox_StageSelect.Items[1].ToString());
            DisplayComboBoxValue(Cbox_EnglishSubtitles, Cbox_EnglishSubtitles.Items[1].ToString());
            DisplayComboBoxValue(Cbox_Swipe, Cbox_Swipe.Items[0].ToString());
            Cbox_FreePlay.Text = Cbox_FreePlay.Items[1].ToString();
            Cbox_EntryType.Text = Cbox_EntryType.Items[0].ToString();
        }

        private void DisplayGameSettings()
        {
            try
            {
                DisplayComboBoxValue(Cbox_Language, _GameSettings_IniFile.IniReadValue("GameSettings", "LANGUAGE"));
                DisplayComboBoxValue(Cbox_Difficulty, _GameSettings_IniFile.IniReadValue("GameSettings", "GAME DIFFICULTY"));
                DisplayComboBoxValue(Cbox_Advertise, _GameSettings_IniFile.IniReadValue("GameSettings", "ADVERTISE SOUND"));
                DisplayComboBoxValue(Cbox_Revival, _GameSettings_IniFile.IniReadValue("GameSettings", "REVIVAL"));
                DisplayComboBoxValue(Cbox_P1Recoil, _GameSettings_IniFile.IniReadValue("GameSettings", "PLAYER1 CONTROLLER REACTION"));
                DisplayComboBoxValue(Cbox_P2Recoil, _GameSettings_IniFile.IniReadValue("GameSettings", "PLAYER2 CONTROLLER REACTION"));
                DisplayComboBoxValue(Cbox_ContinueCountdown, _GameSettings_IniFile.IniReadValue("GameSettings", "CONTINUE COUNTDOWN"));
                DisplayComboBoxValue(Cbox_EnnemyBoost, _GameSettings_IniFile.IniReadValue("GameSettings", "ENEMY BOOST"));
                DisplayComboBoxValue(Cbox_FirstMn, _GameSettings_IniFile.IniReadValue("GameSettings", "1ST MIN GAME PLAY"));
                DisplayComboBoxValue(Cbox_KidsMode, _GameSettings_IniFile.IniReadValue("GameSettings", "KIDS MODE"));
                DisplayComboBoxValue(Cbox_StageSelect, _GameSettings_IniFile.IniReadValue("GameSettings", "SELECT STAGE"));
                DisplayComboBoxValue(Cbox_EnglishSubtitles, _GameSettings_IniFile.IniReadValue("GameSettings", "ENGLISH SUBTITLES"));
                DisplayComboBoxValue(Cbox_Swipe, _GameSettings_IniFile.IniReadValue("GameSettings", "SWIPE CARD TO PLAY"));
            }
            catch (Exception Ex)
            {
                MessageBox.Show("GameSettings : Invalid value found: \n\n" + Ex.Message.ToString(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void DisplayShellSettings()
        {
            try
            {
                int iIndex = int.Parse(_ShellData_IniFile.IniReadValue("Credit", "Freeplay"));
                Cbox_FreePlay.Text = Cbox_FreePlay.Items[iIndex].ToString();
            }
            catch (Exception Ex)
            {
                MessageBox.Show(_ShellData_IniFile.FInfo.FullName + "\n: Invalid value found for Freeplay : \n\n" + Ex.Message.ToString(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            try
            {
                int iIndex = int.Parse(_ShellData_IniFile.IniReadValue("Credit", "EntryType"));
                Cbox_EntryType.Text = Cbox_EntryType.Items[iIndex].ToString();
            }
            catch (Exception Ex)
            {
                MessageBox.Show(_ShellData_IniFile.FInfo.FullName + "\n: Invalid value found for EntryType: \n\n" + Ex.Message.ToString(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void DisplayComboBoxValue(ComboBox Target, String sValue)
        {
            for (int i = 0; i < Target.Items.Count; i++)
            {
                if (Target.Items[i].ToString().Equals(sValue))
                {
                    Target.Text = sValue;
                    return;
                }
            }
            MessageBox.Show(_GameSettings_IniFile.FInfo.FullName + "\n: Invalid value found for " + Target.Name + " : \n\n" + sValue, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
       
        private void SaveGameSettings()
        {
            if (!_GameSettings_IniFile.FInfo.Exists)
                Directory.CreateDirectory(_GameSettings_IniFile.FInfo.Directory.FullName);

            try
            {
                _GameSettings_IniFile.IniWriteValue("GameSettings", "LANGUAGE", Cbox_Language.Text);
                _GameSettings_IniFile.IniWriteValue("GameSettings", "GAME DIFFICULTY", Cbox_Difficulty.Text);
                _GameSettings_IniFile.IniWriteValue("GameSettings", "ADVERTISE SOUND", Cbox_Advertise.Text);
                _GameSettings_IniFile.IniWriteValue("GameSettings", "REVIVAL", Cbox_Revival.Text);
                _GameSettings_IniFile.IniWriteValue("GameSettings", "PLAYER1 CONTROLLER REACTION", Cbox_P1Recoil.Text);
                _GameSettings_IniFile.IniWriteValue("GameSettings", "PLAYER2 CONTROLLER REACTION", Cbox_P2Recoil.Text);
                _GameSettings_IniFile.IniWriteValue("GameSettings", "CONTINUE COUNTDOWN", Cbox_ContinueCountdown.Text);
                _GameSettings_IniFile.IniWriteValue("GameSettings", "ENEMY BOOST", Cbox_EnnemyBoost.Text);
                _GameSettings_IniFile.IniWriteValue("GameSettings", "1ST MIN GAME PLAY", Cbox_FirstMn.Text);
                _GameSettings_IniFile.IniWriteValue("GameSettings", "KIDS MODE", Cbox_KidsMode.Text);
                _GameSettings_IniFile.IniWriteValue("GameSettings", "SELECT STAGE", Cbox_StageSelect.Text);
                _GameSettings_IniFile.IniWriteValue("GameSettings", "ENGLISH SUBTITLES", Cbox_EnglishSubtitles.Text);
                _GameSettings_IniFile.IniWriteValue("GameSettings", "SWIPE CARD TO PLAY", Cbox_Swipe.Text);
            }
            catch (Exception Ex)
            {
                MessageBox.Show("Error saving GameSettings to disk : " + _GameSettings_IniFile.FInfo.FullName + "\n\n" + Ex.Message.ToString(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            MessageBox.Show("GameSettings successfully saved to : \n\n" + _GameSettings_IniFile.FInfo.FullName, this.Text, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
        }

        private void SaveShellSettings()
        {
            if (!_ShellData_IniFile.FInfo.Exists)
                Directory.CreateDirectory(_ShellData_IniFile.FInfo.Directory.FullName);

            try
            {
                _ShellData_IniFile.IniWriteValue("Credit", "Freeplay", Cbox_FreePlay.SelectedIndex.ToString());
                _ShellData_IniFile.IniWriteValue("Credit", "EntryType", Cbox_EntryType.SelectedIndex.ToString());
            }
            catch (Exception Ex)
            {
                MessageBox.Show("Error saving ShellSettings to disk : " + _ShellData_IniFile.FInfo.FullName + "\n\n" + Ex.Message.ToString(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            MessageBox.Show("ShellData successfully saved to : \n\n" + _ShellData_IniFile.FInfo.FullName, this.Text, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
        }

        private void Btn_Save_Click(object sender, EventArgs e)
        {
            SaveGameSettings();
            SaveShellSettings();
        }

        private void Btn_Close_Click(object sender, EventArgs e)
        {
            Close();
        }
    }
}
