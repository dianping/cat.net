using System;

namespace Com.Dianping.Cat
{
    using Message.Internals;
    using System.IO;

    public class Logger
    {
        private static StreamWriter _mWriter;

        public static void Info(string pattern, params object[] args)
        {
            Log("INFO", pattern, args);
        }

        public static void Warn(string pattern, params object[] args)
        {
            Log("WARN", pattern, args);
        }

        public static void Error(string pattern, params object[] args)
        {
            Log("ERROR", pattern, args);
        }

        private static void Log(string severity, string pattern, params object[] args)
        {
            string timestamp = new DateTime(MilliSecondTimer.CurrentTimeMicros()*10L).ToString("yyyy-MM-dd HH:mm:ss.fff");
            string message = string.Format(pattern, args);
            string line = "[" + timestamp + "] [" + severity + "] " + message;

            if (_mWriter != null)
            {
                _mWriter.WriteLine(line);
                _mWriter.Flush();
            }
            else
            {
                Console.WriteLine(line);
            }
        }

        public static void Initialize(string logFile)
        {
            try
            {
                if (!File.Exists(logFile))
                {
                    var directoryInfo = new FileInfo(logFile).Directory;
                    if (directoryInfo != null) directoryInfo.Create();
                }

                _mWriter = new StreamWriter(logFile, true);
            }
            catch (Exception e)
            {
                Console.WriteLine("Error when openning log file: " + e.Message + " " + e.StackTrace + ".");
            }
        }
    }
}