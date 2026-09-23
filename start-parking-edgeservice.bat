@echo off
setlocal
cd /d "%~dp0"

set "Gateway__BaseUrl=http://localhost:5100/"
set "ASPNETCORE_ENVIRONMENT=Development"
set "ASPNETCORE_URLS=http://localhost:5200"
title Parking.EdgeService - http://localhost:5200

dotnet run --no-build --no-launch-profile --project src\Edge\Parking.EdgeService\Parking.EdgeService.csproj
if errorlevel 1 goto :failed
exit /b 0

:failed
echo Parking.EdgeService failed to start.
pause
exit /b 1
