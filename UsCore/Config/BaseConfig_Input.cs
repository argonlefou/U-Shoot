using System.Collections.Generic;
using System.Xml.Serialization;

namespace UsCore.Config
{
    [XmlInclude(typeof(InputAxes))]
    [XmlInclude(typeof(InputButton))]
    public class BaseConfig_Input
    {
        public string Name;

        //Needed for Serialization
        public BaseConfig_Input() : this(string.Empty)
        { }

        public BaseConfig_Input(string Name)
        {
            this.Name = Name;
        }

        public virtual int GetNumberOfEntries()
        { return 0; }

    }

    public class InputAxes : BaseConfig_Input
    {
        //Needed for Serialization
        public InputAxes() : this(string.Empty)
        { }

        public InputAxes(string Name) : base(Name)
        {
            Keys = new List<InputAxesEntry>();
        }

        public override int GetNumberOfEntries()
        { return Keys.Count; }

        public List<InputAxesEntry> Keys;
    }
    
    public class InputAxesEntry
    {
        public string DevicePath;
        public string DeviceName;
        public string DeviceType;
        public bool IsAbsolute;
        public bool OverrideCalibration;
        public InputAxis X;
        public InputAxis Y;

        //Needed for Serialization
        public InputAxesEntry()
        { }
    }


    public class InputAxis
    {
        public int ID;
        public bool IsInverted;
        public long MinValue;
        public long MaxValue;

        public InputAxis()
        { }
    }

    public class InputButton : BaseConfig_Input
    {
        //Needed for Serialization
        public InputButton() : this(string.Empty)
        { }

        public InputButton(string Name) : base(Name)
        {
            Keys = new List<InputButtonEntry>();
        }

        public override int GetNumberOfEntries()
        { return Keys.Count; }

        public List<InputButtonEntry> Keys;
        
    }

    public class InputButtonEntry
    {
        public string DevicePath;
        public string DeviceName;
        public int ButtonId;
        public string ButtonName;
        public string DeviceType;
    }
}
