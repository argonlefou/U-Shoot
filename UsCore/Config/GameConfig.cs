using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;

namespace UsCore.Config
{
    public class GameConfig : BaseConfig
    {     

        #region Most common Game options

        public bool NoInputs
        { get { return GetBoolOption("NoInputs"); } }

        public bool NoCrosshair
        { get { return GetBoolOption("NoCrosshair"); } }

        public bool NoCursor
        { get { return GetBoolOption("NoCursor"); } }

        public bool NoGun
        { get { return GetBoolOption("NoGun"); } }

        public bool HideConsole
        { get { return GetBoolOption("Hide Console"); } }

        public string X_Scaling
        { get { return GetStringOption("X Scaling", "1"); } }

        public int ResolutionWidth
        { get { return GetNumericOption("Resolution Width"); } }

        public int ResolutionHeight
        { get { return GetNumericOption("Resolution Height"); } }

        public bool Fullscreen
        { get { return string.Equals(GetStringOption("Display Mode"), "fullscreen", System.StringComparison.OrdinalIgnoreCase); } }

        public bool Windowed
        { get { return string.Equals(GetStringOption("Display Mode"), "windowed", System.StringComparison.OrdinalIgnoreCase); } }

        #endregion

        //Title will be displayed in the Game List GUI
        public string Title;

        //Identifier will be the xml filename
        public string Identifier;

        //Will be used to display the games in the main GUI
        public string Category;

        //Use to display hint message to know what file to select in Game GUI option window
        public string TargetFileName;

        //Target Path will be used by the Launcher to start the process
        public string TargetPath;

        //Max Player will define how many times INPUT per players will be displayed
        public int PlayerCount;

        //Used to define the RomName in MameHooker (or any equivalent Output software) to ensure backward compatibility with Demulshooter 
        public string RomName;

        //Used to define the correct Window to use as reference to compute axes values
        public string WindowTitle;

        public GameConfig() : this(0)
        { }

        public GameConfig(int PlayerCount) : base(PlayerCount)
        {
            this.PlayerCount = PlayerCount;

            System_Inputs = new List<BaseConfig_Input>();

            Players_Inputs = new List<BaseConfig_Player>(this.PlayerCount);
            for (int i = 0; i < this.PlayerCount; i++)
            {
                Players_Inputs.Add(new BaseConfig_Player(i + 1));
            }

            Options = new List<BaseConfig_Option>();
        }

        /// <summary>
        /// Game config are stored in 2 different places :
        /// - Default ones on /Data/Games/ with default value and config structure/options
        /// - User saved ones, in /USerData/ with custom values
        /// It's not possible to directly load user config, as old values structure may be different in the newest default ones.
        /// That's why it must be analysed item by item so load them to the default one. User file will then be overwritten.
        /// </summary>
        /// <param name="ConfigFilePath"></param>
        /// <returns></returns>
        public static GameConfig LoadFromDisk(string ConfigFilePath)
        {
            string GameFileName = Path.GetFileName(ConfigFilePath);
            string UserConfigFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, USERDATA_FOLDER, USERDATA_GAMES_FOLDER, GameFileName);

            GameConfig DefaultConfig = BaseConfig.LoadFromXml(typeof(GameConfig), ConfigFilePath) as GameConfig;

            if (File.Exists(UserConfigFilePath))
            {
                GameConfig UserConfig = BaseConfig.LoadFromXml(typeof(GameConfig), UserConfigFilePath) as GameConfig;
                if (UserConfig != null)
                {
                    DefaultConfig.MergeCustomConfigurationData(UserConfig, false, false, false);

                    //Loading saved TargetPath
                    if (UserConfig.TargetPath.Length > 0)
                    {
                        DefaultConfig.TargetPath = UserConfig.TargetPath;
                    }
                }
                else
                {
                    MessageBox.Show("Error loading the following config file:\n" + UserConfigFilePath + "\n\nPlease check the file again.", GameFileName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }

            return DefaultConfig;
        }

        public InputAxesEntry GetPlayerAxesEntryFromDevicePath(int PlayerId, string DevicePath)
        {
            for (int i = 0; i < Players_Inputs[PlayerId].Inputs.Count; i++)
            {
                if (Players_Inputs[PlayerId].Inputs[i] is InputAxes)
                {
                    InputAxes Axes = ((InputAxes)Players_Inputs[PlayerId].Inputs[i]);
                    for (int j = 0; j < Axes.Keys.Count; j++)
                    {
                        if (Axes.Keys[j].DevicePath == DevicePath)
                            return Axes.Keys[j];
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Any change in the config file (control / options) will be saved in a different folder.
        /// That way, updating U-Shoot and overwriting /Data/Game/*xml files will not result in loosing any previously set inputs/options
        /// </summary>
        public bool SaveConfig(string ConfigFilePath)
        {
            string GameFileName = Path.GetFileName(ConfigFilePath);
            string UserDataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, USERDATA_FOLDER, USERDATA_GAMES_FOLDER);

            if (!Directory.Exists(UserDataPath))
                Directory.CreateDirectory(UserDataPath);

            return SaveToXml(this, Path.Combine(UserDataPath, GameFileName));
        }
    }
}