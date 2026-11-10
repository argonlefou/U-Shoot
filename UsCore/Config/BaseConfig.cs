using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using UsCore.Logging;

namespace UsCore.Config
{   
    [XmlInclude(typeof(GameConfig))]
    [XmlInclude(typeof(ProgramConfig))]
    public class BaseConfig
    {
        protected const string USERDATA_FOLDER = "UserData";
        protected const string USERDATA_GAMES_FOLDER = "Games";
        protected const string DATA_TEMPLATES_FOLDER = "Data\\Templates";

        //Common Inputs, not related to player
        [XmlArray("Global_Inputs")]
        [XmlArrayItem("Input", typeof(BaseConfig_Input))]
        public List<BaseConfig_Input> System_Inputs;

        [XmlArray("Players_Inputs")]
        [XmlArrayItem("Player", typeof(BaseConfig_Player))]
        public List<BaseConfig_Player> Players_Inputs;

        //Various options
        [XmlArray("Options")]
        [XmlArrayItem("Option", typeof(BaseConfig_Option))]
        public List<BaseConfig_Option> Options;

        public BaseConfig(int PlayerCount)
        {
            System_Inputs = new List<BaseConfig_Input>(PlayerCount);
            Options = new List<BaseConfig_Option>(PlayerCount);
        }

        #region Option Getters

        public bool GetBoolOption(string OptionName, bool DefaultValue = false)
        {
            bool Value = DefaultValue;
            foreach (BaseConfig_Option Option in Options)
            {
                if (string.Equals(Option.Name, OptionName, StringComparison.OrdinalIgnoreCase))
                {
                    if (bool.TryParse(Option.Value, out Value)) 
                    { 
                        return Value; 
                    }
                    else
                    {
                        Logger.WriteLog("[" + this.GetType().Name + "] Error parsing '" + Option.Name + "=" + Option.Value + "' to Bool");
                        break;
                    }
                }
            }
            Logger.WriteLog("[" + this.GetType().Name + "] Option '" + OptionName + "' not found, returning default value: " + DefaultValue.ToString());
            return DefaultValue; 
        }

        public int GetNumericOption(string OptionName, int DefaultValue = 0)
        {
            int Value = DefaultValue;
            foreach (BaseConfig_Option Option in Options)
            {
                if (string.Equals(Option.Name, OptionName, StringComparison.OrdinalIgnoreCase))
                {
                    if (int.TryParse(Option.Value, out Value))
                    {
                        return Value;
                    }
                    else
                    {
                        Logger.WriteLog("[" + this.GetType().Name + "] Error parsing '" + Option.Name + "=" + Option.Value + "' to int");
                        break;
                    }
                }
            }
            Logger.WriteLog("[" + this.GetType().Name + "] Option '" + OptionName + "' not found, returning default value: " + DefaultValue.ToString());
            return DefaultValue;
        }

        public string GetStringOption(string OptionName, string DefaultValue = "")
        {
            foreach (BaseConfig_Option Option in Options)
            {                
                if (string.Equals(Option.Name, OptionName, StringComparison.OrdinalIgnoreCase))
                {
                    if (Option.Type == "List")
                        return Option.Value.Split(';')[0];
                    else if (Option.Type == "Text")
                        return Option.Value;
                    else if (Option.Type == "Browse")
                        return Option.Value;
                }
            }
            Logger.WriteLog("[" + this.GetType().Name + "] Option '" + OptionName + "' not found, returning default value: " + DefaultValue);
            return DefaultValue;
        }       

        #endregion

        #region Options Setters

        public void SetBoolOption(string OptionName, bool Value)
        {
            foreach (BaseConfig_Option Option in Options)
            {
                if (Option.Name == OptionName)
                {
                    Option.Value = Value.ToString();
                    return;
                }
            }
            Logger.WriteLog("[" + this.GetType().Name + "] SetBool(): Option '" + OptionName + "' not found");
        }

        public void SetNumericOption(string OptionName, int Value)
        {
            foreach (BaseConfig_Option Option in Options)
            {
                if (Option.Name == OptionName)
                {
                    Option.Value = Value.ToString();
                    return;
                }
            }
            Logger.WriteLog("[" + this.GetType().Name + "] SetNumeric() Option '" + OptionName + "' not found");
        }

