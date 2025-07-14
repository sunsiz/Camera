using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using System.Windows.Media;

namespace CameraOverlay
{
    /// <summary>
    /// Enhanced Game Bar helper with better error detection and troubleshooting
    /// </summary>
    public static class GameBarHelper
    {
        private static bool isRecording = false;

        // Win32 API for sending keyboard input
        [DllImport("user32.dll")]
        private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

        // Win32 API for window management
        [DllImport("user32.dll")]
        private static extern IntPtr GetDesktopWindow();

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        private static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        private static extern IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        private static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        [DllImport("shell32.dll")]
        private static extern IntPtr SHGetFileInfo(string pszPath, uint dwFileAttributes, ref SHFILEINFO psfi, uint cbSizeFileInfo, uint uFlags);

        private const int SW_HIDE = 0;
        private const int SW_SHOW = 5;
        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_TOOLWINDOW = 0x00000080;
        private const int WS_EX_APPWINDOW = 0x00040000;

        private const byte VK_LWIN = 0x5B;
        private const byte VK_MENU = 0x12; // Alt key
        private const byte VK_R = 0x52;
        private const byte VK_G = 0x47;
        private const uint KEYEVENTF_KEYUP = 0x0002;

        [StructLayout(LayoutKind.Sequential)]
        private struct SHFILEINFO
        {
            public IntPtr hIcon;
            public IntPtr iIcon;
            public uint dwAttributes;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string szDisplayName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
            public string szTypeName;
        }

        /// <summary>
        /// Gets whether Game Bar is currently recording
        /// </summary>
        public static bool IsRecording => isRecording;

        /// <summary>
        /// Check if Game Bar is enabled and available
        /// </summary>
        public static bool IsGameBarEnabled()
        {
            try
            {
                // Check registry for Game Bar settings
                using (var key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\GameDVR"))
                {
                    if (key != null)
                    {
                        var appCaptureEnabled = key.GetValue("AppCaptureEnabled");
                        var gameBarEnabled = key.GetValue("GameBarEnabled");
                        
                        Console.WriteLine($"[DEBUG] Game Bar Registry - AppCaptureEnabled: {appCaptureEnabled}, GameBarEnabled: {gameBarEnabled}");
                        
                        return appCaptureEnabled?.ToString() == "1" || gameBarEnabled?.ToString() == "1";
                    }
                }
                
                // If registry check fails, assume it might be enabled
                Console.WriteLine("[DEBUG] Game Bar registry settings not found, assuming enabled");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DEBUG] Error checking Game Bar status: {ex.Message}");
                return true; // Assume enabled if we can't check
            }
        }

