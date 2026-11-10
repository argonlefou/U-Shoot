using System;

namespace UsCore.MameOutput
{
    /// <summary>
    /// A specific Output from a game to send to MameHooker
    /// </summary>
    public class GameOutput
    {
        protected String _Name;
        protected UInt32 _Id;
        protected int _OutputValue;

        public String Name
        { get { return _Name; } }

        public UInt32 Id
        { get { return _Id; } }

        public virtual int OutputValue
        {
            get { return _OutputValue; }
            set {  _OutputValue = value;}
        }

        public GameOutput(OutputId Id)
        {
            _Name = Id.ToString();
            _Id = (UInt32)Id;
            _OutputValue = 0;
        }
    }
}