        public void SetTextOption(string OptionName, string Value)
        {
            foreach (BaseConfig_Option Option in Options)
            {
                if (Option.Name == OptionName)
                {
                    Option.Value = Value;
                    return;
                }
            }
            Logger.WriteLog("[" + this.GetType().Name + "] SetTextOption() '" + OptionName + "' not found");
        }

        #endregion

        #region XML save/load

        /// <summary>
        /// Serialize an object to XML format and save ot to disk
        /// </summary>
        protected bool SaveToXml<T>(T tObjetc, string XmlFilePath)
        {
            try
            {
                XmlSerializer serializer = new XmlSerializer(typeof(T));
                using (StreamWriter writer = new StreamWriter(XmlFilePath))
                {
                    serializer.Serialize(writer, tObjetc);
                }
                return true;
            }
            catch (Exception Ex)
            {
                Logger.WriteLog("[" + this.GetType().Name + "] Error saving  " + XmlFilePath);
                Logger.WriteLog(Ex.Message.ToString());
                return false;
            }
        }

        /// <summary>
        /// Deserialize XML file to a targetted object
        /// </summary>
        public static object LoadFromXml(Type T, string XmlFilePath)
        {
            object o = null;
            try
            {
                XmlSerializer serializer = new XmlSerializer(T);
                using (StreamReader reader = new StreamReader(XmlFilePath))
                {
                    o = serializer.Deserialize(reader);
                }
            }
            catch (Exception Ex)
            {
                Logger.WriteLog("[UsCore.BaseConfig] LoadFromXml() error loading " + XmlFilePath);
                Logger.WriteLog("[UsCore.BaseConfig] --> " + Ex.Message.ToString());
            }

            return o;
        }

        /// <summary>
        /// Merging a custom user-saved config data with an empty template-based config file.
        /// Doing so will ensure that only correct data will be loaded if the template options/controls are different from the old one
        /// Also, different flags can allow to force load user preset into the final config file to keep settings (ex: manually added default inputs)
        /// </summary>
        protected void MergeCustomConfigurationData(BaseConfig UserConfig, bool ForceLoadSystemInputs, bool ForceLoadPlayersInput, bool ForceLoadOptions)
        {
            //Checking System inputs
            if (ForceLoadSystemInputs)
            {
                System_Inputs.Clear();
                System_Inputs = UserConfig.System_Inputs;
            }
            else
            {
                for (int i = 0; i < System_Inputs.Count; i++)
                {
                    for (int j = 0; j < UserConfig.System_Inputs.Count; j++)
                    {
                        if (System_Inputs[i].Name.Equals(UserConfig.System_Inputs[j].Name))
                        {
                            System_Inputs[i] = UserConfig.System_Inputs[j];
                            break;
                        }
                    }
                }
            }

            //Checking Player Inputs
            if (ForceLoadPlayersInput)
            {
                Players_Inputs.Clear();
                Players_Inputs = UserConfig.Players_Inputs;
            }
            else
            {
                foreach (BaseConfig_Player DefaultPlayer in Players_Inputs)
                {
                    foreach (BaseConfig_Player UserPlayer in UserConfig.Players_Inputs)
                    {
                        if (DefaultPlayer.Id == UserPlayer.Id)
                        {
                            //Found Matching Player ID
                            for (int DefaultInputIndex = 0; DefaultInputIndex < DefaultPlayer.Inputs.Count; DefaultInputIndex++)
                            {
                                foreach (BaseConfig_Input UserInput in UserPlayer.Inputs)
                                {
                                    if (DefaultPlayer.Inputs[DefaultInputIndex].Name.Equals(UserInput.Name))
                                    {
                                        DefaultPlayer.Inputs[DefaultInputIndex] = UserInput;
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
            }

            //Checking Options fields
            if (ForceLoadOptions)
            {
                Options.Clear();
                Options = UserConfig.Options;
            }
            else
            {
                for (int i = 0; i < Options.Count; i++)
                {
                    for (int j = 0; j < UserConfig.Options.Count; j++)
                    {
                        //Discarding BUTTON type option, as the value calls a class constructor and old value may be the wrong class if changed
                        if (!Options[i].Type.Equals(BaseConfig_Option.ItemType.Button.ToString()))
                        {
                            //All other fields are just numeric values
                            if (Options[i].Name.Equals(UserConfig.Options[j].Name) && Options[i].Type == UserConfig.Options[j].Type)
                            {
                                Options[i].Value = UserConfig.Options[j].Value;
                                break;
                            }
                        }
                    }
                }
            }
        }

        #endregion
    }
}
