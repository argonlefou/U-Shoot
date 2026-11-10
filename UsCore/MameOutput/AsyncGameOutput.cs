using System;
using UsCore.Logging;

namespace UsCore.MameOutput
{
    public class AsyncGameOutput : GameOutput
    {
        private Mmt _AsyncResetTimer;
        private UInt32 _AsyncResetTimerOnInterval = 50;
        private UInt32 _AsyncResetTimerOffInterval = 50;
        private bool _IsTimerRunning = false;
        private int _OffValue = 0;

        public override int OutputValue
        {
            get
            {
                { return _OutputValue; }
            }
            set
            {
                if (!_IsTimerRunning && value != _OffValue)
                {
                    if (value != _OffValue)
                    {
                        _IsTimerRunning = true;
                        _OutputValue = value;
                        _AsyncResetTimer.Period = _AsyncResetTimerOnInterval;
                        _AsyncResetTimer.Start();
                        #if DEBUG_ASYNCGAMEOUTPUTS
                            Logger.WriteLog("[Tick=" + Environment.TickCount + "] Starting " + this._Name + " OutputTimer with value = " + value);
                        #endif
                    }
                }
            }
        }

        public AsyncGameOutput(OutputId Id, int AsyncResetTimerOnInterval, int AsyncResetTimerOffInterval, int RestValue)
            : base(Id)
        {
            _OffValue = RestValue;
            _AsyncResetTimerOnInterval = (UInt32)AsyncResetTimerOnInterval;
            _AsyncResetTimerOffInterval = (UInt32)AsyncResetTimerOffInterval;
            _AsyncResetTimer = new Mmt();
            _AsyncResetTimer.Period = (UInt32)AsyncResetTimerOnInterval;
            _AsyncResetTimer.Stop();
            _AsyncResetTimer.Tick += AsyncResetTimer_Elapsed;
        }

        private void AsyncResetTimer_Elapsed(Object sender, EventArgs e)
        {
            if (_OutputValue != _OffValue)
            {
                _AsyncResetTimer.Period = _AsyncResetTimerOffInterval;
                _OutputValue = _OffValue;
                #if DEBUG_ASYNCGAMEOUTPUTS
                    Logger.WriteLog("[Tick=" + Environment.TickCount + "] Forcing " + this._Name + " OutputTimer with value = " + _OffValue);
                #endif
                //MameOutputHelper.Instance().SendValue(this._Id, this._OutputValue);
            }
            else
            {
                #if DEBUG_ASYNCGAMEOUTPUTS
                    Logger.WriteLog("[Tick=" + Environment.TickCount + "] Stopping " + this._Name + " OutputTimer with value = " + _OffValue);
                #endif
                _AsyncResetTimer.Stop();
                _IsTimerRunning = false;
            }            
        }
    }
}
