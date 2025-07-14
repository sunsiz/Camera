using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Runtime.InteropServices;
using System.ComponentModel;
using System.IO;
using System.Text.Json;

namespace CameraOverlay
{
    public partial class MainWindow : Window
    {
        private OpenCVCameraCapture cameraCapture;
        private Point dragStartPoint;
        private CameraSettings settings;
        private readonly string settingsPath = "camera_settings.json";

        // Win32 API imports for always-on-top
        [DllImport("user32.dll")]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, uint uFlags);

        private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        private const uint SWP_NOSIZE = 0x0001;
        private const uint SWP_NOMOVE = 0x0002;

        // Add fields for aspect ratio control
        private bool keepAspectRatio = true;
        private double originalAspectRatio = 4.0 / 3.0; // Default 4:3 ratio

        public MainWindow()
        {
            try
            {
                Console.WriteLine("[DEBUG] MainWindow constructor starting...");
                
                InitializeComponent();
                LoadSettings();
                SetupWindow();
                
                // Wire up event handlers programmatically
                this.MouseLeftButtonDown += Window_MouseLeftButtonDown;
                this.MouseRightButtonDown += Window_MouseRightButtonDown;
                this.Loaded += Window_Loaded;
                this.Closing += Window_Closing;
                this.SizeChanged += Window_SizeChanged;
                
                Console.WriteLine("[DEBUG] MainWindow constructor - InitializeComponent completed");
                
                // Initialize camera capture asynchronously
                _ = InitializeCameraAsync();
                
                Console.WriteLine("[DEBUG] MainWindow constructor completed successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CRITICAL ERROR] MainWindow constructor failed: {ex.Message}");
                Console.WriteLine($"[CRITICAL ERROR] Stack trace: {ex.StackTrace}");
                throw;
            }
        }

        private void SetupWindow()
        {
            Console.WriteLine("[DEBUG] SetupWindow starting...");
            
            // Apply saved settings (with defaults)
            Left = settings?.WindowLeft ?? 200;
            Top = settings?.WindowTop ?? 200;
            Width = settings?.WindowWidth ?? 400;
            Height = settings?.WindowHeight ?? 300;

            // Set window properties for overlay mode
            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            Background = Brushes.Transparent;
            Topmost = true;
            ShowInTaskbar = false;
            
            // Add resize grip
            ResizeMode = ResizeMode.CanResizeWithGrip;
            
            // Add drop shadow effect
            Effect = new System.Windows.Media.Effects.DropShadowEffect
            {
                Color = Colors.Black,
                BlurRadius = 10,
                ShadowDepth = 5,
                Opacity = 0.5
            };
            
            Console.WriteLine("[DEBUG] SetupWindow completed");
            
            // Prevent Game Bar from detecting this as a game
            _ = Task.Run(async () =>
            {
                try
                {
                    // Clear any existing Game Bar game memory
                    await GameBarHelper.ClearGameBarGameMemory();
                    
                    // Wait for window to be fully loaded before applying Game Bar prevention
                    await Task.Delay(1000);
                    await GameBarHelper.PreventGameBarDetection(this);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[DEBUG] Game Bar prevention failed: {ex.Message}");
                }
            });
        }

