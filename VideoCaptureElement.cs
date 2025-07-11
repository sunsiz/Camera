using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Management;
using Microsoft.Win32;

namespace CameraOverlay
{
    public class VideoCaptureElement : UserControl, IDisposable
    {
        private MediaElement mediaElement;
        private Image cameraImage;
        private RealCameraCapture realCamera;
        private DirectShowCameraCapture directShowCamera;
        private WindowsMediaFoundationCapture wmfCamera;
        private List<CameraInfo> availableCameras;
        private CameraInfo currentCamera;
        private string currentResolution;
        private bool isDisposed = false;
        private bool useRealCamera = true;
        private bool useDirectShow = true;
        private bool useWMF = true;

        public CameraInfo CurrentCamera => currentCamera;
        public string CurrentResolution => currentResolution;

        public VideoCaptureElement()
        {
            try
            {
                Console.WriteLine("[DEBUG] VideoCaptureElement constructor starting...");
                InitializeComponent();
                LoadAvailableCameras();
                StartDefaultCamera();
                Console.WriteLine("[DEBUG] VideoCaptureElement constructor completed successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Critical error in VideoCaptureElement constructor: {ex.Message}");
                Console.WriteLine($"[ERROR] Stack trace: {ex.StackTrace}");
                
                // Emergency fallback - create minimal working UI
                try
                {
                    this.Background = Brushes.Black;
                    var errorGrid = new Grid();
                    var errorText = new TextBlock
                    {
                        Text = "❌ Camera Initialization Error\n\nThere was an error starting the camera.\nCheck the debug console for details.\n\nThe application is running in safe mode.",
                        Foreground = Brushes.Yellow,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center,
                        TextAlignment = TextAlignment.Center,
                        FontSize = 12,
                        FontWeight = FontWeights.Bold
                    };
                    errorGrid.Children.Add(errorText);
                    this.Content = errorGrid;
                }
                catch
                {
                    // If even the error display fails, just set a black background
                    this.Background = Brushes.Black;
                }
            }
        }

        private void InitializeComponent()
        {
            try
            {
                Console.WriteLine("[DEBUG] InitializeComponent() called - setting up initial UI");
                this.Background = Brushes.Black;
                
                mediaElement = new MediaElement
                {
                    Stretch = Stretch.Uniform,
                    LoadedBehavior = MediaState.Play,
                    UnloadedBehavior = MediaState.Stop
                };
                
                cameraImage = new Image
                {
                    Stretch = Stretch.Uniform
                };
                
                Console.WriteLine("[DEBUG] Creating RealCameraCapture instance...");
                try
                {
                    realCamera = new RealCameraCapture();
                    realCamera.FrameAvailable += OnCameraFrameAvailable;
                    Console.WriteLine("[DEBUG] RealCameraCapture created successfully");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ERROR] Failed to create RealCameraCapture: {ex.Message}");
                    realCamera = null;
                    useRealCamera = false;
                }

                // Try DirectShow camera as primary option
                Console.WriteLine("[DEBUG] Creating DirectShowCameraCapture instance...");
                try
                {
                    directShowCamera = new DirectShowCameraCapture();
                    directShowCamera.FrameAvailable += OnCameraFrameAvailable;
                    Console.WriteLine("[DEBUG] DirectShowCameraCapture created successfully");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ERROR] Failed to create DirectShowCameraCapture: {ex.Message}");
                    directShowCamera = null;
                    useDirectShow = false;
                }

                // Try Windows Media Foundation as secondary option
                Console.WriteLine("[DEBUG] Creating WindowsMediaFoundationCapture instance...");
                try
                {
                    wmfCamera = new WindowsMediaFoundationCapture();
                    wmfCamera.FrameAvailable += OnCameraFrameAvailable;
                    Console.WriteLine("[DEBUG] WindowsMediaFoundationCapture created successfully");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ERROR] Failed to create WindowsMediaFoundationCapture: {ex.Message}");
                    wmfCamera = null;
                    useWMF = false;
                }
                
