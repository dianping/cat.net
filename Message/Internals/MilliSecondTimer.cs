namespace Com.Dianping.Cat.Message.Internals
{
    using System;
    using System.Runtime.InteropServices;

    ///<summary>
    ///  This timer provides milli-second precise system time.
    ///</summary>
    public class MilliSecondTimer
    {
        public static long CurrentTimeMicros()
        {
            //return HighResTicksProvider.GetTickCount () / 10L; // it's microsecond precise
            return DateTime.Now.Ticks/10L; // it's millisecond precise
        }

        public static long CurrentTimeMillisForJava()
        {
            DateTime baseline = new DateTime(1970, 1, 1, 0, 0, 0);
            TimeSpan ts = new TimeSpan(DateTime.UtcNow.Ticks - baseline.Ticks);

            return ((long) ts.TotalMilliseconds/3600000L);
        }
    }

    public class HighResTicksProvider
    {
        private static long _f;

        [DllImport("kernel32.dll")]
        private static extern bool QueryPerformanceCounter([In, Out] ref long lpPerformanceCount);

        [DllImport("kernel32.dll")]
        private static extern bool QueryPerformanceFrequency([In, Out] ref long lpFrequency);

        /// <summary>
        ///   获得当前时间戳，十分之一微秒（100纳秒，和 DateTime.Now.Ticks 刻度一样）
        /// </summary>
        /// <returns> </returns>
        public static long GetTickCount()
        {
            long f = _f;

            if (f == 0)
            {
                if (QueryPerformanceFrequency(ref f))
                {
                    _f = f;
                }
                else
                {
                    _f = -1;
                }
            }

            if (_f == -1)
            {
                // fallback
                return DateTime.Now.Ticks;
            }

            long c = 0;
            QueryPerformanceCounter(ref c);

            return (long) (((double) c)*1000*10000/(f));
        }
    }
}