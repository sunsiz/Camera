@echo off
echo Camera Overlay v2.0 FIXED - Stable Camera Support
echo.
echo CRASH FIX: This version fixes the startup crash caused by COM interface exceptions.
echo.
echo Key Improvements in v2.0 FIXED:
echo - Robust error handling prevents startup crashes
echo - Simplified MediaFoundation implementation
echo - Graceful fallback when camera APIs fail
echo - Enhanced exception handling throughout
echo - Stable initialization process
echo.
echo What you'll see:
echo - Application starts without crashing
echo - Green background with camera feed or test pattern
echo - Proper camera detection and status display
echo - Debug console for troubleshooting
echo.
echo Technical Fixes:
echo - Removed complex COM interface definitions that caused crashes
echo - Added try-catch blocks around all camera operations
echo - Simplified MediaFoundation calls
echo - Better fallback mechanisms
echo - Improved disposal and cleanup
echo.
echo Starting FIXED Camera Overlay v2.0...
echo.

"publish-v2.0-fixed\CameraOverlay.exe"

pause
