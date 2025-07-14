using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using OpenCvSharp;
using OpenCvSharp.WpfExtensions;

namespace CameraOverlay
{
    /// <summary>
    /// Real camera capture implementation using OpenCV
    /// This provides actual camera feed from connected cameras
    /// </summary>
    public class OpenCVCameraCapture : UserControl, IDisposable
    {
        private Image videoImage;
        private VideoCapture capture;
        private DispatcherTimer captureTimer;
        private Mat frame;
        private bool isCapturing = false;
        private bool isDisposed = false;
        private int cameraIndex = 0;
        private int frameWidth = 640;
        private int frameHeight = 480;
        private readonly object captureLock = new object(); // Thread safety lock
        private int frameCounter = 0; // Debug counter for frame captures

        public bool IsCapturing => isCapturing;
        public int CameraIndex => cameraIndex;

        public OpenCVCameraCapture()
        {
            InitializeUI();
            frame = new Mat();
        }

        private void InitializeUI()
        {
            videoImage = new Image
            {
                Stretch = Stretch.UniformToFill,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            };

            // Create a loading text overlay
            var loadingText = new TextBlock
            {
                Text = "🎥 Starting Camera...",
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Background = new SolidColorBrush(Color.FromArgb(128, 0, 0, 0)) // Semi-transparent background
            };

            // Create a Grid to hold the video image and loading text
            var grid = new Grid
            {
                Background = Brushes.Black
            };
            
            grid.Children.Add(videoImage);
            grid.Children.Add(loadingText);
            
            this.Content = grid;
            this.Background = Brushes.Black;
            
            // Hide loading text once camera starts (will be handled in StartCaptureAsync)
            loadingText.Name = "LoadingText";
        }

        public async Task<bool> StartCaptureAsync(int cameraIndex = 0, int width = 640, int height = 480)
        {
            if (isCapturing)
            {
                Console.WriteLine("[WARNING] Camera is already capturing");
                return true;
            }

            try
            {
                Console.WriteLine($"[DEBUG] Starting OpenCV camera capture for camera index: {cameraIndex}");
                
                this.cameraIndex = cameraIndex;
                this.frameWidth = width;
                this.frameHeight = height;

                // Initialize OpenCV VideoCapture with more conservative settings
                await Task.Run(() =>
                {
                    Console.WriteLine("[DEBUG] Attempting to open camera with DirectShow backend...");
                    
                    // Try DirectShow first (more stable on Windows)
                    capture = new VideoCapture(cameraIndex, VideoCaptureAPIs.DSHOW);
                    
                    if (!capture.IsOpened())
                    {
                        Console.WriteLine("[DEBUG] DirectShow failed, trying default backend...");
                        capture?.Dispose();
                        capture = new VideoCapture(cameraIndex);
                    }
                    
                    if (!capture.IsOpened())
                    {
                        throw new Exception($"Failed to open camera {cameraIndex}");
                    }

                    // Set camera properties with error handling
                    try
                    {
                        capture.Set(VideoCaptureProperties.FrameWidth, width);
                        capture.Set(VideoCaptureProperties.FrameHeight, height);
                        capture.Set(VideoCaptureProperties.Fps, 15); // Lower FPS for stability
                        capture.Set(VideoCaptureProperties.BufferSize, 1);
                        
                        // Test capture to ensure it works
                        using (var testFrame = new Mat())
                        {
                            if (!capture.Read(testFrame) || testFrame.Empty())
                            {
                                throw new Exception("Camera test capture failed - camera may be in use");
                            }
                        }
                        
                        Console.WriteLine($"[DEBUG] Camera opened successfully with backend: {capture.GetBackendName()}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[WARNING] Camera property setup failed: {ex.Message}");
                        // Continue anyway, camera might still work with default settings
                    }
                });

                // Start capture timer
                StartCaptureTimer();
                isCapturing = true;

                // Hide loading text
                await Dispatcher.InvokeAsync(() =>
                {
                    if (this.Content is Grid grid)
                    {
                        var loadingText = grid.Children.OfType<TextBlock>().FirstOrDefault();
                        if (loadingText != null)
                        {
                            grid.Children.Remove(loadingText);
                        }
                    }
                });

                Console.WriteLine("[DEBUG] OpenCV camera capture started successfully");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Failed to start OpenCV camera capture: {ex.Message}");
                return false;
            }
        }

        private void StartCaptureTimer()
        {
            captureTimer = new DispatcherTimer(DispatcherPriority.Normal) // Explicit priority
            {
                Interval = TimeSpan.FromMilliseconds(66) // ~15 FPS for stability
            };
            
            captureTimer.Tick += (s, e) =>
            {
                CaptureFrame();
            };
            
            captureTimer.Start();
            Console.WriteLine("[DEBUG] Capture timer started with Normal priority");
        }

