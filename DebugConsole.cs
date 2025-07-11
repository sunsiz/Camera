using System;
using System.IO;
using System.Runtime.InteropServices;

namespace CameraOverlay
{
    public static class DebugConsole
    {
        [DllImport("kernel32.dll")]
        static extern bool AllocConsole();

        [DllImport("kernel32.dll")]
        static extern bool FreeConsole();

        [DllImport("kernel32.dll")]
        static extern IntPtr GetConsoleWindow();

        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        private const int SW_HIDE = 0;
        private const int SW_SHOW = 5;

        private static bool consoleAllocated = false;

        public static void ShowConsole()
        {
            if (!consoleAllocated)
            {
                AllocConsole();
                consoleAllocated = true;

                // Redirect console output
                Console.SetOut(new StreamWriter(Console.OpenStandardOutput()) { AutoFlush = true });
                Console.SetError(new StreamWriter(Console.OpenStandardError()) { AutoFlush = true });
                
                Console.WriteLine("Camera Overlay Debug Console");
                Console.WriteLine("============================");
                Console.WriteLine($"Started at: {DateTime.Now}");
                Console.WriteLine();
            }

            IntPtr consoleWindow = GetConsoleWindow();
            if (consoleWindow != IntPtr.Zero)
            {
                ShowWindow(consoleWindow, SW_SHOW);
            }
        }

        public static void HideConsole()
        {
            IntPtr consoleWindow = GetConsoleWindow();
            if (consoleWindow != IntPtr.Zero)
            {
                ShowWindow(consoleWindow, SW_HIDE);
            }
        }

        public static void CloseConsole()
        {
            if (consoleAllocated)
            {
                FreeConsole();
                consoleAllocated = false;
            }
        }
    }
}
