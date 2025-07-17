using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Collections.Generic;
using System.Diagnostics;
using SharpAvi;
using SharpAvi.Output;
using SharpAvi.Codecs;

namespace CameraOverlay
{
    /// <summary>
    /// Custom screen recorder that captures desktop and overlays camera feed using SharpAVI
    /// </summary>
    public class ScreenRecorder : IDisposable
    {
        private Thread recordingThread;
        private bool isRecording = false;
        private CancellationTokenSource cancellationTokenSource;
        
        // Recording settings
        private readonly int frameRate;
        private readonly string outputPath;
        private readonly bool includeCameraOverlay;
        
        // Progress reporting
        public event Action<string> ProgressChanged;
        
        // Screen capture
        private readonly int screenWidth;
        private readonly int screenHeight;
        
        // Camera overlay
        private Window cameraWindow;
        private System.Drawing.Point cameraPosition;
        private System.Drawing.Size cameraSize;
        
        // SharpAVI components
        private AviWriter aviWriter;
        private IAviVideoStream videoStream;
        private int frameIndex = 0;
        
        // Win32 API for screen capture
        [DllImport("user32.dll")]
        private static extern IntPtr GetDC(IntPtr hWnd);
        
        [DllImport("user32.dll")]
        private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);
        
        [DllImport("user32.dll")]
        private static extern int GetSystemMetrics(int nIndex);
        
        [DllImport("gdi32.dll")]
        private static extern IntPtr CreateCompatibleDC(IntPtr hDC);
        
        [DllImport("gdi32.dll")]
        private static extern IntPtr CreateCompatibleBitmap(IntPtr hDC, int nWidth, int nHeight);
        
        [DllImport("gdi32.dll")]
        private static extern IntPtr SelectObject(IntPtr hDC, IntPtr hgdiobj);
        
        [DllImport("gdi32.dll")]
        private static extern bool BitBlt(IntPtr hDCDest, int XDest, int YDest, int nWidth, int nHeight, IntPtr hDCSrc, int XSrc, int YSrc, uint dwRop);
        
        [DllImport("gdi32.dll")]
        private static extern bool DeleteDC(IntPtr hDC);
        
        [DllImport("gdi32.dll")]
        private static extern bool DeleteObject(IntPtr hObject);
        
        private const uint SRCCOPY = 0x00CC0020;

        public ScreenRecorder(CameraSettings settings, Window cameraWindow = null)
        {
            this.frameRate = settings.RecordingFrameRate;
            this.includeCameraOverlay = settings.IncludeCameraOverlay;
            this.cameraWindow = cameraWindow;
            
            // Get screen dimensions - use GetSystemMetrics for accurate full screen size
            screenWidth = GetSystemMetrics(0); // SM_CXSCREEN
            screenHeight = GetSystemMetrics(1); // SM_CYSCREEN
            
            Console.WriteLine($"[DEBUG] Screen capture dimensions: {screenWidth}x{screenHeight}");
            
            // Set output path
            if (string.IsNullOrEmpty(settings.RecordingOutputPath))
            {
                string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                string fileName = $"CameraOverlay_Recording_{DateTime.Now:yyyyMMdd_HHmmss}.avi";
                this.outputPath = Path.Combine(desktopPath, fileName);
            }
            else
            {
                this.outputPath = settings.RecordingOutputPath;
            }
            
            // Get camera overlay position and size if available
            UpdateCameraPosition();
        }

        public bool IsRecording => isRecording;
        public string OutputPath => outputPath;

