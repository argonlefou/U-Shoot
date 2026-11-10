using System;
using System.Collections.Generic;
using System.IO;
using UsCore.Logging;

namespace UsCore.Config
{
    public class ProgramConfig : BaseConfig
    {
        public const int DEFAULT_PLAYER_COUNT = 4;
        public const string DEFAULT_CONF_FILENAME = "Config.xml";

        public const string WINDOWS_OUTPUTS = "WindowsOutputs";
        public const string NETWORK_OUTPUTS = "NetworkOutputs";
        public const string OUTPUTS_POLLING_DELAY = "OutputPollingDelay";

        public const string OUTPUTS_DAMAGED_DELAY = "OutputCustomDamagedDelay";
        public const string OUTPUTS_RECOIL_ON_DELAY = "OutputRecoilOnDelay";
        public const string OUTPUTS_RECOIL_OFF_DELAY = "OutputRecoilOffDelay";

        private static ProgramConfig _Instance;
        public static string CustomConfigFilename = string.Empty;

        public static ProgramConfig GetInstance()
        {
            if (_Instance == null)
                _Instance = CreateProgramConfig();

            return _Instance;
        }

        public ProgramConfig() : this(0)
        { }

        public ProgramConfig(int PlayerCount) : base(PlayerCount)
        {
            System_Inputs = new List<BaseConfig_Input>();

            Players_Inputs = new List<BaseConfig_Player>(PlayerCount);
            for (int i = 0; i < PlayerCount; i++)
            {
                Players_Inputs.Add(new BaseConfig_Player(i + 1));
            }

            Options = new List<BaseConfig_Option>();
        }


        public static ProgramConfig CreateProgramConfig()
        {
            string DefaultProgramConfigFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, DATA_TEMPLATES_FOLDER, DEFAULT_CONF_FILENAME);
            string UserProgramConfigFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, USERDATA_FOLDER, DEFAULT_CONF_FILENAME);

            ProgramConfig DefaultConfig = LoadFromXml(typeof(ProgramConfig), DefaultProgramConfigFilePath) as ProgramConfig;
            ProgramConfig UserConfig = null;

            //First priority is given to a custom config file if given in the command line options
            if (!string.IsNullOrEmpty(CustomConfigFilename))
            {
                if (File.Exists(CustomConfigFilename))
                {
                    try
                    {
                        UserConfig = LoadFromXml(typeof(ProgramConfig), CustomConfigFilename) as ProgramConfig;                        
                    }
                    catch (Exception Ex)
                    {
                        Logger.WriteLog("[ProgramConfig] CCtor() Error loading config file : " + CustomConfigFilename);
                        Logger.WriteLog(Ex.Message.ToString());
                    }
                }
                else
                {
                    Logger.WriteLog("[ProgramConfig] CCtor() Config file not found : " + CustomConfigFilename);
                    Logger.WriteLog("Exiting program....");
                    Environment.Exit(0);
                }
            }
            //If no custom file is given, trying to load the default user config file
            else
            {
                if (File.Exists(UserProgramConfigFilePath))
                {
                    try
                    {
                        UserConfig = LoadFromXml(typeof(ProgramConfig), UserProgramConfigFilePath) as ProgramConfig;                        
                    }
                    catch (Exception Ex)
                    {
                        Logger.WriteLog("[ProgramConfig] CCtor() Error loading config file : " + UserProgramConfigFilePath);
                        Logger.WriteLog(Ex.Message.ToString());
                    }
                }
            }

            //Now making sure only relevant data from a saved config file is imported in the Template
            if (UserConfig != null)
            {
                DefaultConfig.MergeCustomConfigurationData(UserConfig, true, true, false);
            }

            return DefaultConfig;
        }

        public bool SaveConfig()
        {
           return SaveToXml(this, Path.Combine(AppDomain.CurrentDomain.BaseDirectory, USERDATA_FOLDER, DEFAULT_CONF_FILENAME));
        }
    }
}
