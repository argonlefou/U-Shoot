using System;
using System.IO;
using System.Windows.Forms;
using UsCore.Config;

namespace U_Shoot.Tools.MissionImpossible
{
    public partial class TestMenu : Form
    {
        private const string GAME_SETTINGS_PATH = @"NVRAM\GameSettings.ini";
        private const string SHELL_SETTINGS_PATH = @"NVRAM\ShellData.ini";

        private string _Language = "ENGLISH";
        private string _GameDifficulty = "NORMAL";
        private string _Revival = "ON";
        private string _P1_Recoil = "ON";
        private string _P2_Recoil = "ON";
        private string _ContinueCountdown = "10";
        private string _LobbyCountDown = "20";
        private string _DamageEffects = "RED";
        private string _EnemyBoost = "OFF";
        private string _MinGameplay = "120s";
        private string _KidsMode = "OFF";
        private string _SelectMission = "ON";
        private string _Subtitles = "ON";
        private string _SwipeCard = "OFF";

        private int _Freeplay = 0;
        private string _CreditsToStart = "1";
        private string _CreditstoContinue = "1";
        private int _AttractSound = 0;

        private string _GameFolderPath;

        public TestMenu() : this("", null)
        { }

        public TestMenu(string GameFolderPath, GameConfig Config)
        {
            InitializeComponent();
            _GameFolderPath = GameFolderPath;

            this.Text = "Mission Impossible Arcade - System Menu";
        }

