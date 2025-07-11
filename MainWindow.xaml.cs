using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Runtime.InteropServices;
using System.Windows.Threading;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text.Json;

namespace CameraOverlay
{
    public partial class MainWindow : Window
    {
        private VideoCaptureElement videoCaptureElement;
        private bool isDragging = false;
        private Point dragStartPoint;
        private DispatcherTimer saveTimer;
        private string deferredResolution;
        private const string SettingsFile = "camera_settings.json";

        // Win32 API constants for always on top
        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_TOPMOST = 0x00000008;
        private const int WS_EX_TOOLWINDOW = 0x00000080;

        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
        
        [DllImport("kernel32.dll")]
        private static extern IntPtr GetConsoleWindow();
        
        [DllImport("user32.dll")]
        private static extern bool IsWindowVisible(IntPtr hWnd);

        public MainWindow()
        {
            // Show debug console for detailed logging
            DebugConsole.ShowConsole();
            Console.WriteLine("[DEBUG] MainWindow constructor started");
            
            InitializeComponent();
            LoadSettings();
            SetupWindow();
            InitializeCamera();
            SetupSaveTimer();
            
            // Apply deferred settings after everything is initialized
            if (!string.IsNullOrEmpty(deferredResolution))
            {
                videoCaptureElement.SetResolution(deferredResolution);
                ApplyResolutionToWindow(deferredResolution);
                deferredResolution = null;
            }
        }

        private void SetupWindow()
        {
            // Remove window chrome
            this.WindowStyle = WindowStyle.None;
            this.ResizeMode = ResizeMode.CanResizeWithGrip;
            this.AllowsTransparency = true;
            this.Background = Brushes.Transparent;
            this.Topmost = true;

            // Add drop shadow effect
            this.Effect = new System.Windows.Media.Effects.DropShadowEffect
            {
                Color = Colors.Black,
                Direction = 315,
                ShadowDepth = 5,
                Opacity = 0.5,
                BlurRadius = 10
            };

            // Set window to always on top using Win32 API
            this.Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            IntPtr hWnd = new WindowInteropHelper(this).Handle;
            int exStyle = GetWindowLong(hWnd, GWL_EXSTYLE);
            SetWindowLong(hWnd, GWL_EXSTYLE, exStyle | WS_EX_TOPMOST);
        }

        private void InitializeCamera()
        {
            videoCaptureElement = new VideoCaptureElement();
            MainGrid.Children.Add(videoCaptureElement);
        }

        private void SetupSaveTimer()
        {
            saveTimer = new DispatcherTimer();
            saveTimer.Interval = TimeSpan.FromSeconds(2);
            saveTimer.Tick += SaveTimer_Tick;
        }

        private void SaveTimer_Tick(object sender, EventArgs e)
        {
            SaveSettings();
            saveTimer.Stop();
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                isDragging = true;
                dragStartPoint = e.GetPosition(this);
                this.DragMove();
                
                // Start timer to save position
                saveTimer.Stop();
                saveTimer.Start();
            }
        }

        private void Window_MouseMove(object sender, MouseEventArgs e)
        {
            if (isDragging && e.LeftButton == MouseButtonState.Pressed)
            {
                // Window is being dragged
                saveTimer.Stop();
                saveTimer.Start();
            }
        }

        private void Window_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            isDragging = false;
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            // Refresh camera display to show new size
            videoCaptureElement?.RefreshDisplay();
            
