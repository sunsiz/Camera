namespace CameraOverlay
{
    public class CameraInfo
    {
        public string Name { get; set; }
        public string DevicePath { get; set; }
        public int Index { get; set; }

        public override string ToString()
        {
            return Name ?? "Unknown Camera";
        }
    }
}
