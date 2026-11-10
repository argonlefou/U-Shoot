namespace UsCore.Config
{
    public class BaseConfig_Option
    {
        public enum ItemType
        {
            Numeric,
            Bool,
            Text,
            List,
            Button,
            Browse
        }

        public string Name;
        public string Type;
        public string Value;

        //Needed for serialization
        public BaseConfig_Option() : this(string.Empty, string.Empty, string.Empty)
        { }

        public BaseConfig_Option(string Name, string Type, string Value)
        {
            this.Name = Name;
            this.Type = Type;
            this.Value = Value;
        }
    }
}
