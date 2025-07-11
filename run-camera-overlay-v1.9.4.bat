@echo off
title Camera Overlay v1.9.4 - Fixed UI Content Issue
echo Starting Camera Overlay v1.9.4 - Fixed UI Content Issue
echo.
echo This version fixes the core UI content management issue that was
echo preventing the GREEN "CAMERA READY" status from displaying.
echo.
echo Key fixes:
echo - Removed initial MediaElement content assignment
echo - Added UI thread safety checks
echo - Clear content before setting new UI
echo - Enhanced debug output for UI method calls
echo.
echo Expected debug output:
echo [DEBUG] Calling ShowCameraFeed() - should show GREEN 'CAMERA READY'
echo [DEBUG] ShowCameraFeed() UI created with green background and 'CAMERA READY' text
echo.
echo This should FINALLY show the green "CAMERA READY" status!
echo.
echo Press any key to continue...
pause > nul

cd /d "%~dp0"
cd publish-v1.9.4
start CameraOverlay.exe
