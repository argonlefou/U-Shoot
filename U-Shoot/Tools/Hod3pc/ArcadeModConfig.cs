using System;
using System.IO;
using System.Windows.Forms;
using UsCore.Config;

namespace U_Shoot.Tools.Hod3pc
{
    public partial class ArcadeModConfig : Form
    {
        private const String CONF_FILENAME = "settings.ini";

        private const string OPTION_FILEPATH = "FilePath";
        private const string OPTION_DIFFICULTY = "Difficulty";
        private const string OPTION_INITIAL_LIFE = "InitialLife";
        private const string OPTION_MAX_LIFE = "MaxLife";
        private const string OPTION_BLOOD_COLOR = "BloodColor";
        private const string OPTION_VIOLENCE = "Violence";
        private const string OPTION_ADVERTISE = "AdvertiseSound";
        private const string OPTION_LANGUAGE = "Language";
        private const string OPTION_CREDITS_TO_START = "CreditsToStart";
        private const string OPTION_CREDITS_TO_CONTINUE = "CreditsToContinue";
        private const string OPTION_FREEPLAY = "Freeplay";

        private const string OPTION_ALIASING = "AntiAliasing";
        private const string OPTION_COLORS = "Colors";
        private const string OPTION_TEXTURES = "Textures";
        private const string OPTION_RESOLUTION = "Resolution";
        private const string OPTION_FRAMERATE = "Framerate";
        private const string OPTION_FULLSCREEN = "Fullscreen";

        private const string OPTION_KEYBOARD1P_CENTER = "Keyboard1pCenter";
        private const string OPTION_KEYBOARD1P_UP = "Keyboard1pUp";
        private const string OPTION_KEYBOARD1P_DOWN = "Keyboard1pDown";
        private const string OPTION_KEYBOARD1P_LEFT = "Keyboard1pLeft";
        private const string OPTION_KEYBOARD1P_RIGHT = "Keyboard1pRight";
        private const string OPTION_KEYBOARD1P_START = "Keyboard1pStart";
        private const string OPTION_KEYBOARD1P_SHOOT = "Keyboard1pShoot";
        private const string OPTION_KEYBOARD1P_RELOAD = "Keyboard1pReload";

        private const string OPTION_KEYBOARD2P_CENTER = "Keyboard2pCenter";
        private const string OPTION_KEYBOARD2P_UP = "Keyboard2pUp";
        private const string OPTION_KEYBOARD2P_DOWN = "Keyboard2Down";
        private const string OPTION_KEYBOARD2P_LEFT = "Keyboard2pLeft";
        private const string OPTION_KEYBOARD2P_RIGHT = "Keyboard2pRight";
        private const string OPTION_KEYBOARD2P_START = "Keyboard2pStart";
        private const string OPTION_KEYBOARD2P_SHOOT = "Keyboard2pShoot";
        private const string OPTION_KEYBOARD2P_RELOAD = "Keyboard2pReload";

        private const string OPTION_DISABLE_PAUSE = "DisableInGamePause";
        private const string OPTION_DISPLAY_SCORE = "DisplayScore";

        private const string OPTION_VOLUME_BGM = "VolumeBGM";
        private const string OPTION_VOLUME_SFX = "VolumeSFX";
        private const string OPTION_VOLUME_VCE = "VolumeVCE";

        private string _ConfigFilePath = string.Empty;

        public ArcadeModConfig() : this("", null)
        { }

        public ArcadeModConfig(string GameFolder, GameConfig Config)
        {
            InitializeComponent();

            this.Text = "The House of the Dead III - Arcade Configurator v" + System.Reflection.Assembly.GetEntryAssembly().GetName().Version.ToString();

            _ConfigFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Properties.Resources.DATA_PAYLOADS_FOLDER, Config.RomName, CONF_FILENAME);

            if (File.Exists(_ConfigFilePath))
                ReadConf();
            else
                SetDefaultValues();
        }

