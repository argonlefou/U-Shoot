namespace UnityPlugin_BepInEx.Core
{
    public class PluginControllerButton
    {
        public volatile bool _Rawstate;
        public bool RawState
        {
            get { return _Rawstate; }
            private set { _Rawstate = value; }
        }

        private volatile bool PreviousRawState;

        public bool FrameState { get; private set; }
        private bool PreviousFrameState;

        private volatile bool _FlagButtonDown;
        private volatile bool _FlagButtonUp;

        public int KeyCode { get; private set; }

        public PluginControllerButton(int iKeyCode = 0)
        {
            KeyCode = iKeyCode;

            RawState = false;
            PreviousFrameState = false;

            FrameState = false;
            PreviousFrameState = false;
        }

        public void SetKeyCode(string StrValue)
        {
            int iResult;
            if (int.TryParse(StrValue, out iResult))
                KeyCode = iResult;
        }

        public void SetKeyCode(int iValue)
        {
            KeyCode = iValue;
        }

        public void SetRawState(bool Value)
        {
            PreviousRawState = RawState;
            RawState = Value;

            //Setting Up/Down RAW events
            if (PreviousRawState != RawState)
            {
                if (RawState)
                {
                    _FlagButtonDown = true;
                    _FlagButtonUp = false;
                }
                else
                {
                    _FlagButtonDown = false;
                    _FlagButtonUp = true;
                }
            }
        }

        public void SetFrameState()
        {
            PreviousFrameState = FrameState;
            FrameState = RawState;
        }

        /** Those EVENTS will be available during a whole Frame **/
        public bool GetButton()
        {
            return FrameState;
        }

        public bool GetButtonDown()
        {
            return FrameState && !PreviousFrameState;
        }

        public bool GetButtonUp()
        {
            return PreviousFrameState && !FrameState;
        }

        /** Those EVENTS will be available just once and immediately consummed **/
        public bool GetRawButtonDown()
        {
            if (_FlagButtonDown)
            {
                _FlagButtonDown = false;
                return true;
            }
            return false;
        }

        public bool GetRawButtonUp()
        {
            if (_FlagButtonUp)
            {
                _FlagButtonUp = false;
                return true;
            }
            return false;
        }
    }
}
