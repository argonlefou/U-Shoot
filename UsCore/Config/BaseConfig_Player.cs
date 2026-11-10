using System.Collections.Generic;
using System.Xml.Serialization;

namespace UsCore.Config
{
    public class BaseConfig_Player
    {
        public int Id;

        [XmlArray("Inputs")]
        [XmlArrayItem("Input", typeof(BaseConfig_Input))]
        /*[XmlArrayItem("Axis", typeof(Axis))]
        [XmlArrayItem("Button", typeof(Button))]*/
        public List<BaseConfig_Input> Inputs;

        public BaseConfig_Player() : this(1)
        { }

        public BaseConfig_Player(int PlayerId)
        {
            this.Id = PlayerId;
            Inputs = new List<BaseConfig_Input>();
        }

    }
}