        public Task<bool> StartRecordingAsync()
        {
            if (isRecording)
            {
                Console.WriteLine("[DEBUG] Recording is already in progress");
                return Task.FromResult(false);
            }

            try
            {
                Console.WriteLine($"[DEBUG] Starting screen recording to: {outputPath}");
                
                // Initialize SharpAVI writer
                aviWriter = new AviWriter(outputPath)
                {
                    FramesPerSecond = frameRate,
                    EmitIndex1 = true
                };
                
                // Create video stream with MJPEG compression for smaller file size
                videoStream = aviWriter.AddVideoStream();
                videoStream.Width = screenWidth;
                videoStream.Height = screenHeight;
                videoStream.Codec = new FourCC("MJPG");
                videoStream.BitsPerPixel = BitsPerPixel.Bpp24;
                
                frameIndex = 0;
                
                ProgressChanged?.Invoke("🔴 Recording started...");
                
                // Start recording thread
                cancellationTokenSource = new CancellationTokenSource();
                isRecording = true;
                
                recordingThread = new Thread(() => RecordingLoop(cancellationTokenSource.Token))
                {
                    IsBackground = true,
                    Name = "ScreenRecordingThread"
                };
                recordingThread.Start();

                Console.WriteLine("[DEBUG] ✓ Screen recording started successfully with SharpAVI");
                return Task.FromResult(true);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Failed to start screen recording: {ex.Message}");
                Cleanup();
                ProgressChanged?.Invoke("❌ Failed to start recording");
                return Task.FromResult(false);
            }
        }