        private void CaptureFrame()
        {
            if (capture == null || !capture.IsOpened() || isDisposed)
                return;

            // Use lock to ensure thread safety
            lock (captureLock)
            {
                if (capture == null || isDisposed) // Double-check inside lock
                    return;

                try
                {
                    // Use synchronous capture on UI thread to avoid thread safety issues
                    bool frameRead = false;
                    
                    // Create a temporary frame for this capture
                    using (var tempFrame = new Mat())
                    {
                        // Capture directly with proper error handling
                        frameRead = capture.Read(tempFrame);
                        
                        if (!frameRead || tempFrame.Empty())
                        {
                            // Skip this frame but don't spam console with warnings
                            return;
                        }

                        // Copy to our main frame safely
                        if (!frame.IsDisposed)
                        {
                            tempFrame.CopyTo(frame);
                        }
                    }

                    // Convert to WPF-compatible bitmap on UI thread
                    if (!frame.IsDisposed && !frame.Empty())
                    {
                        try
                        {
                            var bitmap = frame.ToBitmapSource();
                            videoImage.Source = bitmap;
                            
                            // Debug: Occasionally log that capture is working (every 100 frames = ~6 seconds)
                            if (frameCounter++ % 100 == 0)
                            {
                                Console.WriteLine($"[DEBUG] Camera frame {frameCounter} captured successfully");
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[ERROR] Failed to convert frame to bitmap: {ex.Message}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Log the actual error but stop capture to prevent crashes
                    Console.WriteLine($"[ERROR] Failed to capture frame: {ex.Message}");
                    
                    // If we get memory access violations, stop capture to prevent crash
                    if (ex.Message.Contains("0xC0000005") || ex is AccessViolationException)
                    {
                        Console.WriteLine("[ERROR] Memory access violation detected - stopping capture for safety");
                        Dispatcher.BeginInvoke(() => StopCapture());
                    }
                }
            }
        }

        public void StopCapture()
        {
            if (!isCapturing) return;

            lock (captureLock)
            {
                try
                {
                    Console.WriteLine("[DEBUG] Stopping OpenCV camera capture...");
                    
                    if (captureTimer != null)
                    {
                        captureTimer.Stop();
                        captureTimer.Tick -= (s, e) => CaptureFrame();
                        captureTimer = null;
                    }

                    if (capture != null)
                    {
                        capture.Release();
                        capture.Dispose();
                        capture = null;
                    }

                    isCapturing = false;
                    Console.WriteLine("[DEBUG] OpenCV camera capture stopped");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ERROR] Failed to stop OpenCV camera capture: {ex.Message}");
                }
            }
        }

        public void SetResolution(int width, int height)
        {
            frameWidth = width;
            frameHeight = height;

            if (isCapturing && capture != null)
            {
                try
                {
                    capture.Set(VideoCaptureProperties.FrameWidth, width);
                    capture.Set(VideoCaptureProperties.FrameHeight, height);
                    Console.WriteLine($"[DEBUG] Camera resolution set to {width}x{height}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ERROR] Failed to set camera resolution: {ex.Message}");
                }
            }
        }

        public static async Task<int> GetAvailableCameraCountAsync()
        {
            return await Task.Run(() =>
            {
                int count = 0;
                Console.WriteLine("[DEBUG] Scanning for available cameras...");
                
                // Check fewer indices and use DirectShow backend to reduce warnings
                for (int i = 0; i < 3; i++) // Check first 3 camera indices only
                {
                    try
                    {
                        // Try DirectShow first (more reliable on Windows)
                        using (var testCapture = new VideoCapture(i, VideoCaptureAPIs.DSHOW))
                        {
                            if (testCapture.IsOpened())
                            {
                                // Test if camera can actually capture
                                using (var testFrame = new Mat())
                                {
                                    if (testCapture.Read(testFrame) && !testFrame.Empty())
                                    {
                                        count++;
                                        Console.WriteLine($"[DEBUG] ✓ Camera {i} available and working (DirectShow)");
                                        continue;
                                    }
                                }
                            }
                        }
                        
                        // If DirectShow failed, try default backend
                        using (var testCapture = new VideoCapture(i))
                        {
                            if (testCapture.IsOpened())
                            {
                                using (var testFrame = new Mat())
                                {
                                    if (testCapture.Read(testFrame) && !testFrame.Empty())
                                    {
                                        count++;
                                        Console.WriteLine($"[DEBUG] ✓ Camera {i} available and working (Default)");
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[DEBUG] Camera {i} test failed: {ex.Message}");
                        // Don't break, continue checking other indices
                    }
                }
                
                Console.WriteLine($"[DEBUG] Found {count} working cameras");
                return count;
            });
        }

        public void Dispose()
        {
            if (isDisposed) return;

            try
            {
                Console.WriteLine("[DEBUG] Disposing OpenCVCameraCapture...");
                
                isDisposed = true;
                StopCapture();
                
                frame?.Dispose();
                frame = null;

                Console.WriteLine("[DEBUG] OpenCVCameraCapture disposed");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Error disposing OpenCVCameraCapture: {ex.Message}");
            }
        }
    }
}
