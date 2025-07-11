using System;
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

            // Create a Grid to hold the video image
            var grid = new Grid
            {
                Background = Brushes.Black
            };
            grid.Children.Add(videoImage);
            this.Content = grid;
            this.Background = Brushes.Black;
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

                // Initialize OpenCV VideoCapture
                await Task.Run(() =>
                {
                    capture = new VideoCapture(cameraIndex);
                    
                    if (!capture.IsOpened())
                    {
                        throw new Exception($"Failed to open camera {cameraIndex}");
                    }

                    // Set camera properties
                    capture.Set(VideoCaptureProperties.FrameWidth, width);
                    capture.Set(VideoCaptureProperties.FrameHeight, height);
                    capture.Set(VideoCaptureProperties.Fps, 30);
                });

                // Start capture timer
                StartCaptureTimer();
                isCapturing = true;

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
            captureTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(33) // ~30 FPS
            };
            
            captureTimer.Tick += async (s, e) =>
            {
                await CaptureFrame();
            };
            
            captureTimer.Start();
        }

        private async Task CaptureFrame()
        {
            if (capture == null || !capture.IsOpened() || isDisposed)
                return;

            try
            {
                await Task.Run(() =>
                {
                    // Capture frame from camera
                    capture.Read(frame);
                });

                if (frame.Empty())
                    return;

                // Convert to WPF-compatible bitmap
                await Dispatcher.InvokeAsync(() =>
                {
                    try
                    {
                        var bitmap = frame.ToBitmapSource();
                        videoImage.Source = bitmap;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[ERROR] Failed to convert frame to bitmap: {ex.Message}");
                    }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Failed to capture frame: {ex.Message}");
            }
        }

        public void StopCapture()
        {
            if (!isCapturing) return;

            try
            {
                Console.WriteLine("[DEBUG] Stopping OpenCV camera capture...");
                
                if (captureTimer != null)
                {
                    captureTimer.Stop();
                    captureTimer.Tick -= async (s, e) => await CaptureFrame();
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
                for (int i = 0; i < 10; i++) // Check first 10 camera indices
                {
                    try
                    {
                        using (var testCapture = new VideoCapture(i))
                        {
                            if (testCapture.IsOpened())
                            {
                                count++;
                            }
                        }
                    }
                    catch
                    {
                        // Camera index not available
                        break;
                    }
                }
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