        public async Task<bool> StopRecordingAsync()
        {
            if (!isRecording)
            {
                Console.WriteLine("[DEBUG] No recording in progress");
                return true;
            }

            try
            {
                Console.WriteLine("[DEBUG] Stopping screen recording...");
                ProgressChanged?.Invoke("⏹️ Stopping recording...");
                
                isRecording = false;
                cancellationTokenSource?.Cancel();
                
                // Wait for recording thread to finish
                await Task.Run(() =>
                {
                    if (recordingThread != null && recordingThread.IsAlive)
                    {
                        recordingThread.Join(5000); // Wait up to 5 seconds
                    }
                });

                // Close the AVI writer
                ProgressChanged?.Invoke("💾 Finalizing video file...");
                
                try
                {
                    aviWriter?.Close();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ERROR] Error closing AVI writer: {ex.Message}");
                }
                
                Console.WriteLine($"[DEBUG] Recording captured {frameIndex} frames at {frameRate} FPS");
                Console.WriteLine($"[DEBUG] Recording duration: approximately {(double)frameIndex / frameRate:F1} seconds");
                
                if (File.Exists(outputPath))
                {
                    var fileInfo = new FileInfo(outputPath);
                    Console.WriteLine($"[DEBUG] ✓ Screen recording saved to: {outputPath}");
                    Console.WriteLine($"[DEBUG] Video file size: {fileInfo.Length / 1024 / 1024:F1} MB");
                    ProgressChanged?.Invoke($"✅ Video saved! ({fileInfo.Length / 1024 / 1024:F1} MB)");
                }
                else
                {
                    Console.WriteLine("[ERROR] Video file was not created");
                    ProgressChanged?.Invoke("❌ Video file not created");
                    return false;
                }
                
                Cleanup();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Failed to stop screen recording: {ex.Message}");
                ProgressChanged?.Invoke("❌ Error stopping recording");
                return false;
            }
        }

        private void UpdateCameraPosition()
        {
            if (cameraWindow != null && includeCameraOverlay)
            {
                try
                {
                    cameraWindow.Dispatcher.Invoke(() =>
                    {
                        cameraPosition = new System.Drawing.Point(
                            (int)cameraWindow.Left,
                            (int)cameraWindow.Top
                        );
                        cameraSize = new System.Drawing.Size(
                            (int)cameraWindow.ActualWidth,
                            (int)cameraWindow.ActualHeight
                        );
                    });
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[DEBUG] Could not update camera position: {ex.Message}");
                }
            }
        }

        private void RecordingLoop(CancellationToken cancellationToken)
        {
            var frameIntervalMs = (int)(1000.0 / frameRate);
            var stopwatch = Stopwatch.StartNew();

            Console.WriteLine($"[DEBUG] Recording loop started with SharpAVI - Frame rate: {frameRate} FPS, Interval: {frameIntervalMs}ms");

            try
            {
                while (!cancellationToken.IsCancellationRequested && isRecording)
                {
                    var frameStartTime = stopwatch.ElapsedMilliseconds;
                    
                    // Capture frame
                    var frame = CaptureScreen();
                    if (frame != null)
                    {
                        // Overlay camera if enabled
                        if (includeCameraOverlay && cameraWindow != null)
                        {
                            UpdateCameraPosition();
                            frame = OverlayCameraWindow(frame);
                        }
                        
                        // Write frame directly to AVI using SharpAVI
                        WriteFrameToAvi(frame);
                        
                        frame.Dispose();
                        frameIndex++;
                        
                        // Show progress every 30 frames (about every second at 30fps)
                        if (frameIndex % 30 == 0)
                        {
                            double recordingDuration = (double)frameIndex / frameRate;
                            ProgressChanged?.Invoke($"🔴 Recording... {recordingDuration:F1}s ({frameIndex} frames)");
                        }
                    }
                    
                    // Calculate how long the frame capture took
                    var frameCaptureTime = stopwatch.ElapsedMilliseconds - frameStartTime;
                    
                    // Wait for the remaining time to maintain precise frame rate
                    var remainingTime = frameIntervalMs - frameCaptureTime;
                    if (remainingTime > 0)
                    {
                        Thread.Sleep((int)remainingTime);
                    }
                    else if (frameIndex % 60 == 0) // Only log every 60 frames to avoid spam
                    {
                        Console.WriteLine($"[DEBUG] Frame {frameIndex}: Capture took {frameCaptureTime}ms (longer than {frameIntervalMs}ms interval!)");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Recording loop error: {ex.Message}");
                ProgressChanged?.Invoke("❌ Recording error occurred");
            }
        }

        private void WriteFrameToAvi(Bitmap frame)
        {
            try
            {
                // Convert bitmap to JPEG bytes for MJPEG compression
                using (var memoryStream = new MemoryStream())
                {
                    // Use JPEG encoder with good quality settings
                    var jpegEncoder = GetJpegEncoder();
                    var encoderParams = new EncoderParameters(1);
                    encoderParams.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, 85L); // 85% quality
                    
                    frame.Save(memoryStream, jpegEncoder, encoderParams);
                    var jpegBytes = memoryStream.ToArray();
                    
                    // Write the JPEG frame to video stream
                    videoStream.WriteFrame(true, jpegBytes, 0, jpegBytes.Length);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Failed to write frame to AVI: {ex.Message}");
            }
        }

        private ImageCodecInfo GetJpegEncoder()
        {
            ImageCodecInfo[] codecs = ImageCodecInfo.GetImageEncoders();
            foreach (ImageCodecInfo codec in codecs)
            {
                if (codec.FormatID == ImageFormat.Jpeg.Guid)
                {
                    return codec;
                }
            }
            throw new NotSupportedException("JPEG encoder not found");
        }

        private Bitmap CaptureScreen()
        {
            try
            {
                IntPtr desktopDC = GetDC(IntPtr.Zero);
                IntPtr memoryDC = CreateCompatibleDC(desktopDC);
                IntPtr bitmap = CreateCompatibleBitmap(desktopDC, screenWidth, screenHeight);
                IntPtr oldBitmap = SelectObject(memoryDC, bitmap);

                BitBlt(memoryDC, 0, 0, screenWidth, screenHeight, desktopDC, 0, 0, SRCCOPY);

                SelectObject(memoryDC, oldBitmap);
                DeleteDC(memoryDC);
                ReleaseDC(IntPtr.Zero, desktopDC);

                Bitmap result = Image.FromHbitmap(bitmap);
                DeleteObject(bitmap);

                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Screen capture failed: {ex.Message}");
                return null;
            }
        }

        private Bitmap OverlayCameraWindow(Bitmap screenCapture)
        {
            // Simply return the screen capture without any overlay
            // The camera window is naturally visible in the recording
            // No need to add REC indicators that interfere with the video
            return screenCapture;
        }

        private void Cleanup()
        {
            try
            {
                cancellationTokenSource?.Dispose();
                
                try
                {
                    aviWriter?.Close();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ERROR] Error closing AVI writer during cleanup: {ex.Message}");
                }
                
                aviWriter = null;
                videoStream = null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Cleanup error: {ex.Message}");
            }
            finally
            {
                cancellationTokenSource = null;
                recordingThread = null;
                frameIndex = 0;
            }
        }

        public void Dispose()
        {
            if (isRecording)
            {
                StopRecordingAsync().Wait();
            }
            Cleanup();
        }
    }
}