            // Start timer to save size
            saveTimer.Stop();
            saveTimer.Start();
        }

        private void Window_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            ShowContextMenu();
        }

        private void ShowContextMenu()
        {
            ContextMenu contextMenu = new ContextMenu();
            
            // Camera selection
            MenuItem cameraMenuItem = new MenuItem { Header = "Select Camera" };
            var cameras = videoCaptureElement.GetAvailableCameras();
            
            foreach (var camera in cameras)
            {
                MenuItem cameraItem = new MenuItem { Header = camera.Name, Tag = camera };
                cameraItem.Click += (s, e) => {
                    videoCaptureElement.SelectCamera(camera);
                    SaveSettings();
                };
                cameraMenuItem.Items.Add(cameraItem);
            }
            contextMenu.Items.Add(cameraMenuItem);

            // Resolution selection
            MenuItem resolutionMenuItem = new MenuItem { Header = "Resolution" };
            var resolutions = videoCaptureElement.GetAvailableResolutions();
            
            foreach (var resolution in resolutions)
            {
                MenuItem resItem = new MenuItem { Header = resolution, Tag = resolution };
                resItem.Click += (s, e) => {
                    videoCaptureElement.SetResolution(resolution);
                    ApplyResolutionToWindow(resolution);
                    SaveSettings();
                };
                resolutionMenuItem.Items.Add(resItem);
            }
            contextMenu.Items.Add(resolutionMenuItem);

            contextMenu.Items.Add(new Separator());
            
            // Debug Console toggle
            MenuItem debugMenuItem = new MenuItem { Header = "Toggle Debug Console" };
            debugMenuItem.Click += (s, e) => {
                // Check if console is visible (simplified check)
                IntPtr consoleWindow = GetConsoleWindow();
                if (consoleWindow != IntPtr.Zero && IsWindowVisible(consoleWindow))
                {
                    DebugConsole.HideConsole();
                }
                else
                {
                    DebugConsole.ShowConsole();
                }
            };
            contextMenu.Items.Add(debugMenuItem);

            contextMenu.Items.Add(new Separator());

            // Exit
            MenuItem exitMenuItem = new MenuItem { Header = "Exit" };
            exitMenuItem.Click += (s, e) => {
                SaveSettings();
                Application.Current.Shutdown();
            };
            contextMenu.Items.Add(exitMenuItem);

            contextMenu.IsOpen = true;
        }

        private void SaveSettings()
        {
            try
            {
                var settings = new CameraSettings
                {
                    WindowLeft = this.Left,
                    WindowTop = this.Top,
                    WindowWidth = this.Width,
                    WindowHeight = this.Height,
                    SelectedCameraName = videoCaptureElement.CurrentCamera?.Name,
                    SelectedResolution = videoCaptureElement.CurrentResolution
                };

                string json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(SettingsFile, json);
            }
            catch (Exception ex)
            {
                // Silently fail - settings are not critical
                System.Diagnostics.Debug.WriteLine($"Failed to save settings: {ex.Message}");
            }
        }

        private void LoadSettings()
        {
            try
            {
                if (File.Exists(SettingsFile))
                {
                    string json = File.ReadAllText(SettingsFile);
                    var settings = JsonSerializer.Deserialize<CameraSettings>(json);

                    if (settings != null)
                    {
                        // Restore window position and size
                        this.Left = settings.WindowLeft;
                        this.Top = settings.WindowTop;
                        this.Width = settings.WindowWidth;
                        this.Height = settings.WindowHeight;
                        
                        // Restore camera settings
                        if (!string.IsNullOrEmpty(settings.SelectedResolution))
                        {
                            deferredResolution = settings.SelectedResolution;
                        }
                        
                        // Ensure window is visible on screen
                        EnsureWindowVisible();
                    }
                }
            }
            catch (Exception ex)
            {
                // Silently fail - use default settings
                System.Diagnostics.Debug.WriteLine($"Failed to load settings: {ex.Message}");
                SetDefaultSize();
            }
        }

        private void SetDefaultSize()
        {
            this.Width = 320;
            this.Height = 240;
            this.Left = SystemParameters.WorkArea.Width - this.Width - 20;
            this.Top = 20;
        }

        private void EnsureWindowVisible()
        {
            // Make sure window is visible on screen
            if (this.Left < 0) this.Left = 0;
            if (this.Top < 0) this.Top = 0;
            if (this.Left + this.Width > SystemParameters.WorkArea.Width)
                this.Left = SystemParameters.WorkArea.Width - this.Width;
            if (this.Top + this.Height > SystemParameters.WorkArea.Height)
                this.Top = SystemParameters.WorkArea.Height - this.Height;
        }

        private void ApplyResolutionToWindow(string resolution)
        {
            if (string.IsNullOrEmpty(resolution)) return;
            
            try
            {
                // Parse resolution (e.g., "1280x720")
                var parts = resolution.Split('x');
                if (parts.Length == 2 && 
                    int.TryParse(parts[0], out int width) && 
                    int.TryParse(parts[1], out int height))
                {
                    // Apply resolution to window size
                    this.Width = width;
                    this.Height = height;
                    
                    // Ensure window stays on screen
                    EnsureWindowVisible();
                    
                    // Refresh display to show new size
                    videoCaptureElement?.RefreshDisplay();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error applying resolution: {ex.Message}");
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            SaveSettings();
            videoCaptureElement?.Dispose();
            base.OnClosed(e);
        }
    }
}
