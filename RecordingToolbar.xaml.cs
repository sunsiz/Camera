using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace CameraOverlay
{
    public partial class RecordingToolbar : Window
    {
        private ScreenRecorder screenRecorder;
        private bool isRecording = false;
        private DispatcherTimer durationTimer;
        private DateTime recordingStartTime;
        private Window parentWindow;

        public RecordingToolbar(Window parent)
        {
            InitializeComponent();
            parentWindow = parent;
            
            // Position toolbar at the top of the camera window
            PositionAboveCameraWindow();
            
            // Subscribe to parent window events to move together
            if (parentWindow != null)
            {
                parentWindow.LocationChanged += ParentWindow_LocationChanged;
                parentWindow.SizeChanged += ParentWindow_SizeChanged;
            }
            
            // Initialize duration timer
            durationTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            durationTimer.Tick += UpdateDuration;
            
            Loaded += RecordingToolbar_Loaded;
        }

        private void PositionAboveCameraWindow()
        {
            if (parentWindow != null)
            {
                // Position the toolbar centered above the camera window
                Left = parentWindow.Left + (parentWindow.Width - Width) / 2;
                Top = parentWindow.Top - Height - 5; // 5px gap above the camera window
            }
        }

        private void ParentWindow_LocationChanged(object sender, EventArgs e)
        {
            // Move toolbar when camera window moves
            PositionAboveCameraWindow();
        }

        private void ParentWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            // Reposition toolbar when camera window is resized
            PositionAboveCameraWindow();
        }

        private void RecordingToolbar_Loaded(object sender, RoutedEventArgs e)
        {
            // Make sure toolbar stays on top
            Topmost = true;
        }

        private async void RecordButton_Click(object sender, RoutedEventArgs e)
        {
            if (!isRecording)
            {
                await StartRecording();
            }
            else
            {
                await StopRecording();
            }
        }

        private async Task StartRecording()
        {
            try
            {
                var settings = new CameraSettings(); // Use default settings
                screenRecorder = new ScreenRecorder(settings, parentWindow);
                
                // Subscribe to progress updates
                screenRecorder.ProgressChanged += OnRecordingProgressChanged;
                
                bool started = await screenRecorder.StartRecordingAsync();
                if (started)
                {
                    isRecording = true;
                    recordingStartTime = DateTime.Now;
                    
                    // Update UI
                    RecordButton.Content = "⏹";
                    RecordButton.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(40, 167, 69)); // Green
                    StatusText.Text = "Recording";
                    DurationText.Text = "00:00";
                    
                    // Start duration timer
                    durationTimer.Start();
                    
                    Console.WriteLine("[DEBUG] Recording started from toolbar");
                }
                else
                {
                    StatusText.Text = "Start failed";
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Failed to start recording: {ex.Message}");
                StatusText.Text = "Error";
            }
        }

        private async Task StopRecording()
        {
            try
            {
                if (screenRecorder != null)
                {
                    StatusText.Text = "Stopping";
                    durationTimer.Stop();
                    
                    bool stopped = await screenRecorder.StopRecordingAsync();
                    if (stopped)
                    {
                        StatusText.Text = "Saved!";
                        await Task.Delay(2000); // Show success message for 2 seconds
                    }
                    else
                    {
                        StatusText.Text = "Save failed";
                    }
                    
                    screenRecorder.Dispose();
                    screenRecorder = null;
                }
                
                isRecording = false;
                
                // Update UI
                RecordButton.Content = "⏺";
                RecordButton.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(220, 53, 69)); // Red
                StatusText.Text = "Ready";
                DurationText.Text = "00:00";
                
                Console.WriteLine("[DEBUG] Recording stopped from toolbar");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Failed to stop recording: {ex.Message}");
                StatusText.Text = "Stop failed";
                isRecording = false;
            }
        }

        private void UpdateDuration(object sender, EventArgs e)
        {
            if (isRecording)
            {
                var duration = DateTime.Now - recordingStartTime;
                DurationText.Text = $"{duration.Minutes:D2}:{duration.Seconds:D2}";
            }
        }

        private void OnRecordingProgressChanged(string progress)
        {
            Dispatcher.BeginInvoke(() =>
            {
                // Only update status if it's not a simple duration update
                if (!progress.Contains("Recording...") || !isRecording)
                {
                    var cleanProgress = progress.Replace("🔴 ", "").Replace("⏹️ ", "").Replace("💾 ", "").Replace("✅ ", "").Replace("❌ ", "");
                    // Truncate long messages for compact display
                    if (cleanProgress.Length > 15)
                    {
                        cleanProgress = cleanProgress.Substring(0, 12) + "...";
                    }
                    StatusText.Text = cleanProgress;
                }
            });
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            // Don't actually close, just hide
            e.Cancel = true;
            Hide();
        }

        public async void ForceClose()
        {
            if (isRecording)
            {
                await StopRecording();
            }
            
            durationTimer?.Stop();
            screenRecorder?.Dispose();
            
            // Unsubscribe from parent window events
            if (parentWindow != null)
            {
                parentWindow.LocationChanged -= ParentWindow_LocationChanged;
                parentWindow.SizeChanged -= ParentWindow_SizeChanged;
            }
            
            // Actually close the window
            base.OnClosing(new System.ComponentModel.CancelEventArgs(false));
            Close();
        }
    }
}
