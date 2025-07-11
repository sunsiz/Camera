using System.Windows;

namespace CameraOverlay
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
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
            MainWindow mainWindow = new MainWindow();
            mainWindow.Show();
        }
    }
}
