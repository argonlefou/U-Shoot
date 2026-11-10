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

        private volatile bool _ResetButtonDownFlag = false;
        private volatile bool _ResetButtonUpFlag = false;

        public int KeyCode { get; private set; }

        public PluginControllerButton(int iKeyCode = 0)
        {
            KeyCode = iKeyCode;

            RawState = false;
            PreviousRawState = false;

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

            if (_ResetButtonDownFlag)
            {
                _FlagButtonDown = false;
                _ResetButtonDownFlag = false;
            }

            if (_ResetButtonUpFlag)
            {
                _FlagButtonUp = false;
                _ResetButtonUpFlag = false;
            }
        }

        // Those EVENTS will only be available during 1 single Frame (the same one it is triggered)
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

        // Those EVENTS will be available untill request or state change and will be available during a whole Frame.
        // There are cases where multiple Unity classes are asking for a button state during the same Frame, so the event can't be reset at first call
        public bool GetRawButtonDown()
        {
            if (_FlagButtonDown)
            {
                _ResetButtonDownFlag = true;
                return true;
            }
            return false;
        }

        public bool GetRawButtonUp()
        {
            if (_FlagButtonUp)
            {
                _ResetButtonUpFlag = true;
                return true;
            }
            return false;
        }
    }
}
