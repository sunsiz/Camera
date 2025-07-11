@echo off
echo Building Camera Overlay...
dotnet build
if %errorlevel% neq 0 (
    echo Build failed!
    pause
    exit /b %errorlevel%
)
echo Build successful! Starting application...
dotnet run
pause
