@echo off
title TTS START - SS14 UU
echo ==========================================
echo   SS14 UU - TTS server start
echo ==========================================
echo.

echo [1/3] Checking Docker...
docker info >nul 2>&1
if %errorlevel%==0 goto docker_ok

echo Docker is not running - starting Docker Desktop...
start "" "C:\Users\user\AppData\Local\Programs\DockerDesktop\Docker Desktop.exe"

echo Waiting for Docker engine to start...
set /a tries=0
:wait_docker
set /a tries+=1
docker info >nul 2>&1
if %errorlevel%==0 goto docker_ok
if %tries% GEQ 60 (
    echo ERROR: Docker did not start in time. Open Docker Desktop manually and run again.
    pause
    exit /b 1
)
timeout /t 2 /nobreak >nul
goto wait_docker

:docker_ok
echo [2/3] Docker engine is up. Starting TTS container...
docker start ss14tts
if %errorlevel%==0 (
    echo [3/3] TTS container started.
) else (
    echo WARN: container did not start. Check: docker ps -a
)

echo.
echo Waiting for TTS to boot...
timeout /t 15 /nobreak >nul

echo Checking health...
powershell -NoProfile -Command "try { $r = Invoke-WebRequest -Uri 'http://127.0.0.1:5000/health' -UseBasicParsing -TimeoutSec 5; Write-Output ('TTS health: ' + $r.StatusCode) } catch { Write-Output 'TTS health: NOT READY yet - wait a bit and retry' }"
echo.
pause
