@echo off
title Camera Overlay v1.9.1 - Enhanced Device Validation
echo Starting Camera Overlay v1.9.1 - Enhanced Device Validation
echo.
echo This version includes improved camera device validation
echo to ensure proper "CAMERA READY" status display.
echo.
echo Expected behavior:
echo - DFU devices should be filtered out
echo - Valid cameras should show green "CAMERA READY" status  
echo - Debug console shows detailed validation steps
echo.
echo Press any key to continue...
pause > nul

cd /d "%~dp0"
cd publish-v1.9.1
start CameraOverlay.exe
