using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace FewTags.Utils
{
    internal class ConsoleUtils
    {
        private static readonly object _beepLock = new();
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern bool SetConsoleTitle(string lpConsoleTitle);

        public static void RenameConsole(string title)
        {
            SetConsoleTitle(title);
        }
        public static void AmongUsBeep()
        {
            Task.Run(() =>
            {
                lock (_beepLock)
                {
                    PlayPattern();
                }
            });
        }

        public static void Beep()
        {
            Console.Beep();
        }

        public static void Beep(int frequency, int duration)
        {
            Console.Beep(frequency, duration);
        }

        private static void PlayPattern()
        {
            Beep(300, 400);
            Thread.Sleep(20);

            for (int i = 0; i < 7; i++)
            {
                Beep(750, 100);
                Thread.Sleep(40);
            }

            Thread.Sleep(20);
            Beep(750, 100);
            Thread.Sleep(10);
            Beep(700, 100);
            Thread.Sleep(10);
            Beep(750, 100);

            Beep(400, 180);
            Thread.Sleep(100);
            Beep(400, 180);
        }
    }
}
