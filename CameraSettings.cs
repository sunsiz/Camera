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
    }
}
