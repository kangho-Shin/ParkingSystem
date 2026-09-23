@echo off
setlocal
cd /d "%~dp0"

if not defined PARKING_RUNTIME_CONNECTION (
  echo PARKING_RUNTIME_CONNECTION environment variable is required.
  goto :failed
)

start "Parking.Api" cmd /c call "%~dp0start-parking-api.bat"
call :wait_for_server "http://localhost:5000/" "Parking.Api"
if errorlevel 1 goto :failed

start "Parking.EdgeGateway" cmd /c call "%~dp0start-parking-edgegateway.bat"
call :wait_for_server "http://localhost:5100/" "Parking.EdgeGateway"
if errorlevel 1 goto :failed

start "Parking.EdgeService" cmd /c call "%~dp0start-parking-edgeservice.bat"
call :wait_for_server "http://localhost:5200/" "Parking.EdgeService"
if errorlevel 1 goto :failed

echo Parking.Api, Parking.EdgeGateway and Parking.EdgeService are running.
exit /b 0

:wait_for_server
echo Waiting for %~2 ...
powershell -NoProfile -Command "$ok=$false; foreach($i in 1..30) { try { Invoke-WebRequest -UseBasicParsing -Uri '%~1' -TimeoutSec 2 | Out-Null; $ok=$true; break } catch { Start-Sleep -Seconds 1 } }; if (-not $ok) { exit 1 }"
if errorlevel 1 (
  echo %~2 did not respond at %~1
  exit /b 1
)
exit /b 0

:failed
echo Parking server startup failed.
pause
exit /b 1
