using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bendric.Api
{
    public static class Logger
    {
        private static readonly object LockObj = new object();
        private static bool _noLogo = false;

        public static void SetNoLogo(bool noLogo)
        {
            _noLogo = noLogo;
        }

        private static void WriteLog(string level, string message, ConsoleColor color)
        {
            if (_noLogo && level == "LOGO")
                return;

            lock (LockObj)
            {
                string timestamp = DateTime.Now.ToString("yyyy/M/d HH:mm:ss");
                ConsoleColor originalColor = Console.ForegroundColor;

                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.Write($"[{timestamp}] ");

                Console.ForegroundColor = color;
                Console.Write($"[{level}] ");

                Console.ForegroundColor = originalColor;
                Console.WriteLine(message);
            }
        }

        public static void Logo(string message)
        {
            lock (LockObj)
            {
                if (_noLogo) return;
                var originalColor = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Magenta;
                Console.WriteLine(message);
                Console.ForegroundColor = originalColor;
            }
        }

        public static void Debug(string message)
        {
            WriteLog("DEBUG", message, ConsoleColor.DarkGray);
        }

        public static void Info(string message)
        {
            WriteLog("INFO", message, ConsoleColor.Green);
        }

        public static void Warn(string message)
        {
            WriteLog("WARN", message, ConsoleColor.Yellow);
        }

        public static void Error(string message)
        {
            WriteLog("ERROR", message, ConsoleColor.Red);
        }
    }
}
