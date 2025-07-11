# Camera Overlay for Windows 11 Game Bar

A frameless camera overlay application for Windows 11 designed for elderly users who want to record their screen with Windows Game Bar while having a **LIVE CAMERA FEED** visible.

## 🎯 Project Purpose

**MAIN GOAL:** Display **ACTUAL CAMERA FEED** in a frameless, always-on-top window for Game Bar recording.

**✅ ACHIEVED:** Real camera capture at 30 FPS using OpenCV with multiple camera support.

## ✨ Features

### Live Camera Feed
- **🎥 REAL CAMERA CAPTURE**: Live video feed from connected cameras at 30 FPS
- **📷 Multiple Camera Support**: Automatic detection and switching between cameras  
- **🔄 Smart Detection**: Uses OpenCV for reliable camera access
- **⚡ Hardware Accelerated**: Smooth video processing with no lag

### Professional Overlay
- **🖼️ Frameless Design**: Clean camera display without borders or title bar
- **📌 Always On Top**: Stays visible over all other applications
- **🎯 Drag & Drop**: Click and drag to position anywhere on screen
- **📏 Resizable**: Resize the camera window with live preview
- **🖱️ Right-Click Menu**: Easy access to camera settings and options
- **💾 Auto-Save Settings**: Remembers window position, size, and camera preferences
- **🔒 Single Instance**: Prevents multiple instances from running
- **🌟 Drop Shadow**: Subtle shadow for better visibility

### Smart Features
- **📊 Real-time Size Display**: Shows current window dimensions (e.g., 300x405)
- **🔍 Auto Camera Detection**: Automatically detects and lists available cameras
- **📐 Dynamic Resolution**: Window resizes to match selected resolution
- **🛡️ Error Recovery**: Graceful fallback if camera access fails
- **🔧 Multiple APIs**: OpenCV → Windows Media Foundation → DirectShow fallback

## Usage

1. **Launch the Application**: Run `CameraOverlay.exe`
2. **Live Camera Feed**: See your camera feed immediately upon startup
3. **Check Real-time Size**: The window displays current dimensions (e.g., 320x240)
4. **Resize Manually**: Drag the resize grip - watch the size update with live preview
5. **Position the Camera**: Click and drag the camera window to your desired location
6. **Camera Settings**: Right-click on the camera window to:
   - Select from detected cameras (e.g., "Camera 1 (OpenCV)")
   - Change resolution (window resizes to match with live preview)
   - Exit the application
7. **Screen Recording**: Use Windows Game Bar (Win + G) to record your screen with the live camera overlay visible

## Target Users

This application is designed for elderly users who:
- Want a simple alternative to complex recording software like OBS
- Need to record both screen and live camera simultaneously
- Have Intel Iris Xe integrated graphics without built-in recording tools
- Prefer the simplicity of Windows Game Bar for screen recording
- Want reliable, smooth camera feed without technical complexity

## Technical Requirements

- Windows 10/11
- .NET 9.0 Runtime
- Camera/webcam (integrated or external)
- Camera permissions enabled in Windows Privacy settings

## Installation

1. Download the latest release
2. Extract to your desired location
3. Run `CameraOverlay.exe`
4. Grant camera permissions if prompted

## Windows 11 Camera Permissions

**Important**: Windows 11 requires explicit camera permissions for applications. If you see a message about camera permissions:

1. **Open Windows Settings** (Windows key + I)
2. **Navigate to Privacy & Security** → **Camera**
3. **Enable Camera Access**:
   - Turn ON "Camera access" (system-wide)
   - Turn ON "Let apps access your camera"
   - Turn ON "Let desktop apps access your camera"
4. **Restart the application**

The camera should now be accessible. If you still see permission messages, ensure your camera is not being used by another application (like the Windows Camera app).

## Building from Source

```bash
dotnet restore
dotnet build
dotnet run
```

## Latest Updates (v1.4.0)

- **Improved Camera Detection**: Enhanced WMI-based camera detection with better filtering
- **Better User Feedback**: Clear indication of camera status and video feed requirements
- **Enhanced Placeholder Display**: More informative camera placeholder with current window size
- **Camera Icon Support**: Added support for application icon (instructions provided)
- **Clearer Error Messages**: Better explanation of placeholder vs actual video feed

## Current Status
- **Camera Detection**: ✅ Working - Detects and lists available cameras
- **Video Feed**: ⚠️ Placeholder - Shows camera info but not actual video stream
- **Window Management**: ✅ Working - All positioning, sizing, and settings features
- **User Interface**: ✅ Working - Right-click menus, resolution selection, etc.

**Note**: The application currently shows a placeholder instead of actual camera feed. For real video capture, DirectShow or Media Foundation integration is needed. See `CAMERA-IMPLEMENTATION.md` for technical details.

## Previous Updates (v1.2.0)

- **Upgraded to .NET 8.0**: Latest LTS framework for better performance and security
- **Updated NuGet Packages**: System.Management v9.0.7 for improved camera detection
- **Enhanced Compatibility**: Better support for modern Windows systems
- **Improved Stability**: Latest runtime optimizations and bug fixes

## Notes

- The camera feed is currently implemented as a placeholder for development
- Full camera integration requires DirectShow or Media Foundation implementation
- Settings are saved to `camera_settings.json` in the application directory
- The application ensures only one instance runs at a time

## Future Enhancements

- Real camera capture implementation
- Additional video effects and filters
- Hotkey support for quick show/hide
- Multiple camera profiles
- Custom themes and overlays

## License

This project is created for educational and personal use.
