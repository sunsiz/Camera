@echo off
title Camera Overlay v1.9.5 - FINAL FIX for RefreshDisplay Issue
echo Starting Camera Overlay v1.9.5 - FINAL FIX for RefreshDisplay Issue
echo.
echo *** BREAKTHROUGH DISCOVERY ***
echo The issue was found! RefreshDisplay() was always calling ShowCameraPlaceholder()
echo even after ShowCameraFeed() successfully displayed green "CAMERA READY"
echo.
echo The problem sequence:
echo 1. Camera initializes → ShowCameraFeed() → GREEN "CAMERA READY"
echo 2. Window resize event → RefreshDisplay() → ShowCameraPlaceholder() → YELLOW "CAMERA DETECTED"
echo.
echo v1.9.5 FIX:
echo - RefreshDisplay() now re-checks camera status instead of always showing placeholder
echo - Should maintain GREEN "CAMERA READY" status after window resize/refresh events
echo.
echo Expected result: GREEN "CAMERA READY" status that STAYS green!
echo.
echo Press any key to continue...
pause > nul

cd /d "%~dp0"
cd publish-v1.9.5
start CameraOverlay.exe