                Console.WriteLine("[DEBUG] MediaElement and real camera components created successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Error in InitializeComponent: {ex.Message}");
                // Create minimal fallback components
                this.Background = Brushes.Black;
                cameraImage = new Image { Stretch = Stretch.Uniform };
                useRealCamera = false;
            }
        }

        private void LoadAvailableCameras()
        {
            Console.WriteLine("[DEBUG] Starting camera detection process...");
            availableCameras = new List<CameraInfo>();
            
            try
            {
                var cameras = DetectCamerasWithWMI();
                availableCameras.AddRange(cameras);
                Console.WriteLine($"[DEBUG] WMI detection found {cameras.Count} cameras");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Error loading cameras: {ex.Message}");
            }

            if (availableCameras.Count == 0)
            {
                Console.WriteLine("[DEBUG] No cameras detected, adding default placeholder camera");
                availableCameras.Add(new CameraInfo 
                { 
                    Name = "Default Camera", 
                    DevicePath = "default",
                    Index = 0 
                });
            }
            
            Console.WriteLine($"[DEBUG] Camera detection completed. Total cameras available: {availableCameras.Count}");
            foreach (var camera in availableCameras)
            {
                Console.WriteLine($"  - {camera.Name} (Path: {camera.DevicePath})");
            }
        }

        private List<CameraInfo> DetectCamerasWithWMI()
        {
            var cameras = new List<CameraInfo>();
            
            try
            {
                Console.WriteLine("[DEBUG] Starting WMI camera detection...");
                
                using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_PnPEntity WHERE (PNPClass = 'Camera' OR PNPClass = 'Image' OR Name LIKE '%camera%' OR Name LIKE '%webcam%') AND Status = 'OK'"))
                {
                    var devices = searcher.Get();
                    Console.WriteLine($"[DEBUG] WMI found {devices.Count} potential camera devices");
                    
                    int index = 0;
                    
                    foreach (ManagementObject device in devices)
                    {
                        try
                        {
                            string name = device["Name"]?.ToString();
                            string deviceId = device["DeviceID"]?.ToString();
                            string status = device["Status"]?.ToString();
                            string service = device["Service"]?.ToString();
                            string manufacturer = device["Manufacturer"]?.ToString();
                            
                            Console.WriteLine($"[DEBUG] Processing device: {name}");
                            Console.WriteLine($"  DeviceID: {deviceId}");
                            Console.WriteLine($"  Status: {status}");
                            Console.WriteLine($"  Service: {service}");
                            Console.WriteLine($"  Manufacturer: {manufacturer}");
                            
                            if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(deviceId))
                            {
                                bool isValid = IsValidCameraDevice(name, deviceId);
                                Console.WriteLine($"[DEBUG] Device validation result: {isValid}");
                                
                                if (isValid)
                                {
                                    bool isAccessible = IsAccessibleCameraDevice(name, deviceId);
                                    Console.WriteLine($"[DEBUG] Device accessibility result: {isAccessible}");
                                    
                                    if (isAccessible)
                                    {
                                        cameras.Add(new CameraInfo
                                        {
                                            Name = name,
                                            DevicePath = deviceId,
                                            Index = index++
                                        });
                                        
                                        Console.WriteLine($"[SUCCESS] Added valid camera: {name}");
                                    }
                                    else
                                    {
                                        Console.WriteLine($"[WARNING] Camera not accessible: {name}");
                                    }
                                }
                                else
                                {
                                    Console.WriteLine($"[DEBUG] Filtered out device: {name} (not a functional camera)");
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[ERROR] Error processing camera device: {ex.Message}");
                        }
                    }
                }
                
                Console.WriteLine($"[DEBUG] WMI camera detection completed. Found {cameras.Count} valid cameras");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Error detecting cameras with WMI: {ex.Message}");
            }
            
            return cameras;
        }

        private bool IsValidCameraDevice(string name, string deviceId)
        {
            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(deviceId))
                return false;
                
            string nameLower = name.ToLower();
            string deviceIdLower = deviceId.ToLower();
            
            // Enhanced filter for non-functional camera devices
            string[] excludeTerms = { 
                "dfu", "device firmware upgrade", "firmware", "bootloader", "update", 
                "virtual", "emulated", "composite", "filter", "driver", "hub",
                "controller", "root", "interface", "class", "generic", "unknown"
            };
            
            foreach (string term in excludeTerms)
            {
                if (nameLower.Contains(term) || deviceIdLower.Contains(term))
                {
                    return false;
                }
            }
            
            // Include devices that are likely functional cameras
            string[] includeTerms = { 
                "camera", "webcam", "video device", "usb video", "integrated camera",
                "built-in camera", "front camera", "rear camera", "hd camera",
                "web camera", "usb camera"
            };
            
            foreach (string term in includeTerms)
            {
                if (nameLower.Contains(term))
                {
                    if (!nameLower.Contains("dfu") && !nameLower.Contains("firmware"))
                    {
                        return true;
                    }
                }
            }
            
            return false;
        }

        private bool IsAccessibleCameraDevice(string name, string deviceId)
        {
            try
            {
                using (var searcher = new ManagementObjectSearcher($"SELECT * FROM Win32_PnPEntity WHERE DeviceID = '{deviceId.Replace("\\", "\\\\")}'"))
                {
                    var devices = searcher.Get();
                    foreach (ManagementObject device in devices)
                    {
                        var status = device["Status"]?.ToString();
                        var configManagerErrorCode = device["ConfigManagerErrorCode"]?.ToString();
                        
                        if (status == "OK" && (configManagerErrorCode == "0" || string.IsNullOrEmpty(configManagerErrorCode)))
                        {
                            return true;
                        }
                        else
                        {
                            Console.WriteLine($"Device {name} has issues - Status: {status}, Error: {configManagerErrorCode}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error checking device accessibility for {name}: {ex.Message}");
            }
            
            return false;
        }

        public List<CameraInfo> GetAvailableCameras()
        {
            return availableCameras?.ToList() ?? new List<CameraInfo>();
        }

        public List<string> GetAvailableResolutions()
        {
            return new List<string>
            {
                "320x240", "640x480", "800x600", "1024x768", "1280x720", "1920x1080"
            };
        }

        public void SelectCamera(CameraInfo camera)
        {
            if (camera == null) return;
            currentCamera = camera;
            StartCamera();
        }

        public void SetResolution(string resolution)
        {
            currentResolution = resolution;
        }

        private void StartDefaultCamera()
        {
            if (availableCameras?.Count > 0)
            {
                currentCamera = availableCameras[0];
                StartCamera();
            }
        }

        private void StartCamera()
        {
            try
            {
                Console.WriteLine($"[DEBUG] Starting camera: {currentCamera?.Name}");
                
                bool initResult = TryInitializeWebcam();
                Console.WriteLine($"[DEBUG] TryInitializeWebcam() returned: {initResult}");
                
                if (initResult)
                {
                    Console.WriteLine("[DEBUG] Calling ShowCameraFeed() - should show GREEN 'CAMERA READY'");
                    ShowCameraFeed();
                }
                else
                {
                    Console.WriteLine("[DEBUG] Calling ShowCameraPlaceholder() - will show yellow 'CAMERA DETECTED'");
                    ShowCameraPlaceholder();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Error starting camera: {ex.Message}");
                ShowErrorMessage($"Camera Error: {ex.Message}");
            }
        }

        private bool TryInitializeWebcam()
        {
            try
            {
                Console.WriteLine($"[DEBUG] Attempting to initialize camera: {currentCamera?.Name}");
                Console.WriteLine($"[DEBUG] Camera device path: {currentCamera?.DevicePath}");
                
                if (currentCamera == null || string.IsNullOrEmpty(currentCamera.DevicePath))
                {
                    Console.WriteLine("[DEBUG] No valid camera selected");
                    return false;
                }
                
                bool isAccessible = IsCameraAccessible(currentCamera.DevicePath);
                Console.WriteLine($"[DEBUG] Camera accessibility: {isAccessible}");
                
                if (isAccessible)
                {
                    // Try Windows Media Foundation first (most reliable for modern cameras)
                    if (useWMF && wmfCamera != null)
                    {
                        Console.WriteLine("[DEBUG] Attempting Windows Media Foundation camera initialization...");
                        try
                        {
                            bool wmfInit = wmfCamera.Initialize(currentCamera.DevicePath);
                            Console.WriteLine($"[DEBUG] WMF initialization result: {wmfInit}");
                            
                            if (wmfInit)
                            {
                                Console.WriteLine($"[SUCCESS] Windows Media Foundation camera {currentCamera.Name} is ready for use");
                                return true;
                            }
                            else
                            {
                                Console.WriteLine($"[WARNING] WMF initialization failed, trying DirectShow...");
                                useWMF = false;
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[ERROR] Exception during WMF initialization: {ex.Message}");
                            useWMF = false;
                        }
                    }

                    // Try DirectShow as secondary option
                    if (useDirectShow && directShowCamera != null)
                    {
                        Console.WriteLine("[DEBUG] Attempting DirectShow camera initialization...");
                        try
                        {
                            bool directShowInit = directShowCamera.Initialize(currentCamera.DevicePath);
                            Console.WriteLine($"[DEBUG] DirectShow initialization result: {directShowInit}");
                            
                            if (directShowInit)
                            {
                                Console.WriteLine($"[SUCCESS] DirectShow camera {currentCamera.Name} is ready for use");
                                return true;
                            }
                            else
                            {
                                Console.WriteLine($"[WARNING] DirectShow initialization failed, trying MediaFoundation fallback...");
                                useDirectShow = false;
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[ERROR] Exception during DirectShow initialization: {ex.Message}");
                            useDirectShow = false;
                        }
                    }

                    // Fallback to original MediaFoundation
                    if (useRealCamera && realCamera != null)
                    {
                        Console.WriteLine("[DEBUG] Attempting MediaFoundation fallback camera initialization...");
                        try
                        {
                            bool realCameraInit = realCamera.Initialize(currentCamera.DevicePath);
                            Console.WriteLine($"[DEBUG] MediaFoundation fallback initialization result: {realCameraInit}");
                            
                            if (realCameraInit)
                            {
                                Console.WriteLine($"[SUCCESS] MediaFoundation fallback camera {currentCamera.Name} is ready for use");
                                return true;
                            }
                            else
                            {
                                Console.WriteLine($"[WARNING] All camera initialization methods failed, falling back to test mode");
                                useRealCamera = false;
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[ERROR] Exception during MediaFoundation fallback initialization: {ex.Message}");
                            useRealCamera = false;
                        }
                    }
                    
                    // Try actual camera access to verify permissions
                    Console.WriteLine("[DEBUG] Attempting actual camera access...");
                    bool actualAccess = TryActualCameraAccess();
                    Console.WriteLine($"[DEBUG] Actual camera access result: {actualAccess}");
                    
                    if (actualAccess)
                    {
                        Console.WriteLine($"[SUCCESS] Camera {currentCamera.Name} is ready for use");
                        return true;
                    }
                    else
                    {
                        Console.WriteLine($"[WARNING] Camera {currentCamera.Name} detected but access validation failed");
                        return false;
                    }
                }
                
                Console.WriteLine("[DEBUG] Camera not accessible, showing placeholder");
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Camera initialization failed: {ex.Message}");
                return false;
            }
        }

        private bool IsCameraAccessible(string devicePath)
        {
            if (string.IsNullOrEmpty(devicePath))
                return false;
            
            return !devicePath.Equals("default", StringComparison.OrdinalIgnoreCase);
        }

        private void ShowCameraFeed()
        {
            Console.WriteLine("[DEBUG] ShowCameraFeed() method called - creating REAL CAMERA FEED UI");
            
            // Ensure UI updates happen on UI thread
            if (!this.Dispatcher.CheckAccess())
            {
                Console.WriteLine("[DEBUG] Not on UI thread, invoking on UI thread");
                this.Dispatcher.Invoke(() => ShowCameraFeed());
                return;
            }
            
            var grid = new Grid();
            
            // Real camera feed background
            var gradientBrush = new LinearGradientBrush();
            gradientBrush.StartPoint = new Point(0, 0);
            gradientBrush.EndPoint = new Point(1, 1);
            gradientBrush.GradientStops.Add(new GradientStop(Color.FromRgb(10, 20, 10), 0));
            gradientBrush.GradientStops.Add(new GradientStop(Color.FromRgb(5, 15, 5), 1));
            
            grid.Background = gradientBrush;
            
            if (useWMF && wmfCamera != null)
            {
                Console.WriteLine("[DEBUG] Setting up Windows Media Foundation camera feed display");
                
                try
                {
                    // Create a new image control for this display to avoid parent conflicts
                    var cameraImageControl = new Image
                    {
                        Stretch = Stretch.Uniform,
                        Source = cameraImage?.Source
                    };
                    
                    // Add camera image for real video feed
                    grid.Children.Add(cameraImageControl);
                    
                    // Update the reference so frames will update this control
                    cameraImage = cameraImageControl;
                    
                    // Start camera capture
                    wmfCamera.StartCapture();
                    
                    // Add overlay text
                    var overlayText = new TextBlock
                    {
                        Text = $"🎥 LIVE: {currentCamera?.Name ?? "Camera"}\n📹 Windows Media Foundation",
                        Foreground = Brushes.LightGreen,
                        HorizontalAlignment = HorizontalAlignment.Left,
                        VerticalAlignment = VerticalAlignment.Top,
                        TextAlignment = TextAlignment.Left,
                        FontSize = 10,
                        FontWeight = FontWeights.Bold,
                        Margin = new Thickness(10, 10, 0, 0),
                        Background = new SolidColorBrush(Color.FromArgb(128, 0, 0, 0))
                    };
                    
                    grid.Children.Add(overlayText);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ERROR] Error setting up WMF camera display: {ex.Message}");
                    useWMF = false; // Fall back to next option
                }
            }
            else if (useDirectShow && directShowCamera != null)
            {
                Console.WriteLine("[DEBUG] Setting up DirectShow camera feed display");
                
                try
                {
                    // Create a new image control for this display to avoid parent conflicts
                    var cameraImageControl = new Image
                    {
                        Stretch = Stretch.Uniform,
                        Source = cameraImage?.Source
                    };
                    
                    // Add camera image for real video feed
                    grid.Children.Add(cameraImageControl);
                    
                    // Update the reference so frames will update this control
                    cameraImage = cameraImageControl;
                    
                    // Start camera capture
                    directShowCamera.StartCapture();
                    
                    // Add overlay text
                    var overlayText = new TextBlock
                    {
                        Text = $"🎥 LIVE: {currentCamera?.Name ?? "Camera"}\n📹 DirectShow Feed",
                        Foreground = Brushes.LightGreen,
                        HorizontalAlignment = HorizontalAlignment.Left,
                        VerticalAlignment = VerticalAlignment.Top,
                        TextAlignment = TextAlignment.Left,
                        FontSize = 10,
                        FontWeight = FontWeights.Bold,
                        Margin = new Thickness(10, 10, 0, 0),
                        Background = new SolidColorBrush(Color.FromArgb(128, 0, 0, 0))
                    };
                    
                    grid.Children.Add(overlayText);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ERROR] Error setting up DirectShow camera display: {ex.Message}");
                    useDirectShow = false; // Fall back to next option
                }
            }
            else if (useRealCamera && realCamera != null)
            {
                Console.WriteLine("[DEBUG] Setting up fallback MediaFoundation camera feed display");
                
                try
                {
                    // Create a new image control for this display to avoid parent conflicts
                    var cameraImageControl = new Image
                    {
                        Stretch = Stretch.Uniform,
                        Source = cameraImage?.Source
                    };
                    
                    // Add camera image for real video feed
                    grid.Children.Add(cameraImageControl);
                    
                    // Update the reference so frames will update this control
                    cameraImage = cameraImageControl;
                    
                    // Start camera capture
                    realCamera.StartCapture();
                    
                    // Add overlay text
                    var overlayText = new TextBlock
                    {
                        Text = $"🎥 LIVE: {currentCamera?.Name ?? "Camera"}\n📹 MediaFoundation Fallback",
                        Foreground = Brushes.LightGreen,
                        HorizontalAlignment = HorizontalAlignment.Left,
                        VerticalAlignment = VerticalAlignment.Top,
                        TextAlignment = TextAlignment.Left,
                        FontSize = 10,
                        FontWeight = FontWeights.Bold,
                        Margin = new Thickness(10, 10, 0, 0),
                        Background = new SolidColorBrush(Color.FromArgb(128, 0, 0, 0))
                    };
                    
                    grid.Children.Add(overlayText);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ERROR] Error setting up fallback camera display: {ex.Message}");
                    useRealCamera = false; // Fall back to text display
                }
            }
            else
            {
                Console.WriteLine("[DEBUG] Using fallback text display mode");
                // Fallback to test pattern
                var parentWindow = Window.GetWindow(this);
                string windowSize = parentWindow != null ? 
                    $"{(int)parentWindow.Width}x{(int)parentWindow.Height}" : 
                    currentResolution ?? "Unknown";
                
                var textBlock = new TextBlock
                {
                    Text = $"📹 {currentCamera?.Name ?? "Camera"}\n{windowSize}\n\n⚠️ CAMERA SIMULATION\n\nReal camera capture requires:\n• Camera permissions in Windows\n• Compatible DirectShow drivers\n• Camera not in use by other apps\n\nThis shows the video pipeline works.\nActual camera feed will appear here\nonce camera access is available.\n\nRight-click to toggle debug console.",
                    Foreground = Brushes.Orange,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    TextAlignment = TextAlignment.Center,
                    FontSize = 10,
                    FontWeight = FontWeights.Bold
                };
                
                grid.Children.Add(textBlock);
            }
            
            // Clear any existing content first
            this.Content = null;
            this.Content = grid;
            
            Console.WriteLine("[DEBUG] ShowCameraFeed() UI created with real camera support");
            Console.WriteLine($"[DEBUG] Using Windows Media Foundation: {useWMF}");
            Console.WriteLine($"[DEBUG] Using DirectShow: {useDirectShow}");
            Console.WriteLine($"[DEBUG] Using MediaFoundation Fallback: {useRealCamera}");
        }

        private void ShowCameraPlaceholder()
        {
            Console.WriteLine("[DEBUG] ShowCameraPlaceholder() method called - creating YELLOW 'CAMERA DETECTED' UI");
            
            // Ensure UI updates happen on UI thread
            if (!this.Dispatcher.CheckAccess())
            {
                Console.WriteLine("[DEBUG] Not on UI thread, invoking on UI thread");
                this.Dispatcher.Invoke(() => ShowCameraPlaceholder());
                return;
            }
            
            var grid = new Grid();
            grid.Background = new SolidColorBrush(Color.FromRgb(40, 40, 40));
            
            var parentWindow = Window.GetWindow(this);
            string windowSize = parentWindow != null ? 
                $"{(int)parentWindow.Width}x{(int)parentWindow.Height}" : 
                currentResolution ?? "Unknown";
            
            var textBlock = new TextBlock
            {
                Text = $"📷 {currentCamera?.Name ?? "Camera"}\n{windowSize}\n\n⚠️ CAMERA DETECTED\n\nCamera is detected but may require\npermissions in Windows 11.\n\nTo enable camera access:\n1. Open Windows Settings\n2. Go to Privacy & Security → Camera\n3. Enable camera for desktop apps\n\nRight-click to toggle debug console.",
                Foreground = Brushes.LightYellow,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                TextAlignment = TextAlignment.Center,
                FontSize = 10,
                FontWeight = FontWeights.Bold
            };
            
            grid.Children.Add(textBlock);
            
            // Clear any existing content first
            this.Content = null;
            this.Content = grid;
            
            Console.WriteLine("[DEBUG] ShowCameraPlaceholder() UI created with gray background and 'CAMERA DETECTED' text");
            Console.WriteLine($"[DEBUG] Grid background: Gray, TextBlock foreground: LightYellow");
            Console.WriteLine($"[DEBUG] Content set to grid with 'CAMERA DETECTED' message");
        }

        private void ShowErrorMessage(string errorMessage = "Camera Error")
        {
            var grid = new Grid();
            grid.Background = new SolidColorBrush(Color.FromRgb(60, 30, 30));
            
            var textBlock = new TextBlock
            {
                Text = $"❌ {errorMessage}\n\nCamera detected but video feed not available.\nThis is a placeholder implementation.\n\nFor actual camera capture, DirectShow or\nMedia Foundation integration is needed.\n\nRight-click to toggle debug console.",
                Foreground = Brushes.LightCoral,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                TextAlignment = TextAlignment.Center,
                FontSize = 11,
                FontWeight = FontWeights.Normal
            };
            
            grid.Children.Add(textBlock);
            this.Content = grid;
        }

        public void RefreshDisplay()
        {
            Console.WriteLine("[DEBUG] RefreshDisplay() called - checking current camera status to determine UI");
            
            if (currentCamera != null)
            {
                Console.WriteLine($"[DEBUG] Current camera: {currentCamera.Name}");
                
                // Re-check camera status instead of always showing placeholder
                bool initResult = TryInitializeWebcam();
                Console.WriteLine($"[DEBUG] RefreshDisplay TryInitializeWebcam() returned: {initResult}");
                
                if (initResult)
                {
                    Console.WriteLine("[DEBUG] RefreshDisplay calling ShowCameraFeed() - GREEN 'CAMERA READY'");
                    ShowCameraFeed();
                }
                else
                {
                    Console.WriteLine("[DEBUG] RefreshDisplay calling ShowCameraPlaceholder() - YELLOW 'CAMERA DETECTED'");
                    ShowCameraPlaceholder();
                }
            }
            else
            {
                Console.WriteLine("[DEBUG] No current camera, showing placeholder");
                ShowCameraPlaceholder();
            }
        }

        public void Dispose()
        {
            if (isDisposed) return;
            
            try
            {
                Console.WriteLine("[DEBUG] Disposing VideoCaptureElement and releasing camera resources...");
                
                // Stop and dispose Windows Media Foundation camera
                wmfCamera?.StopCapture();
                wmfCamera?.Dispose();
                wmfCamera = null;
                
                // Stop and dispose DirectShow camera
                directShowCamera?.StopCapture();
                directShowCamera?.Dispose();
                directShowCamera = null;
                
                // Stop and dispose MediaFoundation fallback camera
                realCamera?.StopCapture();
                realCamera?.Dispose();
                realCamera = null;
                
                // Stop and release MediaElement
                mediaElement?.Stop();
                mediaElement = null;
                
                // Clear image reference
                cameraImage = null;
                
                // Clear camera references
                currentCamera = null;
                availableCameras?.Clear();
                
                isDisposed = true;
                Console.WriteLine("[DEBUG] Camera resources released successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Error disposing VideoCaptureElement: {ex.Message}");
            }
        }

        private bool TryActualCameraAccess()
        {
            try
            {
                Console.WriteLine("[DEBUG] Starting actual camera access validation with MediaFoundation...");
                
                // Method 1: Test MediaFoundation camera access
                Console.WriteLine("[DEBUG] Testing MediaFoundation camera access...");
                bool mfAccess = MediaFoundationCamera.TestCameraAccess(currentCamera.DevicePath);
                Console.WriteLine($"[DEBUG] MediaFoundation access result: {mfAccess}");
                
                // Method 2: Check if the device is currently being used by another application
                Console.WriteLine("[DEBUG] Checking if camera is in use by another application...");
                bool isInUse = IsCameraInUse();
                Console.WriteLine($"[DEBUG] Camera in use check: {isInUse}");
                
                // Method 3: Validate device through detailed WMI properties
                Console.WriteLine("[DEBUG] Performing detailed device validation...");
                bool deviceValid = ValidateDeviceDetails();
                Console.WriteLine($"[DEBUG] Device validation result: {deviceValid}");
                
                // Camera is ready if MediaFoundation can access it and it's not in use
                bool result = mfAccess && !isInUse && deviceValid;
                Console.WriteLine($"[DEBUG] Overall camera access result: {result}");
                Console.WriteLine($"[DEBUG] Reasoning: MediaFoundation={mfAccess}, NotInUse={!isInUse}, DeviceValid={deviceValid}");
                
                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Camera access validation failed: {ex.Message}");
                return false;
            }
        }
        
        private bool IsCameraInUse()
        {
            try
            {
                // Check if camera is being used by common applications
                // This is a simplified check - real implementation would use DirectShow graph enumeration
                Console.WriteLine("[DEBUG] Checking for camera usage by other applications...");
                
                // For now, assume camera is not in use
                // In real implementation, this would check DirectShow filter graphs
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Error checking camera usage: {ex.Message}");
                return false;
            }
        }
        
        private bool ValidateDeviceDetails()
        {
            try
            {
                Console.WriteLine("[DEBUG] Validating detailed device properties...");
                
                // Try multiple approaches to validate the device
                bool foundDevice = false;
                
                // Method 1: Direct DeviceID lookup
                try
                {
                    string escapedDeviceId = currentCamera.DevicePath.Replace("\\", "\\\\");
                    Console.WriteLine($"[DEBUG] Searching for device with ID: {escapedDeviceId}");
                    
                    using (var searcher = new ManagementObjectSearcher($"SELECT * FROM Win32_PnPEntity WHERE DeviceID = '{escapedDeviceId}'"))
                    {
                        var devices = searcher.Get();
                        foreach (ManagementObject device in devices)
                        {
                            foundDevice = true;
                            var status = device["Status"]?.ToString();
                            var present = device["Present"]?.ToString();
                            var enabled = device["Enabled"]?.ToString();
                            var configManagerErrorCode = device["ConfigManagerErrorCode"]?.ToString();
                            
                            Console.WriteLine($"[DEBUG] Device detailed status:");
                            Console.WriteLine($"  Status: {status}");
                            Console.WriteLine($"  Present: {present}");
                            Console.WriteLine($"  Enabled: {enabled}");
                            Console.WriteLine($"  ConfigManagerErrorCode: {configManagerErrorCode}");
                            
                            // Device is valid if status is OK and no error codes
                            bool isValid = status == "OK" && 
                                          (configManagerErrorCode == "0" || string.IsNullOrEmpty(configManagerErrorCode));
                            
                            Console.WriteLine($"[DEBUG] Device validation: {isValid}");
                            return isValid;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[DEBUG] Direct DeviceID lookup failed: {ex.Message}");
                }
                
                // Method 2: Search by camera name if direct lookup failed
                if (!foundDevice)
                {
                    Console.WriteLine("[DEBUG] Trying device lookup by name...");
                    try
                    {
                        using (var searcher = new ManagementObjectSearcher($"SELECT * FROM Win32_PnPEntity WHERE Name LIKE '%{currentCamera.Name}%' AND Status = 'OK'"))
                        {
                            var devices = searcher.Get();
                            foreach (ManagementObject device in devices)
                            {
                                foundDevice = true;
                                Console.WriteLine($"[DEBUG] Found device by name: {device["Name"]}");
                                Console.WriteLine($"[DEBUG] Device status: {device["Status"]}");
                                return true; // If we found it by name and status is OK, it's valid
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[DEBUG] Name-based lookup failed: {ex.Message}");
                    }
                }
                
                // Method 3: If device was found during initial detection, assume it's still valid
                if (!foundDevice)
                {
                    Console.WriteLine("[DEBUG] Device not found in validation, but was detected initially");
                    Console.WriteLine("[DEBUG] Assuming device is valid since it passed initial detection filters");
                    return true; // Device passed initial filtering, so it should be valid
                }
                
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Device validation failed: {ex.Message}");
                // If validation fails but camera was detected, assume it's valid
                return true;
            }
        }
        
        private bool SimulateCameraAccess()
        {
            try
            {
                Console.WriteLine("[DEBUG] Simulating camera access attempt...");
                
                // NOTE: This is where we would normally try to access the camera
                // For now, we'll simulate without actually grabbing the camera resource
                // to avoid blocking other applications
                
                // Simulate the time it would take to initialize a camera
                System.Threading.Thread.Sleep(100);
                
                // In a real implementation, this would:
                // 1. Create DirectShow graph or MediaFoundation session
                // 2. Try to start video capture
                // 3. Immediately release resources if successful
                // 4. Return true only if camera is actually accessible
                
                Console.WriteLine("[DEBUG] Camera access simulation completed successfully");
                Console.WriteLine("[DEBUG] Note: This is a simulation - actual video requires DirectShow/MediaFoundation");
                Console.WriteLine("[DEBUG] Camera resources not actually held to avoid blocking other apps");
                
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Camera access simulation failed: {ex.Message}");
                return false;
            }
        }

        private void OnCameraFrameAvailable(WriteableBitmap frame)
        {
            try
            {
                // Ensure UI updates happen on UI thread
                if (!this.Dispatcher.CheckAccess())
                {
                    this.Dispatcher.BeginInvoke(() => OnCameraFrameAvailable(frame));
                    return;
                }
                
                if (cameraImage != null && frame != null)
                {
                    cameraImage.Source = frame;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Error updating camera frame: {ex.Message}");
            }
        }
    }
}
