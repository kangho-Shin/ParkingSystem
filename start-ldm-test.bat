@echo off
setlocal
cd /d "%~dp0"

if not defined PARKING_RUNTIME_CONNECTION (
    echo PARKING_RUNTIME_CONNECTION is not set.
    echo Example: set PARKING_RUNTIME_CONNECTION=Server=localhost;Database=parking000test;User ID=test;Password=test
    pause
    exit /b 1
)

set "ConnectionStrings__ParkingDatabase=%PARKING_RUNTIME_CONNECTION%"
set "Edge__SiteId=9001"
set "Edge__LprListenPort=29200"

echo [1/4] Starting Parking.Api on http://localhost:5000
start "Parking.Api" cmd /k "dotnet run --no-launch-profile --project src\Central\Parking.Api\Parking.Api.csproj"
REM pause

echo [2/4] Starting Parking.EdgeGateway on http://localhost:5100
start "Parking.EdgeGateway" cmd /k "dotnet run --no-launch-profile --project src\Central\Parking.EdgeGateway\Parking.EdgeGateway.csproj"
REM pause

echo [3/4] Starting Parking.EdgeService on http://localhost:5200
start "Parking.EdgeService" cmd /k "dotnet run --no-launch-profile --project src\Edge\Parking.EdgeService\Parking.EdgeService.csproj"
REM pause

echo [4/4] Starting Parking.KioskSimulator with Site 9001 Group 2 DeviceNumber 201
start "Parking.KioskSimulator" cmd /k "dotnet run --project src\Tools\Parking.KioskSimulator\Parking.KioskSimulator.csproj -- --site 9001 --group 2 --device-number 201 --url http://localhost:5200 --complete"
REM pause

echo All LDM integration-test programs have been started.
endlocal