        private void TestMenu_Load(object sender, EventArgs e)
        {
            string GameSettingPath = Path.Combine(_GameFolderPath, GAME_SETTINGS_PATH);
            if (File.Exists(GameSettingPath))
            {
                try
                {
                    INIFile Game_IniFile = new INIFile(GameSettingPath);
                    _Language = Game_IniFile.IniReadValue("GameSettings", "LANGUAGE");
                    _GameDifficulty = Game_IniFile.IniReadValue("GameSettings", "GAME DIFFICULTY");
                    _Revival = Game_IniFile.IniReadValue("GameSettings", "REVIVAL");
                    _P1_Recoil = Game_IniFile.IniReadValue("GameSettings", "PLAYER1 CONTROLLER REACTION");
                    _P2_Recoil = Game_IniFile.IniReadValue("GameSettings", "PLAYER2 CONTROLLER REACTION");
                    _ContinueCountdown = Game_IniFile.IniReadValue("GameSettings", "CONTINUE COUNTDOWN");
                    _LobbyCountDown = Game_IniFile.IniReadValue("GameSettings", "LOBBY COUNTDOWN");
                    _DamageEffects = Game_IniFile.IniReadValue("GameSettings", "DAMAGE EFFECTS");
                    _EnemyBoost = Game_IniFile.IniReadValue("GameSettings", "ENEMY BOOST");
                    _MinGameplay = Game_IniFile.IniReadValue("GameSettings", "1ST MIN GAME PLAY");
                    _KidsMode = Game_IniFile.IniReadValue("GameSettings", "KIDS MODE");
                    _SelectMission = Game_IniFile.IniReadValue("GameSettings", "SELECT MISSION");
                    _Subtitles = Game_IniFile.IniReadValue("GameSettings", "SUBTITLE");
                    _SwipeCard = Game_IniFile.IniReadValue("GameSettings", "SWIPE CARD TO PLAY");
                }
                catch (Exception Ex)
                {
                    MessageBox.Show("Error reading game config file " + GameSettingPath + " :\n\n" + Ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }

            string ShellSettingPath = Path.Combine(_GameFolderPath, SHELL_SETTINGS_PATH);
            if (File.Exists(ShellSettingPath))
            {
                try
                {
                    INIFile Shell_IniFile = new INIFile(ShellSettingPath);
                    _Freeplay = Int32.Parse(Shell_IniFile.IniReadValue("Credit", "Freeplay"));
                    _CreditsToStart = Shell_IniFile.IniReadValue("Credit", "GameCost");
                    _CreditstoContinue = Shell_IniFile.IniReadValue("Credit", "ContinueCost");
                    _AttractSound = Int32.Parse(Shell_IniFile.IniReadValue("Audio", "AttractAudio"));
                }
                catch (Exception Ex)
                {
                    MessageBox.Show("Error reading shell config file " + ShellSettingPath + " :\n\n" + Ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }

            UpdateDisplay();
        }

        private void UpdateDisplay()
        {
            Cbox_FreePlay.SelectedIndex = _Freeplay;
            Cbox_CreditStart.Text = _CreditsToStart;
            Cbox_CreditContinue.Text = _CreditstoContinue;
            Cbox_AttractSound.SelectedIndex = _AttractSound;

            Cbox_Language.Text = _Language;
            Cbox_Difficulty.Text = _GameDifficulty;
            Cbox_Revival.Text = _Revival;
            Cbox_P1Recoil.Text = _P1_Recoil;
            Cbox_P2Recoil.Text = _P2_Recoil;
            Cbox_ContinueCountdown.Text = _ContinueCountdown;
            Cbox_LobbyCountDown.Text = _LobbyCountDown;
            Cbox_DamageEffects.Text = _DamageEffects;
            Cbox_EnemyBoost.Text = _EnemyBoost;
            Cbox_MinGameplay.Text = _MinGameplay;
            Cbox_KidsMode.Text = _KidsMode;
            Cbox_MissionSelect.Text = _SelectMission;
            Cbox_Subtitles.Text = _Subtitles;
            Cbox_Swipe.Text = _SwipeCard;
        }

        private void Btn_Close_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void Btn_Save_Click(object sender, EventArgs e)
        {
            //ShellData
            string ShellSettingPath = Path.Combine(_GameFolderPath, SHELL_SETTINGS_PATH);
            try
            {
                INIFile Shell_IniFile = new INIFile(ShellSettingPath);
                Shell_IniFile.IniWriteValue("Credit", "Freeplay", Cbox_FreePlay.SelectedIndex.ToString());
                Shell_IniFile.IniWriteValue("Credit", "GameCost", Cbox_CreditStart.Text);
                Shell_IniFile.IniWriteValue("Credit", "ContinueCost", Cbox_CreditContinue.Text);
                Shell_IniFile.IniWriteValue("Audio", "AttractAudio", Cbox_AttractSound.SelectedIndex.ToString());
            }
            catch (Exception Ex)
            {
                MessageBox.Show("Error writing shell config file " + ShellSettingPath + " :\n\n" + Ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            string GameSettingPath = Path.Combine(_GameFolderPath, GAME_SETTINGS_PATH);                
            try
            {
                INIFile Game_IniFile = new INIFile(GameSettingPath);
                Game_IniFile.IniWriteValue("GameSettings", "LANGUAGE", Cbox_Language.Text);
                Game_IniFile.IniWriteValue("GameSettings", "GAME DIFFICULTY", Cbox_Difficulty.Text);
                Game_IniFile.IniWriteValue("GameSettings", "REVIVAL", Cbox_Revival.Text);
                Game_IniFile.IniWriteValue("GameSettings", "PLAYER1 CONTROLLER REACTION", Cbox_P1Recoil.Text);
                Game_IniFile.IniWriteValue("GameSettings", "PLAYER2 CONTROLLER REACTION", Cbox_P2Recoil.Text);
                Game_IniFile.IniWriteValue("GameSettings", "CONTINUE COUNTDOWN", Cbox_ContinueCountdown.Text);
                Game_IniFile.IniWriteValue("GameSettings", "LOBBY COUNTDOWN", Cbox_LobbyCountDown.Text);
                Game_IniFile.IniWriteValue("GameSettings", "DAMAGE EFFECTS", Cbox_DamageEffects.Text);
                Game_IniFile.IniWriteValue("GameSettings", "ENEMY BOOST", Cbox_EnemyBoost.Text);
                Game_IniFile.IniWriteValue("GameSettings", "1ST MIN GAME PLAY", Cbox_MinGameplay.Text);
                Game_IniFile.IniWriteValue("GameSettings", "KIDS MODE", Cbox_KidsMode.Text);
                Game_IniFile.IniWriteValue("GameSettings", "SELECT MISSION", Cbox_MissionSelect.Text);
                Game_IniFile.IniWriteValue("GameSettings", "SUBTITLE", Cbox_Subtitles.Text);
                Game_IniFile.IniWriteValue("GameSettings", "SWIPE CARD TO PLAY", Cbox_Swipe.Text);
            }
            catch (Exception Ex)
            {
                MessageBox.Show("Error writing game config file " + GameSettingPath + " :\n\n" + Ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            MessageBox.Show("Save complete !", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}
