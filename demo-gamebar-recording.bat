@echo off
echo.
echo ========================================
echo   Camera Overlay with Game Bar Demo
echo ========================================
echo.
echo This demo will show you how to use the new Game Bar recording feature:
echo.
echo 1. Camera Overlay will start
echo 2. Position the camera window where you want it
echo 3. Right-click on the camera window
echo 4. Look for "🎮 Game Bar Recording" menu
echo 5. Click "⏺️ Start Recording" to begin
echo 6. Everything on screen will be recorded!
echo.
pause
echo.
echo Starting Camera Overlay...
echo.
cd /d "%~dp0"
start "" ".\bin\Debug\net9.0-windows\CameraOverlay.exe"
echo.
echo ✅ Camera Overlay started!
echo.
echo 📋 Quick Instructions:
echo   • Position camera by dragging
echo   • Resize by dragging corners  
echo   • Right-click for Game Bar menu
echo   • Select "Start Recording" 
echo   • Record your screen + camera!
echo.
echo 🎯 Perfect for elderly users - no keyboard shortcuts needed!
echo.
pause