        private async Task InitializeCameraAsync()
        {
            try
            {
                Console.WriteLine("[DEBUG] InitializeCameraAsync starting...");
                
                // Create camera capture element
                cameraCapture = new OpenCVCameraCapture
                {
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    VerticalAlignment = VerticalAlignment.Stretch
                };
                
                Console.WriteLine("[DEBUG] OpenCVCameraCapture instance created");
                
                // Add to main grid
                Grid mainGrid = this.FindName("MainGrid") as Grid;
                if (mainGrid != null)
                {
                    Console.WriteLine("[DEBUG] Found MainGrid, clearing and adding camera capture");
                    mainGrid.Children.Clear();
                    mainGrid.Children.Add(cameraCapture);
                }
                else
                {
                    Console.WriteLine("[WARNING] MainGrid not found, creating new grid");
                    // Create grid if it doesn't exist
                    var grid = new Grid();
                    grid.Children.Add(cameraCapture);
                    this.Content = grid;
                }
                
                Console.WriteLine("[DEBUG] Camera capture element added to grid");
                
                // Get available camera count
                Console.WriteLine("[DEBUG] Checking for available cameras...");
                int cameraCount = await OpenCVCameraCapture.GetAvailableCameraCountAsync();
                Console.WriteLine($"[DEBUG] Available cameras: {cameraCount}");
                
                if (cameraCount > 0)
                {
                    // Start camera capture with default settings
                    int cameraIndex = 0;  // Default to first camera
                    int width = 640;      // Default resolution
                    int height = 480;
                    
                    // Set the original aspect ratio based on camera resolution
                    originalAspectRatio = (double)width / height;
                    
                    Console.WriteLine($"[DEBUG] Starting camera capture: index={cameraIndex}, resolution={width}x{height}, aspect ratio={originalAspectRatio:F2}");
                    
                    bool success = await cameraCapture.StartCaptureAsync(cameraIndex, width, height);
                    
                    if (success)
                    {
                        Console.WriteLine("[DEBUG] ✓ Camera capture started successfully - you should see camera feed!");
                    }
                    else
                    {
                        Console.WriteLine("[ERROR] ✗ Failed to start camera capture");
                        AddErrorMessage("Failed to start camera");
                    }
                }
                else
                {
                    Console.WriteLine("[WARNING] ⚠ No cameras available");
                    AddErrorMessage("No cameras detected\n\nPlease check:\n• Camera is connected\n• Camera drivers are installed\n• Camera is not in use by another app");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] InitializeCameraAsync failed: {ex.Message}");
                Console.WriteLine($"[ERROR] Stack trace: {ex.StackTrace}");
                AddErrorMessage($"Camera initialization failed:\n{ex.Message}");
            }
            finally
            {
                // Ensure camera window is visible for manual Game Bar recording
                try
                {
                    await GameBarHelper.EnsureCameraInRecording(this);
                    Console.WriteLine("[DEBUG] Camera window prepared for Game Bar recording");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[DEBUG] Failed to prepare camera for recording: {ex.Message}");
                }
            }
        }

        private void AddErrorMessage(string message)
        {
            Console.WriteLine($"[DEBUG] Adding error message to UI: {message}");
            
            var errorText = new TextBlock
            {
                Text = message,
                Foreground = Brushes.White,
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Background = Brushes.Red,
                Padding = new Thickness(10),
                TextAlignment = TextAlignment.Center,
                TextWrapping = TextWrapping.Wrap
            };
            
            Grid mainGrid = this.FindName("MainGrid") as Grid;
            if (mainGrid != null)
            {
                mainGrid.Children.Clear();
                mainGrid.Children.Add(errorText);
            }
            else
            {
                this.Content = errorText;
            }
        }

        private void LoadSettings()
        {
            try
            {
                if (File.Exists(settingsPath))
                {
                    string json = File.ReadAllText(settingsPath);
                    settings = JsonSerializer.Deserialize<CameraSettings>(json);
                    Console.WriteLine("[DEBUG] Settings loaded successfully");
                }
                else
                {
                    settings = new CameraSettings();
                    Console.WriteLine("[DEBUG] Using default settings");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Failed to load settings: {ex.Message}");
                settings = new CameraSettings();
            }
        }

        private void SaveSettings()
        {
            try
            {
                if (settings != null)
                {
                    settings.WindowLeft = Left;
                    settings.WindowTop = Top;
                    settings.WindowWidth = Width;
                    settings.WindowHeight = Height;
                    
                    string json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
                    File.WriteAllText(settingsPath, json);
                    Console.WriteLine("[DEBUG] Settings saved successfully");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Failed to save settings: {ex.Message}");
            }
        }

        // Event handlers
        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Console.WriteLine("[DEBUG] Left mouse button clicked - starting drag");
            if (e.ClickCount == 1)
            {
                dragStartPoint = e.GetPosition(this);
                this.DragMove();
            }
        }

        private void Window_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            Console.WriteLine("[DEBUG] Right mouse button clicked - showing context menu");
            Console.WriteLine($"[DEBUG] Camera capturing: {cameraCapture?.IsCapturing}");
            ShowContextMenu();
        }