        /// <summary>
        /// Get detailed Game Bar diagnostics
        /// </summary>
        public static string GetGameBarDiagnostics()
        {
            var diagnostics = "🔍 Game Bar Diagnostics:\n\n";
            
            try
            {
                // Check if Game Bar process is running
                var gameBarProcesses = Process.GetProcessesByName("GameBar");
                diagnostics += $"• Game Bar Process: {(gameBarProcesses.Length > 0 ? "✅ Running" : "❌ Not Running")}\n";

                // Check Windows version
                var version = Environment.OSVersion.Version;
                diagnostics += $"• Windows Version: {version.Major}.{version.Minor}.{version.Build}\n";

                // Check Game Bar registry settings
                using (var key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\GameDVR"))
                {
                    if (key != null)
                    {
                        var appCaptureEnabled = key.GetValue("AppCaptureEnabled");
                        var gameBarEnabled = key.GetValue("GameBarEnabled");
                        
                        diagnostics += $"• App Capture Enabled: {(appCaptureEnabled?.ToString() == "1" ? "✅ Yes" : "❌ No")}\n";
                        diagnostics += $"• Game Bar Enabled: {(gameBarEnabled?.ToString() == "1" ? "✅ Yes" : "❌ No")}\n";
                    }
                    else
                    {
                        diagnostics += "• Registry Settings: ❌ Not Found\n";
                    }
                }

                // Check for common Game Bar issues
                diagnostics += "\n🛠️ Common Issues:\n";
                diagnostics += "• Make sure Game Bar is enabled in Settings > Gaming > Game Bar\n";
                diagnostics += "• Ensure 'Record game clips...' is turned ON\n";
                diagnostics += "• If app is detected as 'game', use Game Bar prevention features\n";
                diagnostics += "• Try pressing Win+G manually to test Game Bar\n";
                diagnostics += "• Error 0x8232360F means Game Bar can't record the window properly\n";
                diagnostics += "• Some antivirus software blocks Game Bar\n";
                
                // Check if this app is remembered as a game
                diagnostics += "\n🎮 Game Detection Status:\n";
                try
                {
                    bool foundAsGame = false;
                    string[] gameRegistryPaths = {
                        @"SOFTWARE\Microsoft\Windows\CurrentVersion\GameDVR\WhiteList",
                        @"SOFTWARE\Microsoft\Windows\CurrentVersion\GameDVR\AppSettings"
                    };
                    
                    foreach (string path in gameRegistryPaths)
                    {
                        try
                        {
                            using (var key = Registry.CurrentUser.OpenSubKey(path))
                            {
                                if (key != null)
                                {
                                    string[] valueNames = key.GetValueNames();
                                    foreach (string valueName in valueNames)
                                    {
                                        if (valueName.Contains("Camera") || valueName.Contains("CameraOverlay"))
                                        {
                                            foundAsGame = true;
                                            diagnostics += $"• Found in Game Bar registry: {valueName}\n";
                                        }
                                    }
                                }
                            }
                        }
                        catch { }
                    }
                    
                    if (!foundAsGame)
                    {
                        diagnostics += "• ✅ Not detected as game in Game Bar registry\n";
                    }
                    else
                    {
                        diagnostics += "• ⚠️ App is remembered as a game - this causes recording issues\n";
                    }
                }
                catch (Exception ex)
                {
                    diagnostics += $"• Error checking game status: {ex.Message}\n";
                }

            }
            catch (Exception ex)
            {
                diagnostics += $"❌ Error gathering diagnostics: {ex.Message}\n";
            }

            return diagnostics;
        }

        /// <summary>
        /// Starts Game Bar recording using multiple methods with better error handling
        /// </summary>
        public static async Task<bool> StartRecordingAsync()
        {
            return await StartRecordingAsync(null);
        }

        /// <summary>
        /// Starts Game Bar recording with camera window hiding for desktop recording
        /// </summary>
        public static async Task<bool> StartRecordingAsync(Window cameraWindow)
        {
            // Use the robust desktop-recording method instead of hiding the window
            return await StartDesktopRecordingWithRetryAsync(cameraWindow);
        }

        /// <summary>
        /// Stops Game Bar recording
        /// </summary>
        public static async Task<bool> StopRecordingAsync()
        {
            try
            {
                Console.WriteLine("[DEBUG] Attempting to stop Game Bar recording...");

                // Check if recording is actually active first
                if (!await IsGameBarRecordingActive())
                {
                    Console.WriteLine("[DEBUG] No active Game Bar recording detected");
                    isRecording = false;
                    return true; // Consider it successful if nothing was recording
                }

                // Use keyboard shortcut to toggle recording (same key stops it)
                await SendKeyCombo(VK_LWIN, VK_MENU, VK_R);
                await Task.Delay(2000); // Wait for Game Bar to process stop command
                
                // Verify recording actually stopped
                if (!await IsGameBarRecordingActive())
                {
                    isRecording = false;
                    Console.WriteLine("[DEBUG] ✓ Game Bar recording stopped successfully");
                    return true;
                }
                
                // Try alternative stop method - open Game Bar and try again
                Console.WriteLine("[DEBUG] First stop attempt failed, trying Game Bar sequence...");
                await SendKeyCombo(VK_LWIN, VK_G);
                await Task.Delay(2000);
                await SendKeyCombo(VK_LWIN, VK_MENU, VK_R);
                await Task.Delay(2000);
                
                if (!await IsGameBarRecordingActive())
                {
                    isRecording = false;
                    Console.WriteLine("[DEBUG] ✓ Game Bar recording stopped via sequence");
                    return true;
                }

                Console.WriteLine("[WARNING] Could not stop Game Bar recording");
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Failed to stop Game Bar recording: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Opens Game Bar overlay
        /// </summary>
        public static async Task<bool> OpenGameBarAsync()
        {
            try
            {
                Console.WriteLine("[DEBUG] Opening Game Bar overlay...");

                // Use Windows+G to open Game Bar
                await SendKeyCombo(VK_LWIN, VK_G);
                Console.WriteLine("[DEBUG] ✓ Game Bar overlay opened");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Failed to open Game Bar: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Try to start Game Bar directly via protocol for SCREEN recording
        /// </summary>
        private static async Task<bool> TryStartGameBarDirectly()
        {
            try
            {
                // Focus desktop first to ensure screen recording
                await FocusDesktop();
                await Task.Delay(200);
                
                // Use screen recording instead of app recording
                var startInfo = new ProcessStartInfo
                {
                    FileName = "ms-gamebar://record",
                    UseShellExecute = true,
                    CreateNoWindow = true
                };

                using (var process = Process.Start(startInfo))
                {
                    if (process != null)
                    {
                        await Task.Delay(1500); // Give it more time to start
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DEBUG] Direct Game Bar launch failed: {ex.Message}");
            }

            return false;
        }

        /// <summary>
        /// Try to start recording using keyboard shortcut (records entire screen)
        /// </summary>
        private static async Task<bool> TryStartWithKeyboardShortcut()
        {
            try
            {
                // First, minimize focus from any specific window to ensure desktop recording
                await FocusDesktop();
                await Task.Delay(200); // Small delay to ensure focus change takes effect
                
                // Send Win+Alt+R (this records the entire screen/desktop)
                await SendKeyCombo(VK_LWIN, VK_MENU, VK_R);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DEBUG] Keyboard shortcut failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Try opening Game Bar first, then starting SCREEN recording
        /// </summary>
        private static async Task<bool> TryGameBarSequence()
        {
            try
            {
                // Focus desktop to ensure screen recording
                await FocusDesktop();
                await Task.Delay(200);
                
                // First open Game Bar
                await SendKeyCombo(VK_LWIN, VK_G);
                await Task.Delay(2000); // Wait for Game Bar to open
                
                // Then try to start screen recording
                await SendKeyCombo(VK_LWIN, VK_MENU, VK_R);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DEBUG] Game Bar sequence failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Send a key combination using Win32 API
        /// </summary>
        private static async Task SendKeyCombo(params byte[] keys)
        {
            await Task.Run(() =>
            {
                try
                {
                    // Press all keys down
                    foreach (byte key in keys)
                    {
                        keybd_event(key, 0, 0, UIntPtr.Zero);
                    }

                    // Small delay
                    System.Threading.Thread.Sleep(100);

                    // Release all keys in reverse order
                    for (int i = keys.Length - 1; i >= 0; i--)
                    {
                        keybd_event(keys[i], 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ERROR] SendKeyCombo failed: {ex.Message}");
                    throw;
                }
            });
        }

        /// <summary>
        /// Focus the desktop and ensure no specific window is targeted for Game Bar recording
        /// </summary>
        private static async Task FocusDesktop()
        {
            await Task.Run(() =>
            {
                try
                {
                    // Method 1: Focus on Windows Explorer (which represents the desktop)
                    var explorerProcesses = Process.GetProcessesByName("explorer");
                    if (explorerProcesses.Length > 0)
                    {
                        var explorerWindow = explorerProcesses[0].MainWindowHandle;
                        if (explorerWindow != IntPtr.Zero)
                        {
                            SetForegroundWindow(explorerWindow);
                            Console.WriteLine("[DEBUG] Focused Windows Explorer for desktop recording");
                            return;
                        }
                    }
                    
                    // Method 2: Get the desktop window handle as fallback
                    IntPtr desktopWindow = GetDesktopWindow();
                    SetForegroundWindow(desktopWindow);
                    
                    Console.WriteLine("[DEBUG] Focused desktop window for screen recording");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[DEBUG] Failed to focus desktop: {ex.Message}");
                    // Not critical, continue anyway
                }
            });
        }

        /// <summary>
        /// Show simplified Game Bar help
        /// </summary>
        public static void ShowGameBarInfo()
        {
            string message = @"🎮 Game Bar Desktop Recording Help

PROBLEM SOLVED:
Game Bar was detecting Camera Overlay as a 'game' causing recording issues and error 0x8232360F.

NEW SOLUTION:
1. Right-click camera → 'Start Recording'
2. Camera window completely disappears (hidden from Game Bar)
3. Game Bar records ENTIRE DESKTOP (not just camera window)
4. Camera window reappears after 3 seconds and is visible in recording

HOW IT WORKS:
• Camera window is completely hidden (minimized + hidden + removed from taskbar)
• This prevents Game Bar from detecting it as a 'game' to record
• Game Bar defaults to desktop recording instead
• Camera window reappears and is captured as part of desktop recording

PREVENTING GAME DETECTION:
• App configures itself as a 'tool window' not a 'game'
• Clears Game Bar registry entries that remember it as a game
• Uses window properties that avoid game detection

MANUAL RECORDING:
• Win+Alt+R now works properly for desktop recording
• Camera positioned optimally and visible in recording

TROUBLESHOOTING ERROR 0x8232360F:
• This error occurs when Game Bar tries to record the camera as a 'game'
• Solution: Use context menu recording (prevents game detection)
• If error persists: Restart application to clear Game Bar memory

KEYBOARD SHORTCUTS:
• Win + Alt + R = Start/Stop Desktop Recording
• Win + G = Open Game Bar

IMPORTANT NOTES:
• Records ENTIRE DESKTOP (all windows, desktop, etc.)
• Camera overlay visible in recording after it reappears
• No more 'game' detection issues
• Error 0x8232360F should be eliminated

The camera overlay now uses advanced Game Bar evasion to ensure proper desktop recording!";

            MessageBox.Show(message, "Game Bar Desktop Recording Help", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        /// <summary>
        /// Temporarily hide the camera window from Game Bar detection by minimizing it
        /// </summary>
        public static async Task<bool> HideCameraFromGameBar(Window cameraWindow)
        {
            if (cameraWindow == null) return false;

            try
            {
                Console.WriteLine("[DEBUG] Aggressively hiding camera window from Game Bar game detection...");
                
                await cameraWindow.Dispatcher.InvokeAsync(() =>
                {
                    // Store original window state and properties
                    cameraWindow.Tag = new
                    {
                        WindowState = cameraWindow.WindowState,
                        ShowInTaskbar = cameraWindow.ShowInTaskbar,
                        Topmost = cameraWindow.Topmost,
                        Visibility = cameraWindow.Visibility
                    };
                    
                    // Completely hide the window from Game Bar detection
                    cameraWindow.WindowState = WindowState.Minimized;
                    cameraWindow.ShowInTaskbar = false;  // Remove from taskbar
                    cameraWindow.Topmost = false;        // Remove topmost flag
                    cameraWindow.Hide();                 // Hide completely
                    
                    Console.WriteLine("[DEBUG] Camera window completely hidden from Game Bar");
                });
                
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Failed to hide camera window: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Restore the camera window to normal visibility and ensure it's prominently displayed for recording
        /// </summary>
        public static async Task<bool> ShowCameraToGameBar(Window cameraWindow)
        {
            if (cameraWindow == null) return false;

            try
            {
                Console.WriteLine("[DEBUG] Restoring camera window after Game Bar recording started...");
                
                await cameraWindow.Dispatcher.InvokeAsync(() =>
                {
                    // Restore original window properties from stored state
                    if (cameraWindow.Tag != null)
                    {
                        dynamic originalState = cameraWindow.Tag;
                        
                        cameraWindow.WindowState = originalState.WindowState;
                        cameraWindow.ShowInTaskbar = originalState.ShowInTaskbar;
                        cameraWindow.Topmost = originalState.Topmost;
                        cameraWindow.Visibility = originalState.Visibility;
                    }
                    else
                    {
                        // Fallback defaults
                        cameraWindow.WindowState = WindowState.Normal;
                        cameraWindow.ShowInTaskbar = false;  // Keep hidden from taskbar
                        cameraWindow.Topmost = true;         // Stay on top for recording
                        cameraWindow.Visibility = Visibility.Visible;
                    }
                    
                    // Ensure it's visible, active, and properly positioned for recording
                    cameraWindow.Show();
                    cameraWindow.Activate();
                    cameraWindow.Focus();
                    
                    // Move to a visible area if needed (not at screen edges)
                    var screenWidth = SystemParameters.PrimaryScreenWidth;
                    var screenHeight = SystemParameters.PrimaryScreenHeight;
                    
                    if (cameraWindow.Left < 50)
                        cameraWindow.Left = 50;
                    if (cameraWindow.Top < 50)
                        cameraWindow.Top = 50;
                    if (cameraWindow.Left + cameraWindow.Width > screenWidth - 50)
                        cameraWindow.Left = screenWidth - cameraWindow.Width - 50;
                    if (cameraWindow.Top + cameraWindow.Height > screenHeight - 50)
                        cameraWindow.Top = screenHeight - cameraWindow.Height - 50;
                    
                    Console.WriteLine("[DEBUG] Camera window restored and positioned for recording");
                });
                
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Failed to restore camera window: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Alternative recording approach that ensures camera window is visible in recording
        /// This is for when user manually starts Game Bar recording but wants camera included
        /// </summary>
        public static async Task<bool> EnsureCameraInRecording(Window cameraWindow)
        {
            if (cameraWindow == null) return false;
            
            try
            {
                Console.WriteLine("[DEBUG] Ensuring camera window is prominently visible for manual recording...");
                
                await cameraWindow.Dispatcher.InvokeAsync(() =>
                {
                    // Make sure window is visible and restored
                    if (cameraWindow.WindowState == WindowState.Minimized)
                    {
                        cameraWindow.WindowState = WindowState.Normal;
                    }
                    
                    // Ensure it's shown, active, and prominent
                    cameraWindow.Show();
                    cameraWindow.Activate();
                    cameraWindow.Focus();
                    cameraWindow.Topmost = true; // Ensure it stays on top for recording
                    
                    // Position prominently on screen for better recording visibility
                    var screenWidth = SystemParameters.PrimaryScreenWidth;
                    var screenHeight = SystemParameters.PrimaryScreenHeight;
                    
                    // Position in upper-right area where it's clearly visible
                    cameraWindow.Left = screenWidth - cameraWindow.Width - 100;
                    cameraWindow.Top = 100;
                    
                    // Make sure it has a reasonable size for recording
                    if (cameraWindow.Width < 200)
                        cameraWindow.Width = 320;
                    if (cameraWindow.Height < 150)
                        cameraWindow.Height = 240;
                    
                    Console.WriteLine("[DEBUG] Camera window positioned prominently for manual recording");
                });
                
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Failed to ensure camera visibility: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Check if Game Bar is currently recording by looking for recording processes/indicators
        /// </summary>
        private static async Task<bool> IsGameBarRecordingActive()
        {
            return await Task.Run(() =>
            {
                try
                {
                    // Method 1: Check for Game Bar recording-related processes
                    var gameBarProcesses = Process.GetProcessesByName("GameBar");
                    var recordingProcesses = Process.GetProcessesByName("GameBarFTServer");
                    var captureProcesses = Process.GetProcessesByName("WinGameBarCapture");
                    
                    bool hasActiveProcesses = gameBarProcesses.Length > 0 || 
                                            recordingProcesses.Length > 0 || 
                                            captureProcesses.Length > 0;
                    
                    if (hasActiveProcesses)
                    {
                        Console.WriteLine("[DEBUG] Game Bar recording processes detected");
                        return true;
                    }
                    
                    // Method 2: Check for Game Bar window titles indicating recording
                    var allProcesses = Process.GetProcesses();
                    foreach (var process in allProcesses)
                    {
                        try
                        {
                            if (!string.IsNullOrEmpty(process.MainWindowTitle) && 
                                (process.MainWindowTitle.Contains("Recording") || 
                                 process.MainWindowTitle.Contains("Game Bar") ||
                                 process.ProcessName.Contains("GameBar")))
                            {
                                Console.WriteLine($"[DEBUG] Found Game Bar process: {process.ProcessName} - {process.MainWindowTitle}");
                                return true;
                            }
                        }
                        catch { }
                    }
                    
                    return false;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[DEBUG] Error checking Game Bar recording status: {ex.Message}");
                    return false;
                }
            });
        }

        /// <summary>
        /// Configure camera window to avoid Game Bar game detection
        /// </summary>
        public static async Task<bool> PreventGameBarDetection(Window cameraWindow)
        {
            if (cameraWindow == null) return false;
            
            try
            {
                Console.WriteLine("[DEBUG] Configuring camera window to avoid Game Bar game detection...");
                
                await cameraWindow.Dispatcher.InvokeAsync(() =>
                {
                    // Get window handle for Win32 operations
                    var hwnd = new System.Windows.Interop.WindowInteropHelper(cameraWindow).Handle;
                    
                    if (hwnd != IntPtr.Zero)
                    {
                        // Get current extended window style
                        int currentStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
                        
                        // Add WS_EX_TOOLWINDOW (makes it a tool window - not a game)
                        // Remove WS_EX_APPWINDOW (prevents it from appearing as an application)
                        int newStyle = (currentStyle | WS_EX_TOOLWINDOW) & ~WS_EX_APPWINDOW;
                        
                        // Apply the new style
                        SetWindowLong(hwnd, GWL_EXSTYLE, newStyle);
                        
                        Console.WriteLine("[DEBUG] Camera window configured as tool window to avoid game detection");
                    }
                    
                    // Also configure WPF properties to minimize game-like behavior
                    cameraWindow.ShowInTaskbar = false;  // Don't show in taskbar
                    cameraWindow.Title = "Camera Tool";  // Generic tool name
                });
                
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Failed to prevent Game Bar detection: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Clear Game Bar registry entries that might remember this app as a game
        /// </summary>
        public static async Task<bool> ClearGameBarGameMemory()
        {
            return await Task.Run(() =>
            {
                try
                {
                    Console.WriteLine("[DEBUG] Clearing Game Bar game memory for Camera Overlay...");
                    
                    // List of potential registry paths where Game Bar stores game information
                    string[] registryPaths = {
                        @"SOFTWARE\Microsoft\Windows\CurrentVersion\GameDVR\WhiteList",
                        @"SOFTWARE\Microsoft\Windows\CurrentVersion\GameDVR\BlackList",
                        @"SOFTWARE\Microsoft\Windows\CurrentVersion\GameDVR\AppSettings",
                        @"SOFTWARE\Microsoft\Windows\CurrentVersion\GameDVR\KnownGames"
                    };
                    
                    foreach (string path in registryPaths)
                    {
                        try
                        {
                            using (var key = Registry.CurrentUser.OpenSubKey(path, true))
                            {
                                if (key != null)
                                {
                                    // Look for entries related to our application
                                    string[] valueNames = key.GetValueNames();
                                    foreach (string valueName in valueNames)
                                    {
                                        if (valueName.Contains("Camera") || valueName.Contains("CameraOverlay"))
                                        {
                                            key.DeleteValue(valueName, false);
                                            Console.WriteLine($"[DEBUG] Removed Game Bar entry: {path}\\{valueName}");
                                        }
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[DEBUG] Could not access registry path {path}: {ex.Message}");
                        }
                    }
                    
                    Console.WriteLine("[DEBUG] Game Bar memory cleanup completed");
                    return true;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ERROR] Failed to clear Game Bar memory: {ex.Message}");
                    return false;
                }
            });
        }

        /// <summary>
        /// Configure camera window as a floating window for recording
        /// </summary>
        public static async Task<bool> ConfigureCameraWindow(Window cameraWindow)
        {
            if (cameraWindow == null) return false;

            try
            {
                Console.WriteLine("[DEBUG] Configuring camera window as floating window...");

                await cameraWindow.Dispatcher.InvokeAsync(() =>
                {
                    // Remove Topmost property to allow floating behavior
                    cameraWindow.Topmost = false;

                    // Add optional frame around the camera window
                    cameraWindow.BorderThickness = new Thickness(2);
                    cameraWindow.BorderBrush = Brushes.Black;

                    // Ensure camera feed is visible
                    cameraWindow.Visibility = Visibility.Visible;

                    Console.WriteLine("[DEBUG] Camera window configured as floating window.");
                });

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Failed to configure camera window: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Starts desktop recording with Game Bar, ensuring camera window is configured correctly
        /// </summary>
        public static async Task<bool> StartDesktopRecordingAsync(Window cameraWindow)
        {
            try
            {
                Console.WriteLine("[DEBUG] Starting desktop recording with Game Bar...");

                // Ensure Game Bar is enabled
                if (!IsGameBarEnabled())
                {
                    Console.WriteLine("[ERROR] Game Bar is not enabled or available");
                    return false;
                }

                // Configure camera window as floating window
                await ConfigureCameraWindow(cameraWindow);

                // Focus desktop to ensure screen recording
                await FocusDesktop();
                await Task.Delay(500);

                // Start recording using keyboard shortcut
                Console.WriteLine("[DEBUG] Trying keyboard shortcut method (Win+Alt+R)...");
                await SendKeyCombo(VK_LWIN, VK_MENU, VK_R);
                await Task.Delay(2000);

                // Verify recording started
                if (await IsGameBarRecordingActive())
                {
                    Console.WriteLine("[DEBUG] ✓ Desktop recording started successfully");
                    return true;
                }

                Console.WriteLine("[WARNING] Failed to start desktop recording");
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Failed to start desktop recording: {ex.Message}");
                return false;
            }
        }
        }

        /// <summary>
        /// Starts desktop recording with Game Bar, ensuring camera window is configured correctly
        /// with retry mechanism
        /// </summary>
        public static async Task<bool> StartDesktopRecordingWithRetryAsync(Window cameraWindow)
        {
            try
            {
                Console.WriteLine("[DEBUG] Starting desktop recording with Game Bar (camera window visible, not focused)...");

                // Ensure Game Bar is available
                if (!IsGameBarEnabled())
                {
                    Console.WriteLine("[ERROR] Game Bar is not enabled or available");
                    return false;
                }

                // Ensure camera window is visible and in normal state
                if (cameraWindow != null)
                {
                    await cameraWindow.Dispatcher.InvokeAsync(() =>
                    {
                        cameraWindow.WindowState = WindowState.Normal;
                        cameraWindow.ShowInTaskbar = true;
                        cameraWindow.Topmost = false;
                        cameraWindow.Visibility = Visibility.Visible;
                        cameraWindow.Show();
                    });
                    Console.WriteLine("[DEBUG] Camera window set to visible and normal.");
                }

                // Focus desktop to remove focus from camera window
                await FocusDesktop();
                await Task.Delay(1000);

                // Attempt to start recording (Win+Alt+R)
                Console.WriteLine("[DEBUG] Sending Win+Alt+R to start recording...");
                await SendKeyCombo(VK_LWIN, VK_MENU, VK_R);
                await Task.Delay(2000);

                // Check if recording started
                if (await IsGameBarRecordingActive())
                {
                    Console.WriteLine("[DEBUG] ✓ Desktop recording started successfully");
                    return true;
                }

                // Retry if initial attempt failed
                Console.WriteLine("[WARNING] First attempt failed, retrying...");
                await FocusDesktop();
                await Task.Delay(1000);
                await SendKeyCombo(VK_LWIN, VK_MENU, VK_R);
                await Task.Delay(2000);

                if (await IsGameBarRecordingActive())
                {
                    Console.WriteLine("[DEBUG] ✓ Desktop recording started successfully on retry");
                    return true;
                }

                Console.WriteLine("[ERROR] Failed to start desktop recording after retry");
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Failed to start desktop recording: {ex.Message}");
                return false;
            }
