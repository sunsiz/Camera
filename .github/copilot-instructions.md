<!-- Use this file to provide workspace-specific custom instructions to Copilot. For more details, visit https://code.visualstudio.com/docs/copilot/copilot-customization#_use-a-githubcopilotinstructionsmd-file -->

# Camera Overlay WPF Application

This is a WPF application that creates a frameless camera overlay window for Windows 11. The application is designed to be simple and user-friendly for elderly users who want to record their screen with Windows Game Bar while having a camera feed visible.

## Key Features
- Frameless, always-on-top camera window
- Drag and drop positioning
- Resizable window with grip
- Right-click context menu for camera selection and resolution settings
- Automatic position and size saving
- Drop shadow for better visibility
- Single instance application

## Technical Details
- Built with .NET 9.0 WPF
- Uses Win32 API for always-on-top behavior
- JSON-based settings persistence
- OpenCV for video display
- WMI and Registry-based camera detection
- Latest NuGet packages (System.Management v9.0.7)

## Development Notes
- The camera implementation is need to be actual camera capture
- The application is designed to work alongside Windows Game Bar for screen recording
- Focus on simplicity and ease of use for elderly users
- Every time make changes, ensure increase version number and update the changelog, also update the README.md with new features or changes
- Always keep committing your changes with clear messages when make changes to the codebase
