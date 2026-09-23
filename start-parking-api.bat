@echo off
setlocal
cd /d "%~dp0"

if not defined PARKING_RUNTIME_CONNECTION (
  echo PARKING_RUNTIME_CONNECTION environment variable is required.
  goto :failed
)

powershell -NoProfile -Command "if ($env:PARKING_RUNTIME_CONNECTION -notmatch '(?i)(^|;)\s*(database|initial catalog)\s*=\s*parking000test\s*(;|$)') { exit 1 }"
if errorlevel 1 (
  echo PARKING_RUNTIME_CONNECTION must use database=parking000test.
  goto :failed
)

set "ConnectionStrings__ParkingDatabase=%PARKING_RUNTIME_CONNECTION%"
set "ASPNETCORE_ENVIRONMENT=Development"
set "ASPNETCORE_URLS=http://localhost:5000"
title Parking.Api - http://localhost:5000

dotnet run --no-build --no-launch-profile --project src\Central\Parking.Api\Parking.Api.csproj
if errorlevel 1 goto :failed
exit /b 0

:failed
echo Parking.Api failed to start.
pause
exit /b 1
