@echo off
echo Camera Overlay v2.0 - Real MediaFoundation Camera Support
echo.
echo MAJOR UPDATE: This version includes REAL camera capture using Windows MediaFoundation API!
echo.
echo Key Features in v2.0:
echo - Real camera feed using Windows built-in MediaFoundation
echo - Live video streaming from your webcam
echo - Animated test pattern fallback if MediaFoundation fails
echo - Hardware-accelerated video processing
echo - Improved camera detection and validation
echo - Enhanced error handling and debug logging
echo.
echo What you'll see:
echo - Green background with LIVE camera feed overlay
echo - Real video frames from your webcam
echo - "LIVE: [Camera Name] - Real Camera Feed Active" indicator
echo - Smooth 30 FPS video playback
echo.
echo Technical Implementation:
echo - Uses Windows MediaFoundation COM interfaces
echo - DirectX-compatible video rendering
echo - WriteableBitmap for WPF integration
echo - Proper camera resource management
echo - Thread-safe frame updates
echo.
echo Starting Camera Overlay v2.0...
echo.

"publish-v2.0\CameraOverlay.exe"

pause
