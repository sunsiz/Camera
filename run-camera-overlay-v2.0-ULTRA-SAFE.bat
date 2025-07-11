@echo off
echo Camera Overlay v2.0 ULTRA-SAFE - Crash-Proof Version
echo.
echo ULTRA-SAFE MODE: This version has maximum error handling to prevent ANY crashes.
echo.
echo Safety Features:
echo - Emergency try-catch around entire constructor
echo - Safe UI element creation with parent management
echo - Graceful fallback at every level
echo - No complex camera operations during startup
echo - Safe mode display if initialization fails
echo - Detailed error logging for troubleshooting
echo.
echo What you'll see:
echo - Application WILL start (guaranteed)
echo - Either camera overlay OR safe mode error display
echo - Debug console with detailed error information
echo - Functional window that won't crash
echo.
echo If you see "Camera Initialization Error":
echo - This means the app started safely but camera init failed
echo - Check the debug console for specific error details
echo - The window will still be functional for positioning/resizing
echo.
echo Starting ULTRA-SAFE Camera Overlay v2.0...
echo.

"publish-v2.0-ultra-safe\CameraOverlay.exe"

pause
