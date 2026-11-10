using System;
using System.IO;
using System.Windows.Forms;
using UsCore.Config;

namespace U_Shoot.Tools.MIB
{
    public partial class TestMenu : Form
    {
        private const string GAMESETTINGS_INI_PATH = @"ShellData\GameSettings.ini";
        private const string SHELLDATA_INI_PATH = @"ShellData\ShellData.ini";
        private FileInfo _fInfo_GameSettings;
        private FileInfo _fInfo_ShellData;
        private INIFile _GameSettings_IniFile;
        private INIFile _ShellData_IniFile;

        private string _GameFolderPath;

        public TestMenu() : this("", null)
        { }

        public TestMenu(string GameFolderPath, GameConfig Config)
        {
            InitializeComponent();
            _GameFolderPath = GameFolderPath;

            this.Text = "Men In Black - System Menu";

            //Populating Tickets
            for (int i = 2; i <= 10; i++)
            {
                Cbox_MaxTickets.Items.Add(i);
            }
            for (int i = 15; i <= 100; i += 5)
            {
                Cbox_MaxTickets.Items.Add(i);
            }
            for (int i = 125; i <= 200; i += 25)
            {
                Cbox_MaxTickets.Items.Add(i);
            }
            Cbox_MaxTickets.Items.Add(250);


            //Populating Super bonus
            Cbox_SuperBonus.Items.Add(5);
            Cbox_SuperBonus.Items.Add(10);
            for (int i = 25; i <= 200; i += 25)
            {
                Cbox_SuperBonus.Items.Add(i);
            }
            for (int i = 200; i <= 1000; i += 50)
            {
                Cbox_SuperBonus.Items.Add(i);
            }
            for (int i = 1250; i <= 3000; i += 250)
            {
                Cbox_SuperBonus.Items.Add(i);
            }
            Cbox_SuperBonus.Items.Add(4000);
            Cbox_SuperBonus.Items.Add(5000);

            //Populating Merci
            for (int i = 0; i <= 10; i++)
            {
                Cbox_MerciTickets.Items.Add(i);
            }

            //Populationg Volume
            for (int i = 0; i <= 10; i++)
            {
                Cbox_Music.Items.Add(i);
                Cbox_Sfx.Items.Add(i);
                Cbox_Voice.Items.Add(i);
                Cbox_AttractVolume.Items.Add(i);
            }

            string GameSettingPath = Path.Combine(_GameFolderPath, GAMESETTINGS_INI_PATH);
            string ShellSettingPath = Path.Combine(_GameFolderPath, SHELLDATA_INI_PATH);

            _fInfo_GameSettings = new FileInfo(GameSettingPath);
            _fInfo_ShellData = new FileInfo(ShellSettingPath);

            _GameSettings_IniFile = new INIFile(GameSettingPath);
            _ShellData_IniFile = new INIFile(ShellSettingPath);
        }

