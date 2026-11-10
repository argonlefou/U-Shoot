using System;

namespace UsCore.RawInput
{
    public class RawInputAxis
    {
        
        #region Events

        public delegate void ValueChangedEventHandler(object sender, ValueChangedEventArgs e);
        public event ValueChangedEventHandler OnValueChanged;

        public class ValueChangedEventArgs : EventArgs
        {
            public int Value { get; private set; }

            public ValueChangedEventArgs(int Value)
            {
                this.Value = Value;
            }
        }

        #endregion

        private int _Value;

        public bool ValueChangedWithDeadzone
        { get; private set; }

        public int AxisId
        { get; private set; }

        public long MinValue
        { get; private set; }

        public long MaxValue
        { get; private set; }

        public int Value
        {
            get { return _Value; }
            set
            {
                if (value != _Value)
                {
                    ValueChangedEventArgs eventArgs = new ValueChangedEventArgs(Value);

                    if (OnValueChanged != null)
                        OnValueChanged(this, eventArgs);

                    _Value = value;
                }
            }
        }

        public RawInputAxis(int Id, long LogicalMin, long LogicalMax)
        {
            AxisId = Id;
            MinValue = LogicalMin;
            MaxValue = LogicalMax;
            ValueChangedWithDeadzone = false;
        }

        
    }
}
