@echo off
title BUILD DEBUG - SS14 UU
cd /d "%~dp0"
setlocal

echo ==========================================================
echo   SS14 UU - full Debug build, then server and client
echo ==========================================================
echo.

if not exist "SpaceStation14.slnx" (
    echo [FAIL] SpaceStation14.slnx not found. Run this file from the repo root.
    pause
    exit /b 1
)

REM --- 1. Close running server/client: they lock DLLs in bin\ ---
set RUNNING=0
tasklist /FI "IMAGENAME eq Content.Server.exe" 2>nul | find /I "Content.Server.exe" >nul && set RUNNING=1
tasklist /FI "IMAGENAME eq Content.Client.exe" 2>nul | find /I "Content.Client.exe" >nul && set RUNNING=1
if "%RUNNING%"=="1" (
    echo [WARN] Server or client is running and locks DLLs in bin.
    choice /C YN /N /M "Close them and continue? [Y/N] "
    if errorlevel 2 goto :cancelled
    taskkill /IM Content.Server.exe /F >nul 2>&1
    taskkill /IM Content.Client.exe /F >nul 2>&1
    timeout /t 2 /nobreak >nul
    echo [ OK ] Processes closed.
    echo.
)

REM --- 2. Full solution build in Debug ---
echo [BUILD] dotnet build SpaceStation14.slnx -c Debug
echo.
dotnet build SpaceStation14.slnx -c Debug
if errorlevel 1 (
    echo.
    echo [FAIL] Build failed. Fix the errors above and run again.
    pause
    exit /b 1
)
echo.
echo [ OK ] Build finished.
echo.

REM --- 3. TTS container (optional, skipped when Docker is unavailable) ---
docker start ss14tts >nul 2>&1
if errorlevel 1 (
    echo [TTS] container ss14tts is not available - skipping.
) else (
    echo [TTS] container ss14tts is running.
)
echo.

REM --- 4. Server, wait for the port, then client ---
echo [SERVER] starting bin\Content.Server\Content.Server.exe
start "SS14 Server" "bin\Content.Server\Content.Server.exe"

echo [WAIT] waiting for the server to bind port 1212...
timeout /t 8 /nobreak >nul

echo [CLIENT] starting bin\Content.Client\Content.Client.exe
start "SS14 Client" "bin\Content.Client\Content.Client.exe"

echo.
echo ==========================================================
echo   Done: server and client are starting.
echo   Second local client: duo.bat
echo ==========================================================
timeout /t 5 /nobreak >nul
exit /b 0

:cancelled
echo.
echo [STOP] Cancelled by user.
pause
exit /b 1
