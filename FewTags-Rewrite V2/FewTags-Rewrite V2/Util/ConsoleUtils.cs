using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FewTags.FewTags_Rewrite_V2.Util
{
    internal class ConsoleUtils
    {
        private static readonly object _beepLock = new();

        public static void Beep()
        {
            Console.Beep(); // Default beep
        }

        public static void Beep(int frequency, int duration)
        {
            // frequency in Hertz (37 to 32767), duration in milliseconds
            Console.Beep(frequency, duration);
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