        private void Btn_Save_Click(object sender, EventArgs e)
        {
            WriteConf();
        }

        private void ReadConf()
        {
            try
            {
                String[] lines = System.IO.File.ReadAllLines(_ConfigFilePath);
                foreach (String line in lines)
                {
                    if (!line.StartsWith("[") && !line.StartsWith(";"))
                    {
                        String[] buffer = line.Split(':');
                        String Key = buffer[0];
                        String Value = buffer[1];
                        int i_Value = 0;

                        switch (Key)
                        {                          
                            case OPTION_DIFFICULTY:
                                {
                                    if (int.TryParse(Value, out i_Value))
                                        Cbox_Difficulty.SelectedIndex = i_Value;
                                }
                                break;

                            case OPTION_INITIAL_LIFE:
                                {
                                    if (int.TryParse(Value, out i_Value))
                                        Cbox_InitialLife.SelectedIndex = i_Value - 1;
                                }
                                break;

                            case OPTION_MAX_LIFE:
                                {
                                    if (int.TryParse(Value, out i_Value))
                                        Cbox_MaxLife.SelectedIndex = i_Value - 1;
                                }
                                break;
                            case OPTION_BLOOD_COLOR:
                                {
                                    if (int.TryParse(Value, out i_Value))
                                        Cbox_BloodColor.SelectedIndex = i_Value;
                                }
                                break;
                            case OPTION_VIOLENCE:
                                {
                                    if (int.TryParse(Value, out i_Value))
                                        Cbox_Violence.SelectedIndex = i_Value;
                                }
                                break;
                            case OPTION_ADVERTISE:
                                {
                                    if (int.TryParse(Value, out i_Value))
                                        Cbox_AdvertiseSound.SelectedIndex = i_Value;
                                }
                                break;
                            case OPTION_LANGUAGE:
                                {
                                    if (int.TryParse(Value, out i_Value))
                                        Cbox_Language.SelectedIndex = i_Value;
                                }
                                break;
                            case OPTION_CREDITS_TO_START:
                                {
                                    if (int.TryParse(Value, out i_Value))
                                        Cbox_CreditsToStart.SelectedIndex = i_Value - 1;
                                }
                                break;
                            case OPTION_CREDITS_TO_CONTINUE:
                                {
                                    if (int.TryParse(Value, out i_Value))
                                        Cbox_CreditsToContinue.SelectedIndex = i_Value - 1;
                                }
                                break;
                            case OPTION_FREEPLAY:
                                {
                                    if (int.TryParse(Value, out i_Value))
                                        Cbox_Freeplay.SelectedIndex = i_Value;
                                }
                                break;

                            case OPTION_ALIASING:
                                {
                                    if (int.TryParse(Value, out i_Value))
                                        Cbox_Aliasing.SelectedIndex = i_Value;
                                }
                                break;
                            case OPTION_COLORS:
                                {
                                    if (int.TryParse(Value, out i_Value))
                                        Cbox_Colors.SelectedIndex = i_Value - 1;
                                }
                                break;
                            case OPTION_TEXTURES:
                                {
                                    if (int.TryParse(Value, out i_Value))
                                        Cbox_Textures.SelectedIndex = i_Value;
                                }
                                break;
                            case OPTION_RESOLUTION:
                                {
                                    if (int.TryParse(Value, out i_Value))
                                        Cbox_Resolution.SelectedIndex = i_Value;
                                }
                                break;
                            case OPTION_FRAMERATE:
                                {
                                    if (int.TryParse(Value, out i_Value))
                                        Cbox_Framerate.SelectedIndex = i_Value;
                                }
                                break;
                            case OPTION_FULLSCREEN:
                                {
                                    if (int.TryParse(Value, out i_Value))
                                        Cbox_Fullscreen.SelectedIndex = i_Value;
                                }
                                break;                            
                            case OPTION_DISABLE_PAUSE:
                                {
                                    if (int.TryParse(Value, out i_Value))
                                    {
                                        if (i_Value == 1)
                                            Chk_DisablePause.Checked = true;
                                        else
                                            Chk_DisablePause.Checked = false;
                                    }
                                }
                                break;
                            case OPTION_DISPLAY_SCORE:
                                {
                                    if (int.TryParse(Value, out i_Value))
                                    {
                                        if (i_Value == 1)
                                            Chk_DisplayScore.Checked = true;
                                        else
                                            Chk_DisplayScore.Checked = false;
                                    }
                                }
                                break;
                            case OPTION_VOLUME_BGM:
                                {
                                    if (int.TryParse(Value, out i_Value))
                                        TrackBar_BGM.Value = i_Value;
                                }
                                break;
                            case OPTION_VOLUME_SFX:
                                {
                                    if (int.TryParse(Value, out i_Value))
                                        TrackBar_SFX.Value = i_Value;
                                }
                                break;
                            case OPTION_VOLUME_VCE:
                                {
                                    if (int.TryParse(Value, out i_Value))
                                        TrackBar_VCE.Value = i_Value;
                                }
                                break;
                            default: break;
                        }
                    }
                }
            }
            catch (Exception Ex)
            {
                MessageBox.Show("Can't read config data :\n" + _ConfigFilePath + " :\n\n" + Ex.Message.ToString(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                SetDefaultValues();
            }
        }

        private void WriteConf()
        {
            try
            {
                using (StreamWriter sw = new StreamWriter(_ConfigFilePath, false))
                {
                    sw.WriteLine("[Arcade SYSTEM MENU options]");
                    sw.WriteLine(OPTION_DIFFICULTY + ":" + Cbox_Difficulty.SelectedIndex.ToString());
                    sw.WriteLine(OPTION_INITIAL_LIFE + ":" + (Cbox_InitialLife.SelectedIndex + 1).ToString());
                    sw.WriteLine(OPTION_MAX_LIFE + ":" + (Cbox_MaxLife.SelectedIndex + 1).ToString());
                    sw.WriteLine(OPTION_BLOOD_COLOR + ":" + Cbox_BloodColor.SelectedIndex.ToString());
                    sw.WriteLine(OPTION_VIOLENCE + ":" + Cbox_Violence.SelectedIndex.ToString());
                    sw.WriteLine(OPTION_ADVERTISE + ":" + Cbox_AdvertiseSound.SelectedIndex.ToString());
                    sw.WriteLine(OPTION_LANGUAGE + ":" + Cbox_Language.SelectedIndex.ToString());
                    sw.WriteLine(OPTION_CREDITS_TO_START + ":" + (Cbox_CreditsToStart.SelectedIndex + 1).ToString());
                    sw.WriteLine(OPTION_CREDITS_TO_CONTINUE + ":" + (Cbox_CreditsToContinue.SelectedIndex + 1).ToString());
                    sw.WriteLine(OPTION_FREEPLAY + ":" + Cbox_Freeplay.SelectedIndex.ToString());

                    sw.WriteLine("[Game options]");
                    sw.WriteLine(OPTION_ALIASING + ":" + Cbox_Aliasing.SelectedIndex.ToString());
                    sw.WriteLine(OPTION_COLORS + ":" + (Cbox_Colors.SelectedIndex + 1).ToString());
                    sw.WriteLine(OPTION_TEXTURES + ":" + Cbox_Textures.SelectedIndex.ToString());
                    sw.WriteLine(OPTION_RESOLUTION + ":" + Cbox_Resolution.SelectedIndex.ToString());
                    sw.WriteLine(OPTION_FRAMERATE + ":" + Cbox_Framerate.SelectedIndex.ToString());
                    sw.WriteLine(OPTION_FULLSCREEN + ":" + Cbox_Fullscreen.SelectedIndex.ToString());                   
                    sw.WriteLine(OPTION_VOLUME_BGM + ":" + TrackBar_BGM.Value.ToString());
                    sw.WriteLine(OPTION_VOLUME_SFX + ":" + TrackBar_SFX.Value.ToString());
                    sw.WriteLine(OPTION_VOLUME_VCE + ":" + TrackBar_VCE.Value.ToString());

                    sw.WriteLine("[Keyboard Controls]");
                    sw.WriteLine(";Keyboard ScanCodes are needed (https://kippykip.com/b3ddocs/commands/scancodes.htm)");
                    sw.WriteLine(OPTION_KEYBOARD1P_UP + ":200");
                    sw.WriteLine(OPTION_KEYBOARD1P_DOWN + ":208");
                    sw.WriteLine(OPTION_KEYBOARD1P_LEFT + ":203");
                    sw.WriteLine(OPTION_KEYBOARD1P_RIGHT + ":205");
                    sw.WriteLine(OPTION_KEYBOARD1P_CENTER + ":57");
                    sw.WriteLine(OPTION_KEYBOARD1P_START + ":2");
                    sw.WriteLine(OPTION_KEYBOARD1P_SHOOT + ":29");
                    sw.WriteLine(OPTION_KEYBOARD1P_RELOAD + ":42");

                    sw.WriteLine(OPTION_KEYBOARD2P_UP + ":72");
                    sw.WriteLine(OPTION_KEYBOARD2P_DOWN + ":80");
                    sw.WriteLine(OPTION_KEYBOARD2P_LEFT + ":75");
                    sw.WriteLine(OPTION_KEYBOARD2P_RIGHT + ":77");
                    sw.WriteLine(OPTION_KEYBOARD2P_CENTER + ":76");
                    sw.WriteLine(OPTION_KEYBOARD2P_START + ":3");
                    sw.WriteLine(OPTION_KEYBOARD2P_SHOOT + ":157");
                    sw.WriteLine(OPTION_KEYBOARD2P_RELOAD + ":54");                    

                    sw.WriteLine("[Mod Options]");                    
                    if (Chk_DisablePause.Checked)
                        sw.WriteLine(OPTION_DISABLE_PAUSE + ":1");
                    else
                        sw.WriteLine(OPTION_DISABLE_PAUSE + ":0");

                    if (Chk_DisplayScore.Checked)
                        sw.WriteLine(OPTION_DISPLAY_SCORE + ":1");
                    else
                        sw.WriteLine(OPTION_DISPLAY_SCORE + ":0");
                }
            }
            catch (Exception Ex)
            {
                MessageBox.Show("Can't save config data to :\n" + _ConfigFilePath + " :\n\n" + Ex.Message.ToString(), "Hod3 Arcade Configurator", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            DialogResult = DialogResult.OK;
        }

        private void SetDefaultValues()
        {
            Cbox_Difficulty.SelectedIndex = 2;
            Cbox_InitialLife.SelectedIndex = 3;
            Cbox_MaxLife.SelectedIndex = 4;
            Cbox_BloodColor.SelectedIndex = 0;
            Cbox_Violence.SelectedIndex = 0;
            Cbox_AdvertiseSound.SelectedIndex = 1;
            Cbox_Language.SelectedIndex = 0;
            Cbox_CreditsToStart.SelectedIndex = 0;
            Cbox_CreditsToContinue.SelectedIndex = 0;
            Cbox_Freeplay.SelectedIndex = 0;

            Cbox_Aliasing.SelectedIndex = 1;
            Cbox_Colors.SelectedIndex = 0;
            Cbox_Textures.SelectedIndex = 2;
            Cbox_Resolution.SelectedIndex = 4;
            Cbox_Framerate.SelectedIndex = 0;
            Cbox_Fullscreen.SelectedIndex = 1;

            Chk_DisablePause.Checked = true;

            TrackBar_BGM.Value = 255;
            TrackBar_SFX.Value = 255;
            TrackBar_VCE.Value = 255;
        }

        private void Btn_Close_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
        }
    }
}
