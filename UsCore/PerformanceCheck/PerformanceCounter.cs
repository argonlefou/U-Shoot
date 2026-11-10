using System.Diagnostics;

namespace UsCore.PerformanceCheck
{
    public class PerformanceCounter
    {
        private Stopwatch _StopWatch;
        private long _Frequency;
        private long _NanosecondsPerTick;

        private static PerformanceCounter _Instance;

        public static PerformanceCounter GetInstance()
        {
            if (_Instance == null)
                _Instance = CreatePerformanceCounter();

            return _Instance;
        }

        public PerformanceCounter()
        {
            _StopWatch = new Stopwatch();
            _Frequency = Stopwatch.Frequency;
            _NanosecondsPerTick = (1000L * 1000L * 1000L) / _Frequency;

            DebugMessage("Stopwatch is High Resolution =  " + Stopwatch.IsHighResolution.ToString());
            DebugMessage("Stopwatch Timer frequency in ticks per second =  "+ _Frequency);
            DebugMessage("Stopwatch Timer is accurate within " + _NanosecondsPerTick + " nanoseconds");
        }

        public static PerformanceCounter CreatePerformanceCounter()
        {
            return new PerformanceCounter();
        }

        public void Restart(string Comment)
        {
            DebugMessage(Comment);
            _StopWatch.Restart();
        }

        public void Measure(string Comment)
        {
            _StopWatch.Stop();
            double ns = _StopWatch.ElapsedTicks / (double)Stopwatch.Frequency * 1000000000;
            double us = _StopWatch.ElapsedTicks / (double)Stopwatch.Frequency * 1000000;

            DebugMessage(Comment + ": ");
            DebugMessage(" Elaped Ticks = " + _StopWatch.ElapsedTicks);
            DebugMessage(" Elaped Time = " + ns.ToString() + " ns / " + us.ToString() + " us" + " / " + _StopWatch.ElapsedMilliseconds.ToString() + " ms");
        }

        private void DebugMessage(string Text)
        {
            Trace.WriteLine("[U-Shoot] " + Text);
        }
    }
}
