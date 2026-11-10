using System;

namespace UsCore.RawInput
{
    public class RawInputButton
    {
        private bool _IsPressed = false;

        #region Events

        public delegate void KeyDownEventHandler(object sender, EventArgs e);
        public event KeyDownEventHandler OnKeyDown;

        public delegate void KeyUpEventHandler(object sender, EventArgs e);
        public event KeyUpEventHandler OnKeyUp;

        #endregion

        public RawInputButton() 
        {
            _IsPressed = false;
        }

        /// <summary>
        /// Setting the button pressed state and triggering events if needed
        /// </summary>
        /// <param name="NewPressedState"></param>
        public void SetState(bool NewPressedState)
        {
            if (_IsPressed)
            {
                if (!NewPressedState)
                {
                    if (OnKeyUp != null)
                        OnKeyUp(this, EventArgs.Empty);
                }
            }
            else
            {
                if (NewPressedState)
                {
                    if (OnKeyDown != null)
                        OnKeyDown(this, EventArgs.Empty);
                }
            }
            _IsPressed = NewPressedState;
        }
    }
}
