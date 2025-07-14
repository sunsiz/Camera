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
    /// Safer camera capture implementation with minimal OpenCV calls
    /// Use this if the main OpenCVCameraCapture crashes
    /// </summary>
    public class SafeCameraCapture : UserControl, IDisposable
    {
        private Image videoImage;
        private VideoCapture capture;
        private DispatcherTimer captureTimer;
        private Mat frame;
        private bool isCapturing = false;
        private bool isDisposed = false;
        private int cameraIndex = 0;
        private readonly object captureLock = new object();

        public bool IsCapturing => isCapturing;
        public int CameraIndex => cameraIndex;

        public SafeCameraCapture()
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

            var grid = new Grid { Background = Brushes.Black };
            grid.Children.Add(videoImage);
            this.Content = grid;
            this.Background = Brushes.Black;
        }

        public async Task<bool> StartCaptureAsync(int cameraIndex = 0, int width = 640, int height = 480)
        {
            if (isCapturing) return true;

            try
            {
                Console.WriteLine($"[DEBUG] [SAFE] Starting camera capture for index: {cameraIndex}");
                
                this.cameraIndex = cameraIndex;

                await Task.Run(() =>
                {
                    // Use only default backend for maximum compatibility
                    capture = new VideoCapture(cameraIndex);
                    
                    if (!capture.IsOpened())
                    {
                        throw new Exception($"Failed to open camera {cameraIndex}");
                    }
                    
                    Console.WriteLine("[DEBUG] [SAFE] Camera opened successfully");
                });

                // Start with slower, safer timer
                captureTimer = new DispatcherTimer
                {
                    Interval = TimeSpan.FromMilliseconds(100) // 10 FPS for maximum stability
                };
                
                captureTimer.Tick += SafeCaptureFrame;
                captureTimer.Start();
                
                isCapturing = true;
                Console.WriteLine("[DEBUG] [SAFE] Camera capture started successfully");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] [SAFE] Failed to start camera capture: {ex.Message}");
                return false;
            }
        }

        private void SafeCaptureFrame(object sender, EventArgs e)
        {
            if (capture == null || !capture.IsOpened() || isDisposed)
                return;

            lock (captureLock)
            {
                if (capture == null || isDisposed) return;

                try
                {
                    // Minimal OpenCV operations
                    if (capture.Read(frame) && !frame.Empty())
                    {
                        var bitmap = frame.ToBitmapSource();
                        videoImage.Source = bitmap;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ERROR] [SAFE] Frame capture failed: {ex.Message}");
                    
                    // If any error occurs, stop immediately to prevent crashes
                    StopCapture();
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
                    Console.WriteLine("[DEBUG] [SAFE] Stopping camera capture...");
                    
                    if (captureTimer != null)
                    {
                        captureTimer.Stop();
                        captureTimer.Tick -= SafeCaptureFrame;
                        captureTimer = null;
                    }

                    if (capture != null)
                    {
                        capture.Release();
                        capture.Dispose();
                        capture = null;
                    }

                    isCapturing = false;
                    Console.WriteLine("[DEBUG] [SAFE] Camera capture stopped");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ERROR] [SAFE] Stop capture failed: {ex.Message}");
                }
            }
        }

        public void Dispose()
        {
            if (isDisposed) return;
            
            isDisposed = true;
            StopCapture();
            frame?.Dispose();
        }
    }
}