        private void TestMenu_Load(object sender, EventArgs e)
        {
            DisplayDefaultValues();

            if (File.Exists(_fInfo_GameSettings.FullName))
            {
                DisplayGameSettings();
            }
            else
            {
                MessageBox.Show("TEST MENU config file not found :\n\n" + _fInfo_GameSettings.FullName, this.Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }

            if (File.Exists(_fInfo_ShellData.FullName))
            {
                DisplayShellSettings();
            }
            else
            {
                MessageBox.Show("SHELL config file not found :\n\n" + _fInfo_ShellData.FullName, this.Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void DisplayDefaultValues()
        {
            DisplayComboBoxValue(Cbox_Difficulty, Cbox_Difficulty.Items[2].ToString());
            DisplayComboBoxValue(Cbox_MaxTickets, Cbox_MaxTickets.Items[0].ToString());
            DisplayComboBoxValue(Cbox_SuperBonus, Cbox_SuperBonus.Items[0].ToString());
            DisplayComboBoxValue(Cbox_MerciTickets, Cbox_MerciTickets.Items[0].ToString());
            DisplayComboBoxValue(Cbox_P1Recoil, Cbox_P1Recoil.Items[1].ToString());
            DisplayComboBoxValue(Cbox_P2Recoil, Cbox_P2Recoil.Items[1].ToString());
            DisplayComboBoxValue(Cbox_Swipe, Cbox_Swipe.Items[0].ToString());
            DisplayComboBoxValue(Cbox_ETicket, Cbox_ETicket.Items[0].ToString());
            DisplayComboBoxValue(Cbox_Covid, Cbox_Covid.Items[0].ToString());
            DisplayComboBoxValue(Cbox_Language, Cbox_Language.Items[0].ToString());
            DisplayComboBoxValue(Cbox_AdaptiveDifficulty, Cbox_AdaptiveDifficulty.Items[0].ToString());
            DisplayComboBoxValue(Cbox_GooPools, Cbox_GooPools.Items[0].ToString());
            DisplayComboBoxValue(Cbox_ShortIntro, Cbox_ShortIntro.Items[1].ToString());

            Cbox_FreePlay.Text = Cbox_FreePlay.Items[0].ToString();
            Cbox_EntryType.Text = Cbox_EntryType.Items[0].ToString();
            Cbox_GameCost.Text = Cbox_GameCost.Items[0].ToString();
            Cbox_ContinueCost.Text = Cbox_ContinueCost.Items[0].ToString();
            Cbox_2CoinMech.Text = Cbox_2CoinMech.Items[0].ToString();
            Cbox_StartCoinIn.Text = Cbox_2CoinMech.Items[0].ToString();

            DisplayComboBoxValue(Cbox_Music, Cbox_Music.Items[0].ToString());
            DisplayComboBoxValue(Cbox_Sfx, Cbox_Sfx.Items[0].ToString());
            DisplayComboBoxValue(Cbox_Voice, Cbox_Voice.Items[0].ToString());
            DisplayComboBoxValue(Cbox_AttractVolume, Cbox_AttractVolume.Items[0].ToString());
        }

        private void DisplayGameSettings()
        {
            try
            {
                DisplayComboBoxValue(Cbox_Difficulty, _GameSettings_IniFile.IniReadValue("GameSettings", "GAME DIFFICULTY"));
                DisplayComboBoxValue(Cbox_MaxTickets, _GameSettings_IniFile.IniReadValue("GameSettings", "MAX TICKETS"));
                DisplayComboBoxValue(Cbox_SuperBonus, _GameSettings_IniFile.IniReadValue("GameSettings", "SUPER BONUS"));
                DisplayComboBoxValue(Cbox_MerciTickets, _GameSettings_IniFile.IniReadValue("GameSettings", "MERCY TICKETS"));
                DisplayComboBoxValue(Cbox_P1Recoil, _GameSettings_IniFile.IniReadValue("GameSettings", "PLAYER1 CONTROLLER REACTION"));
                DisplayComboBoxValue(Cbox_P2Recoil, _GameSettings_IniFile.IniReadValue("GameSettings", "PLAYER2 CONTROLLER REACTION"));
                DisplayComboBoxValue(Cbox_Swipe, _GameSettings_IniFile.IniReadValue("GameSettings", "SWIPE CARD TO PLAY"));
                DisplayComboBoxValue(Cbox_ETicket, _GameSettings_IniFile.IniReadValue("GameSettings", "ETICKETS"));
                DisplayComboBoxValue(Cbox_Covid, _GameSettings_IniFile.IniReadValue("GameSettings", "SHOW COVID MESSAGE"));
                DisplayComboBoxValue(Cbox_Language, _GameSettings_IniFile.IniReadValue("GameSettings", "LANGUAGE"));
                DisplayComboBoxValue(Cbox_AdaptiveDifficulty, _GameSettings_IniFile.IniReadValue("GameSettings", "ADAPTIVE DIFFICULTY"));
                DisplayComboBoxValue(Cbox_GooPools, _GameSettings_IniFile.IniReadValue("GameSettings", "GOO POOLS"));
                DisplayComboBoxValue(Cbox_ShortIntro, _GameSettings_IniFile.IniReadValue("GameSettings", "SHORT INTRODUCTION"));
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

                iIndex = int.Parse(_ShellData_IniFile.IniReadValue("Credit", "EntryType"));
                Cbox_EntryType.Text = Cbox_FreePlay.Items[iIndex].ToString();

                iIndex = int.Parse(_ShellData_IniFile.IniReadValue("Credit", "TwoCoinMech"));
                Cbox_2CoinMech.Text = Cbox_FreePlay.Items[iIndex].ToString();

                iIndex = int.Parse(_ShellData_IniFile.IniReadValue("Credit", "StartOnCoinIn"));
                Cbox_StartCoinIn.Text = Cbox_FreePlay.Items[iIndex].ToString();


                //DisplayComboBoxValue(Cbox_FreePlay, _ShellData_IniFile.IniReadValue("Credit", "Freeplay"));
                //DisplayComboBoxValue(Cbox_EntryType, _ShellData_IniFile.IniReadValue("Credit", "EntryType"));
                DisplayComboBoxValue(Cbox_GameCost, _ShellData_IniFile.IniReadValue("Credit", "GameCost"));
                DisplayComboBoxValue(Cbox_ContinueCost, _ShellData_IniFile.IniReadValue("Credit", "ContinueCost"));
                //DisplayComboBoxValue(Cbox_2CoinMech, _ShellData_IniFile.IniReadValue("Credit", "TwoCoinMech"));
                //DisplayComboBoxValue(Cbox_StartCoinIn, _ShellData_IniFile.IniReadValue("Credit", "StartOnCoinIn"));
                DisplayComboBoxValue(Cbox_Music, _ShellData_IniFile.IniReadValue("Audio", "MusicVolume"));
                DisplayComboBoxValue(Cbox_Sfx, _ShellData_IniFile.IniReadValue("Audio", "SFXVolume"));
                DisplayComboBoxValue(Cbox_Voice, _ShellData_IniFile.IniReadValue("Audio", "VoiceVolume"));
                DisplayComboBoxValue(Cbox_AttractVolume, _ShellData_IniFile.IniReadValue("Audio", "AttractMusic"));
            }
            catch (Exception Ex)
            {
                MessageBox.Show(_fInfo_ShellData.FullName + "\n: Invalid value found for Freeplay : \n\n" + Ex.Message.ToString(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            try
            {
                int iIndex = int.Parse(_ShellData_IniFile.IniReadValue("Credit", "EntryType"));
                Cbox_EntryType.Text = Cbox_EntryType.Items[iIndex].ToString();
            }
            catch (Exception Ex)
            {
                MessageBox.Show(_fInfo_ShellData.FullName + "\n: Invalid value found for EntryType: \n\n" + Ex.Message.ToString(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
            MessageBox.Show(_fInfo_GameSettings.FullName + "\n: Invalid value found for " + Target.Name + " : \n\n" + sValue, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private void SaveGameSettings()
        {
            if (!_fInfo_GameSettings.Exists)
                Directory.CreateDirectory(_fInfo_GameSettings.Directory.FullName);
            try
            {
                _GameSettings_IniFile.IniWriteValue("GameSettings", "GAME DIFFICULTY", Cbox_Difficulty.Text);
                _GameSettings_IniFile.IniWriteValue("GameSettings", "MAX TICKETS", Cbox_MaxTickets.Text);
                _GameSettings_IniFile.IniWriteValue("GameSettings", "SUPER BONUS", Cbox_SuperBonus.Text);
                _GameSettings_IniFile.IniWriteValue("GameSettings", "MERCY TICKETS", Cbox_MerciTickets.Text);
                _GameSettings_IniFile.IniWriteValue("GameSettings", "PLAYER1 CONTROLLER REACTION", Cbox_P1Recoil.Text);
                _GameSettings_IniFile.IniWriteValue("GameSettings", "PLAYER2 CONTROLLER REACTION", Cbox_P2Recoil.Text);
                _GameSettings_IniFile.IniWriteValue("GameSettings", "SWIPE CARD TO PLAY", Cbox_Swipe.Text);
                _GameSettings_IniFile.IniWriteValue("GameSettings", "ETICKETS", Cbox_ETicket.Text);
                _GameSettings_IniFile.IniWriteValue("GameSettings", "SHOW COVID MESSAGE", Cbox_Covid.Text);
                _GameSettings_IniFile.IniWriteValue("GameSettings", "LANGUAGE", Cbox_Language.Text);
                _GameSettings_IniFile.IniWriteValue("GameSettings", "ADAPTIVE DIFFICULTY", Cbox_AdaptiveDifficulty.Text);
                _GameSettings_IniFile.IniWriteValue("GameSettings", "GOO POOLS", Cbox_GooPools.Text);
                _GameSettings_IniFile.IniWriteValue("GameSettings", "SHORT INTRODUCTION", Cbox_ShortIntro.Text);
            }
            catch (Exception Ex)
            {
                MessageBox.Show("Error saving GameSettings to disk : " + _fInfo_GameSettings.FullName + "\n\n" + Ex.Message.ToString(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            MessageBox.Show("GameSettings successfully saved to : \n\n" + _fInfo_GameSettings.FullName, this.Text, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
        }

        private void SaveShellSettings()
        {
            if (!_fInfo_ShellData.Exists)
                Directory.CreateDirectory(_fInfo_ShellData.Directory.FullName);

            try
            {
                _ShellData_IniFile.IniWriteValue("Credit", "Freeplay", Cbox_FreePlay.SelectedIndex.ToString());
                _ShellData_IniFile.IniWriteValue("Credit", "EntryType", Cbox_EntryType.SelectedIndex.ToString());
                _ShellData_IniFile.IniWriteValue("Credit", "GameCost", Cbox_GameCost.Text);
                _ShellData_IniFile.IniWriteValue("Credit", "ContinueCost", Cbox_ContinueCost.Text);
                _ShellData_IniFile.IniWriteValue("Credit", "TwoCoinMech", Cbox_2CoinMech.SelectedIndex.ToString());
                _ShellData_IniFile.IniWriteValue("Credit", "StartOnCoinIn", Cbox_StartCoinIn.SelectedIndex.ToString());
                _ShellData_IniFile.IniWriteValue("Audio", "MusicVolume", Cbox_Music.SelectedIndex.ToString());
                _ShellData_IniFile.IniWriteValue("Audio", "SFXVolume", Cbox_Sfx.SelectedIndex.ToString());
                _ShellData_IniFile.IniWriteValue("Audio", "VoiceVolume", Cbox_Voice.SelectedIndex.ToString());
                _ShellData_IniFile.IniWriteValue("Audio", "AttractMusic", Cbox_AttractVolume.SelectedIndex.ToString());
            }
            catch (Exception Ex)
            {
                MessageBox.Show("Error saving ShellSettings to disk : " + _fInfo_ShellData.FullName + "\n\n" + Ex.Message.ToString(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            MessageBox.Show("ShellData successfully saved to : \n\n" + _fInfo_ShellData.FullName, this.Text, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
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
