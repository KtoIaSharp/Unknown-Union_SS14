@echo off
title DUO BUILD - SS14 UU
cd /d "%~dp0"

echo ==========================================
echo   SS14 UU - Duo build: server + 2 clients
echo ==========================================
echo.

REM --- TTS container (if Docker is running) ---
docker ps --filter "name=ss14tts" --format "{{.Names}}" 2>nul | findstr /I "ss14tts" >nul
if %errorlevel%==0 (
    echo [TTS] container already running.
) else (
    echo [TTS] container not running - trying to start it...
    docker start ss14tts 2>nul
    if %errorlevel%==0 (echo [TTS] started.) else (echo [TTS] WARN: cannot start, check Docker Desktop.)
)

echo.
echo [SERVER] starting...
start "SS14 Server" bin\Content.Server\Content.Server.exe

REM wait for the server to bind the port
timeout /t 8 /nobreak >nul

echo [CLIENT 1] starting...
start "SS14 Client 1" bin\Content.Client\Content.Client.exe

echo [CLIENT 2] starting...
start "SS14 Client 2" bin\Content.Client\Content.Client.exe

echo.
echo Done. Server on port 1212, two clients launched.
