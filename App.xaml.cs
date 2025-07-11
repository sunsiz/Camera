using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;

namespace CameraOverlay
{
    public partial class App : Application
    {
        [DllImport("kernel32.dll")]
        private static extern bool AttachConsole(int dwProcessId);
        
        [DllImport("kernel32.dll")]
        private static extern bool AllocConsole();
        
        [DllImport("kernel32.dll")]
        private static extern bool FreeConsole();

        private const int ATTACH_PARENT_PROCESS = -1;

        protected override void OnStartup(StartupEventArgs e)
        {
            try
            {
                // Try to attach to parent console (VS Code terminal) first
                if (!AttachConsole(ATTACH_PARENT_PROCESS))
                {
                    // If that fails, allocate a new console
                    AllocConsole();
                }
                
                // Redirect console output to make it work properly
                Console.SetOut(new StreamWriter(Console.OpenStandardOutput()) { AutoFlush = true });
                Console.SetError(new StreamWriter(Console.OpenStandardError()) { AutoFlush = true });
                
                Console.WriteLine("[DEBUG] App starting up...");
                
                // Ensure only one instance is running
                var currentProcess = System.Diagnostics.Process.GetCurrentProcess();
                var processes = System.Diagnostics.Process.GetProcessesByName(currentProcess.ProcessName);
                
                if (processes.Length > 1)
                {
                    // Another instance is already running
                    MessageBox.Show("Camera Overlay is already running!", "Camera Overlay", 
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    Current.Shutdown();
                    return;
                }
                
                base.OnStartup(e);
                
                // Create and show main window
                Console.WriteLine("[DEBUG] App.OnStartup - Creating MainWindow...");
                MainWindow mainWindow = new MainWindow();
                Console.WriteLine("[DEBUG] App.OnStartup - Showing MainWindow...");
                mainWindow.Show();
                Console.WriteLine("[DEBUG] App.OnStartup - MainWindow.Show() completed");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CRITICAL ERROR] App.OnStartup failed: {ex.Message}");
                Console.WriteLine($"[CRITICAL ERROR] Stack trace: {ex.StackTrace}");
                
                MessageBox.Show($"Critical error starting Camera Overlay:\n\n{ex.Message}", 
                    "Camera Overlay Startup Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    
                Current.Shutdown();
            }
        }
        
        protected override void OnExit(ExitEventArgs e)
        {
            Console.WriteLine("[DEBUG] App.OnExit - Application shutting down");
            FreeConsole();
            base.OnExit(e);
        }
    }
}