        private void ShowContextMenu()
        {
            try
            {
                var contextMenu = new ContextMenu();
                
                // Camera selection menu (populate synchronously to avoid blocking)
                var cameraMenuItem = new MenuItem { Header = "Camera" };
                PopulateCameraMenuSync(cameraMenuItem);
                contextMenu.Items.Add(cameraMenuItem);
                
                // Note: Resolution menu removed - users can now freely resize the window
                // by dragging the resize grip without camera restart issues
                
                // Information menu item
                var infoMenuItem = new MenuItem 
                { 
                    Header = "💡 Tip: Drag corners to resize window",
                    IsEnabled = false // Make it look like informational text
                };
                contextMenu.Items.Add(infoMenuItem);
                
                // Separator
                contextMenu.Items.Add(new Separator());
                
                // Keep aspect ratio toggle
                var aspectRatioMenuItem = new MenuItem 
                { 
                    Header = "Keep Aspect Ratio",
                    IsCheckable = true,
                    IsChecked = true // Default to true
                };
                aspectRatioMenuItem.Click += AspectRatio_Click;
                contextMenu.Items.Add(aspectRatioMenuItem);
                
                // Separator
                contextMenu.Items.Add(new Separator());
                
                // Simple Game Bar Recording
                var recordingItem = new MenuItem 
                { 
                    Header = GameBarHelper.IsRecording ? "⏹️ Stop Recording" : "⏺️ Start Recording"
                };
                recordingItem.Click += async (s, e) => await ToggleRecording();
                contextMenu.Items.Add(recordingItem);
                
                // Game Bar Help (simplified)
                var gameBarHelpItem = new MenuItem { Header = "❓ Recording Help" };
                gameBarHelpItem.Click += (s, e) => GameBarHelper.ShowGameBarInfo();
                contextMenu.Items.Add(gameBarHelpItem);
                
                // Game Bar Diagnostics
                var diagnosticsItem = new MenuItem { Header = "🔍 Game Bar Diagnostics" };
                diagnosticsItem.Click += (s, e) => ShowGameBarDiagnostics();
                contextMenu.Items.Add(diagnosticsItem);
                
                // Separator
                contextMenu.Items.Add(new Separator());
                
                // Exit menu
                var exitMenuItem = new MenuItem { Header = "Exit" };
                exitMenuItem.Click += (s, e) => 
                {
                    Console.WriteLine("[DEBUG] Exit menu clicked - shutting down");
                    Application.Current.Shutdown();
                };
                contextMenu.Items.Add(exitMenuItem);
                
                contextMenu.IsOpen = true;
                Console.WriteLine("[DEBUG] Context menu opened");
                
                // Ensure camera keeps running
                if (cameraCapture != null && cameraCapture.IsCapturing)
                {
                    Console.WriteLine("[DEBUG] Camera still capturing after context menu opened");
                }
                
                // Add event handler for when context menu closes
                contextMenu.Closed += (s, e) =>
                {
                    Console.WriteLine("[DEBUG] Context menu closed");
                    if (cameraCapture != null && cameraCapture.IsCapturing)
                    {
                        Console.WriteLine("[DEBUG] Camera still capturing after context menu closed");
                    }
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] ShowContextMenu failed: {ex.Message}");
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("[DEBUG] Window_Loaded event - setting always on top");
            // Set always on top
            var handle = new System.Windows.Interop.WindowInteropHelper(this).Handle;
            SetWindowPos(handle, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE);
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            Console.WriteLine("[DEBUG] MainWindow closing...");
            
            SaveSettings();
            
            if (cameraCapture != null)
            {
                Console.WriteLine("[DEBUG] Disposing camera capture...");
                cameraCapture.StopCapture();
                cameraCapture.Dispose();
            }
            
            Console.WriteLine("[DEBUG] MainWindow closed");
        }

        private void PopulateCameraMenuSync(MenuItem cameraMenuItem)
        {
            try
            {
                Console.WriteLine("[DEBUG] Populating camera menu (sync)...");
                
                // Use a simpler approach - just show the current camera and a "Switch Camera" option
                // This avoids async operations that could interfere with camera capture
                
                if (cameraCapture != null && cameraCapture.IsCapturing)
                {
                    // Show current camera
                    var currentCameraItem = new MenuItem 
                    { 
                        Header = $"✓ Camera {cameraCapture.CameraIndex + 1} (Active)",
                        IsEnabled = false
                    };
                    cameraMenuItem.Items.Add(currentCameraItem);
                    
                    // Add separator
                    cameraMenuItem.Items.Add(new Separator());
                    
                    // Add switch options for first few cameras
                    for (int i = 0; i < 3; i++) // Check first 3 cameras
                    {
                        if (i != cameraCapture.CameraIndex) // Don't show current camera again
                        {
                            var switchItem = new MenuItem 
                            { 
                                Header = $"Switch to Camera {i + 1}"
                            };
                            
                            int cameraIndex = i; // Capture for closure
                            switchItem.Click += async (s, e) => await SwitchCamera(cameraIndex);
                            cameraMenuItem.Items.Add(switchItem);
                        }
                    }
                }
                else
                {
                    var noCameraItem = new MenuItem { Header = "No camera active", IsEnabled = false };
                    cameraMenuItem.Items.Add(noCameraItem);
                }
                
                Console.WriteLine("[DEBUG] Camera menu populated (sync)");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Failed to populate camera menu (sync): {ex.Message}");
                var errorItem = new MenuItem { Header = "Error loading cameras", IsEnabled = false };
                cameraMenuItem.Items.Add(errorItem);
            }
        }

        private async Task PopulateCameraMenu(MenuItem cameraMenuItem)
        {
            try
            {
                Console.WriteLine("[DEBUG] Populating camera menu...");
                
                // Get available camera count
                int cameraCount = await OpenCVCameraCapture.GetAvailableCameraCountAsync();
                
                if (cameraCount == 0)
                {
                    var noCameraItem = new MenuItem { Header = "No cameras detected", IsEnabled = false };
                    cameraMenuItem.Items.Add(noCameraItem);
                }
                else
                {
                    for (int i = 0; i < cameraCount; i++)
                    {
                        var cameraItem = new MenuItem 
                        { 
                            Header = $"Camera {i + 1}",
                            IsCheckable = true,
                            IsChecked = (cameraCapture?.CameraIndex == i)
                        };
                        
                        int cameraIndex = i; // Capture for closure
                        cameraItem.Click += async (s, e) => await SwitchCamera(cameraIndex);
                        cameraMenuItem.Items.Add(cameraItem);
                    }
                }
                
                Console.WriteLine($"[DEBUG] Camera menu populated with {cameraCount} cameras");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Failed to populate camera menu: {ex.Message}");
                var errorItem = new MenuItem { Header = "Error loading cameras", IsEnabled = false };
                cameraMenuItem.Items.Add(errorItem);
            }
        }

        private async Task SwitchCamera(int cameraIndex)
        {
            try
            {
                Console.WriteLine($"[DEBUG] Switching to camera {cameraIndex}...");
                
                if (cameraCapture != null)
                {
                    // Stop current camera
                    cameraCapture.StopCapture();
                    
                    // Start new camera with current resolution
                    bool success = await cameraCapture.StartCaptureAsync(cameraIndex, 
                        (int)this.Width, (int)this.Height);
                    
                    if (success)
                    {
                        Console.WriteLine($"[DEBUG] Successfully switched to camera {cameraIndex}");
                    }
                    else
                    {
                        Console.WriteLine($"[ERROR] Failed to switch to camera {cameraIndex}");
                        AddErrorMessage($"Failed to switch to camera {cameraIndex + 1}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] SwitchCamera failed: {ex.Message}");
                AddErrorMessage($"Error switching camera: {ex.Message}");
            }
        }

        private void AspectRatio_Click(object sender, RoutedEventArgs e)
        {
            var menuItem = sender as MenuItem;
            if (menuItem != null)
            {
                keepAspectRatio = menuItem.IsChecked;
                Console.WriteLine($"[DEBUG] Keep aspect ratio: {keepAspectRatio}");
                
                // Update window behavior
                UpdateWindowAspectRatio();
            }
        }

        private void UpdateWindowAspectRatio()
        {
            if (keepAspectRatio)
            {
                // Adjust window size to maintain aspect ratio
                double currentRatio = this.Width / this.Height;
                
                if (Math.Abs(currentRatio - originalAspectRatio) > 0.01)
                {
                    // Adjust height to match aspect ratio based on current width
                    this.Height = this.Width / originalAspectRatio;
                    Console.WriteLine($"[DEBUG] Adjusted window to maintain aspect ratio: {this.Width}x{this.Height}");
                }
            }
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (keepAspectRatio && originalAspectRatio > 0)
            {
                Console.WriteLine($"[DEBUG] Window size changed to {e.NewSize.Width}x{e.NewSize.Height}");
                
                // Calculate what the height should be based on the new width
                double targetHeight = e.NewSize.Width / originalAspectRatio;
                
                // Only adjust if the difference is significant (avoid infinite loops)
                if (Math.Abs(e.NewSize.Height - targetHeight) > 2)
                {
                    this.Height = targetHeight;
                    Console.WriteLine($"[DEBUG] Adjusted height to {targetHeight} to maintain aspect ratio {originalAspectRatio:F2}");
                }
            }
        }

        private async Task ToggleRecording()
        {
            try
            {
                Console.WriteLine("[DEBUG] Toggle recording clicked");
                
                if (GameBarHelper.IsRecording)
                {
                    Console.WriteLine("[DEBUG] Stopping Game Bar screen recording...");
                    bool success = await GameBarHelper.StopRecordingAsync();
                    
                    if (success)
                    {
                        AddStatusMessage("📹 Screen recording stopped");
                    }
                    else
                    {
                        AddErrorMessage("Failed to stop Game Bar recording.\nTry using Win+Alt+R manually.");
                    }
                }
                else
                {
                    Console.WriteLine("[DEBUG] Starting Game Bar screen recording...");
                    
                    // Pass this window to GameBar helper so it can hide it during recording startup
                    bool success = await GameBarHelper.StartRecordingAsync(this);
                    
                    if (success)
                    {
                        AddStatusMessage("🔴 Game Bar recording started!\nRecording entire desktop including camera overlay.\n\nNOTE: Camera window hidden completely during startup to prevent Game Bar from detecting it as a 'game'.\nWindow will reappear and be visible in recording.");
                    }
                    else
                    {
                        AddErrorMessage("Failed to start Game Bar recording.\n\nTROUBLESHOOTING:\n1. Check 🔍 Game Bar Diagnostics for issues\n2. If error 0x8232360F occurs, restart application\n3. Ensure Game Bar is enabled in Settings\n4. Try Win+Alt+R manually\n5. Check if camera is detected as 'game'");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] ToggleRecording failed: {ex.Message}");
                AddErrorMessage($"Error with Game Bar recording: {ex.Message}");
            }
        }

        private async Task OpenGameBar()
        {
            try
            {
                Console.WriteLine("[DEBUG] Opening Game Bar...");
                bool success = await GameBarHelper.OpenGameBarAsync();
                
                if (success)
                {
                    AddStatusMessage("🎮 Game Bar opened!\nYou can now access recording controls.");
                }
                else
                {
                    AddErrorMessage("Failed to open Game Bar.\nTry using Win+G manually.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] OpenGameBar failed: {ex.Message}");
                AddErrorMessage($"Error opening Game Bar: {ex.Message}");
            }
        }

        private void AddStatusMessage(string message)
        {
            try
            {
                // Show a temporary status message (you could enhance this with a status bar or tooltip)
                Console.WriteLine($"[STATUS] {message}");
                
                // For now, just show in title briefly
                var originalTitle = this.Title;
                this.Title = message;
                
                // Reset title after 3 seconds
                Task.Delay(3000).ContinueWith(_ =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        if (this.Title == message) // Only reset if it hasn't changed
                        {
                            this.Title = originalTitle;
                        }
                    });
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] AddStatusMessage failed: {ex.Message}");
            }
        }

        private void ShowGameBarDiagnostics()
        {
            try
            {
                string diagnostics = GameBarHelper.GetGameBarDiagnostics();
                MessageBox.Show(diagnostics, "Game Bar Diagnostics", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error getting diagnostics: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
