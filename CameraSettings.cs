using System;

namespace CameraOverlay
{
    public class CameraSettings
    {
        public double WindowLeft { get; set; } = 100;
        public double WindowTop { get; set; } = 100;
        public double WindowWidth { get; set; } = 320;
        public double WindowHeight { get; set; } = 240;
        public string SelectedCameraName { get; set; } = "";
        public string SelectedResolution { get; set; } = "320x240";
        
        // Screen recording settings
        public int RecordingFrameRate { get; set; } = 3; // Low frame rate for small file sizes
        public int RecordingQuality { get; set; } = 70; // 1-100
        public string RecordingOutputPath { get; set; } = "";
        public bool IncludeCameraOverlay { get; set; } = true;
        public bool RecordAudio { get; set; } = false; // Future feature
    }
}
