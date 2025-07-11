@echo off
title Camera Overlay v1.9.2 - Fixed Camera Ready Status
echo Starting Camera Overlay v1.9.2 - Fixed Camera Ready Status
echo.
echo This version fixes the camera validation logic to properly
echo show green "CAMERA READY" status for working cameras.
echo.
echo Key changes:
echo - Simplified validation logic 
echo - Removed DeviceID lookup dependency
echo - Camera should now show GREEN "CAMERA READY" status
echo.
echo Expected behavior:
echo - DFU devices filtered out
echo - Valid cameras show GREEN "CAMERA READY" status
echo - Debug console shows "Overall camera access result: True"
echo.
echo Press any key to continue...
pause > nul

cd /d "%~dp0"
cd publish-v1.9.2
start CameraOverlay.exe
