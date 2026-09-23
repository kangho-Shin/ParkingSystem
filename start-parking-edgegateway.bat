@echo off
setlocal
cd /d "%~dp0"

set "ParkingApi__BaseUrl=http://localhost:5000/"
set "ASPNETCORE_ENVIRONMENT=Development"
set "ASPNETCORE_URLS=http://localhost:5100"
title Parking.EdgeGateway - http://localhost:5100

dotnet run --no-build --no-launch-profile --project src\Central\Parking.EdgeGateway\Parking.EdgeGateway.csproj
if errorlevel 1 goto :failed
exit /b 0

:failed
echo Parking.EdgeGateway failed to start.
pause
exit /b 1
