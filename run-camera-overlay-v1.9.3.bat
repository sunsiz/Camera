@echo off
title Camera Overlay v1.9.3 - Debug Camera Status Issue
echo Starting Camera Overlay v1.9.3 - Debug Camera Status Issue
echo.
echo This version includes enhanced debug output to track why
echo "CAMERA READY" status is not showing despite successful validation.
echo.
echo The debug console will now show:
echo - TryInitializeWebcam() return value
echo - Which UI method gets called (ShowCameraFeed vs ShowCameraPlaceholder)
echo - Improved resource management to prevent camera blocking
echo.
echo Expected debug output:
echo [SUCCESS] Camera Integrated Camera is ready for use
echo [DEBUG] TryInitializeWebcam() returned: True
echo [DEBUG] Calling ShowCameraFeed() - should show GREEN 'CAMERA READY'
echo.
echo If you still see "CAMERA DETECTED", the debug output will help identify the issue.
echo.
echo Press any key to continue...
pause > nul

cd /d "%~dp0"
cd publish-v1.9.3
start CameraOverlay.exe
