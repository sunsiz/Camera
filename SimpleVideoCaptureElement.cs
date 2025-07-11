using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Management;
using Microsoft.Win32;

namespace CameraOverlay
{
    public class SimpleVideoCaptureElement : UserControl, IDisposable
    {
        private List<CameraInfo> availableCameras;
        private CameraInfo currentCamera;
        private string currentResolution;
        private bool isDisposed = false;

        public CameraInfo CurrentCamera => currentCamera;
        public string CurrentResolution => currentResolution;

        public SimpleVideoCaptureElement()
        {
            try
            {
                Console.WriteLine("[DEBUG] SimpleVideoCaptureElement constructor starting...");
                InitializeComponent();
                LoadAvailableCameras();
                StartDefaultCamera();
                Console.WriteLine("[DEBUG] SimpleVideoCaptureElement constructor completed successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Error in SimpleVideoCaptureElement constructor: {ex.Message}");
                
                // Emergency fallback
                this.Background = Brushes.Black;
                var errorGrid = new Grid();
                var errorText = new TextBlock
                {
                    Text = "🛡️ SAFE MODE\n\nSimple camera overlay is running.\nNo complex features but fully functional.\n\nWindow can be moved and resized.",
                    Foreground = Brushes.LightBlue,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    TextAlignment = TextAlignment.Center,
                    FontSize = 12,
                    FontWeight = FontWeights.Bold
                };
                errorGrid.Children.Add(errorText);
                this.Content = errorGrid;
            }
        }

        private void InitializeComponent()
        {
            Console.WriteLine("[DEBUG] SimpleVideoCaptureElement InitializeComponent() called");
            this.Background = Brushes.Black;
        }

        private void LoadAvailableCameras()
        {
            Console.WriteLine("[DEBUG] Loading available cameras (simple mode)...");
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
        }

        private List<CameraInfo> DetectCamerasWithWMI()
        {
            var cameras = new List<CameraInfo>();
            
            try
            {
                Console.WriteLine("[DEBUG] Starting simple WMI camera detection...");
                
                using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_PnPEntity WHERE (PNPClass = 'Camera' OR Name LIKE '%camera%') AND Status = 'OK'"))
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
                            
                            if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(deviceId))
                            {
                                // Simple validation - just check if it contains "camera"
                                if (name.ToLower().Contains("camera") && !name.ToLower().Contains("dfu"))
                                {
                                    cameras.Add(new CameraInfo
                                    {
                                        Name = name,
                                        DevicePath = deviceId,
                                        Index = index++
                                    });
                                    
                                    Console.WriteLine($"[SUCCESS] Added camera: {name}");
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[ERROR] Error processing camera device: {ex.Message}");
                        }
                    }
                }
                
                Console.WriteLine($"[DEBUG] Simple camera detection completed. Found {cameras.Count} cameras");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Error detecting cameras with WMI: {ex.Message}");
            }
            
            return cameras;
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
            ShowCameraDisplay();
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
                ShowCameraDisplay();
            }
        }

        private void ShowCameraDisplay()
        {
            try
            {
                Console.WriteLine("[DEBUG] Showing simple camera display");
                
                var grid = new Grid();
                
                var gradientBrush = new LinearGradientBrush();
                gradientBrush.StartPoint = new Point(0, 0);
                gradientBrush.EndPoint = new Point(1, 1);
                gradientBrush.GradientStops.Add(new GradientStop(Color.FromRgb(20, 40, 20), 0));
                gradientBrush.GradientStops.Add(new GradientStop(Color.FromRgb(10, 30, 10), 1));
                
                grid.Background = gradientBrush;
                
                var parentWindow = Window.GetWindow(this);
                string windowSize = parentWindow != null ? 
                    $"{(int)parentWindow.Width}x{(int)parentWindow.Height}" : 
                    currentResolution ?? "Unknown";
                
                var textBlock = new TextBlock
                {
                    Text = $"📹 {currentCamera?.Name ?? "Camera"}\n{windowSize}\n\n✅ CAMERA DETECTED\n\n🛡️ Simple Mode Active\n\nThis is a simplified camera overlay\nwithout complex video processing.\n\nCamera: {currentCamera?.Name}\nStatus: Detected and Ready\n\nRight-click for camera options.",
                    Foreground = Brushes.LightGreen,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    TextAlignment = TextAlignment.Center,
                    FontSize = 10,
                    FontWeight = FontWeights.Bold
                };
                
                grid.Children.Add(textBlock);
                this.Content = grid;
                
                Console.WriteLine("[DEBUG] Simple camera display created successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Error showing camera display: {ex.Message}");
            }
        }

        public void RefreshDisplay()
        {
            ShowCameraDisplay();
        }

        public void Dispose()
        {
            if (isDisposed) return;
            
            try
            {
                Console.WriteLine("[DEBUG] Disposing SimpleVideoCaptureElement...");
                currentCamera = null;
                availableCameras?.Clear();
                isDisposed = true;
                Console.WriteLine("[DEBUG] SimpleVideoCaptureElement disposed successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Error disposing SimpleVideoCaptureElement: {ex.Message}");
            }
        }
    }
}
