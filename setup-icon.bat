@echo off
echo Camera Overlay - Icon Setup Helper
echo.
echo This script will help you set up an application icon.
echo.
echo OPTION 1: Download a free icon
echo.
echo 1. Visit: https://www.flaticon.com/free-icon/video-camera_1864502
echo 2. Download as ICO format (32x32 or 48x48)
echo 3. Save as "camera-icon.ico" in the project folder
echo 4. Uncomment the ApplicationIcon line in CameraOverlay.csproj
echo.
echo OPTION 2: Use Windows built-in icons
echo.
echo 1. Copy an icon from: C:\Windows\System32\*.ico
echo 2. Or use: %SystemRoot%\System32\shell32.dll (contains many icons)
echo.
echo OPTION 3: Skip custom icon
echo.
echo The application will use Windows default executable icon.
echo.
echo After adding the icon file:
echo 1. Edit CameraOverlay.csproj
echo 2. Uncomment: ^<ApplicationIcon^>camera-icon.ico^</ApplicationIcon^>
echo 3. Run: dotnet build
echo.
pause
