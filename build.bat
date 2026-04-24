@echo off
echo ========================================================
echo        Timezone Tray Clock - Build Script
echo ========================================================
echo.

:: Check for local .dotnet installation
set DOTNET_CMD=dotnet
if exist ".dotnet\dotnet.exe" (
    echo [INFO] Local .NET SDK detected. Using local environment.
    set DOTNET_CMD=".\.dotnet\dotnet.exe"
) else (
    echo [INFO] Using global system .NET SDK.
)

echo.
echo Cleaning old build files...
if exist "bin\Release" rmdir /s /q "bin\Release"

echo.
echo Packaging as a single-file executable (all DLLs embedded)...
%DOTNET_CMD% publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:DebugType=none

if %errorlevel% neq 0 (
    echo.
    echo [ERROR] Build failed! Please check the output above.
    pause
    exit /b %errorlevel%
)

echo.
echo ========================================================
echo [SUCCESS] Build Complete!
echo Your standalone executable is located at:
echo bin\Release\net8.0-windows\win-x64\publish\timezone-tray-clock.exe
echo ========================================================
echo.

pause
