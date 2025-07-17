using System;
using System.Threading.Tasks;
using System.Windows;

namespace CameraOverlay
{
    /// <summary>
    /// Simple helper for custom screen recording functionality
    /// </summary>
    public class ScreenRecorderHelper
    {
        private ScreenRecorder screenRecorder;
        private bool isRecording = false;

        /// <summary>
        /// Gets whether recording is currently active
        /// </summary>
        public bool IsRecording => isRecording;

        // Progress reporting
        public event Action<string> ProgressChanged;

        /// <summary>
        /// Start custom screen recording with camera overlay
        /// </summary>
        public async Task<bool> StartRecordingAsync(Window cameraWindow, CameraSettings settings)
        {
            try
            {
                if (screenRecorder != null && screenRecorder.IsRecording)
                {
                    Console.WriteLine("[DEBUG] Screen recording is already in progress");
                    return false;
                }

                Console.WriteLine("[DEBUG] Starting custom screen recording...");
                
                screenRecorder = new ScreenRecorder(settings, cameraWindow);
                
                // Wire up progress events
                screenRecorder.ProgressChanged += (message) => ProgressChanged?.Invoke(message);
                
                bool started = await screenRecorder.StartRecordingAsync();
                
                if (started)
                {
                    isRecording = true;
                    Console.WriteLine("[DEBUG] ✓ Custom screen recording started successfully");
                }
                else
                {
                    Console.WriteLine("[ERROR] Failed to start screen recording");
                }
                
                return started;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Failed to start custom screen recording: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Stop custom screen recording
        /// </summary>
        public async Task<bool> StopRecordingAsync()
        {
            try
            {
                if (screenRecorder == null || !screenRecorder.IsRecording)
                {
                    Console.WriteLine("[DEBUG] No screen recording in progress");
                    isRecording = false;
                    return true;
                }

                Console.WriteLine("[DEBUG] Stopping custom screen recording...");
                
                bool stopped = await screenRecorder.StopRecordingAsync();
                if (stopped)
                {
                    isRecording = false;
                    Console.WriteLine($"[DEBUG] ✓ Screen recording saved to: {screenRecorder.OutputPath}");
                }
                
                screenRecorder?.Dispose();
                screenRecorder = null;
                
                return stopped;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Failed to stop custom screen recording: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Get the current output path
        /// </summary>
        public string GetOutputPath()
        {
            return screenRecorder?.OutputPath ?? "";
        }

        /// <summary>
        /// Show recording help information
        /// </summary>
        public static void ShowRecordingInfo()
        {
            string message = @"🎬 Screen Recording Help

CUSTOM SCREEN RECORDING:
• Right-click camera → 'Start Recording'
• Records entire desktop with camera overlay visible
• Camera window appears naturally in recording
• Clean recording without overlays or indicators
• Saves video files to Desktop folder

FEATURES:
• Full desktop capture at 3 FPS
• Very natural playback speed
• Camera position captured naturally
• Works on all Windows versions
• No external dependencies

OUTPUT:
• Video files saved as PNG frames first
• Automatically converted to MP4 if FFmpeg is available
• Files saved to Desktop with timestamp
• Check console for exact file location

TROUBLESHOOTING:
• If no video file appears, check Desktop for frame folders
• Ensure sufficient disk space for recording
• Close other resource-intensive applications
• Recording quality depends on system performance

The screen recorder captures everything visible on your desktop including the camera overlay window!";

            MessageBox.Show(message, "Screen Recording Help", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        
        /// <summary>
        /// Clean up resources
        /// </summary>
        public void Dispose()
        {
            screenRecorder?.Dispose();
            screenRecorder = null;
            isRecording = false;
        }
    }
}
