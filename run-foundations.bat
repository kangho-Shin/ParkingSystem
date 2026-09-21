@echo off
setlocal

if "%PARKING_RUNTIME_CONNECTION%"=="" (
  echo PARKING_RUNTIME_CONNECTION environment variable is required.
  pause
  exit /b 1
)

set "ConnectionStrings__ParkingDatabase=%PARKING_RUNTIME_CONNECTION%"
set "Gateway__BaseUrl=http://localhost:5100/"
set "EdgeService__BaseUrl=http://localhost:5200/"
set "ParkingApi__BaseUrl=http://localhost:5000/"

echo [1/8] Parking.Api
set "ASPNETCORE_URLS=http://localhost:5000"
start "Parking.Api" cmd /k dotnet run --no-launch-profile --project src\Central\Parking.Api\Parking.Api.csproj
pause

echo [2/8] Parking.EdgeGateway
set "ASPNETCORE_URLS=http://localhost:5100"
start "Parking.EdgeGateway" cmd /k dotnet run --no-launch-profile --project src\Central\Parking.EdgeGateway\Parking.EdgeGateway.csproj
pause

echo [3/8] Parking.EdgeService
set "ASPNETCORE_URLS=http://localhost:5200"
start "Parking.EdgeService" cmd /k dotnet run --no-launch-profile --project src\Edge\Parking.EdgeService\Parking.EdgeService.csproj
pause

echo [4/8] Parking.KioskSimulator SignalR
start "Parking.KioskSimulator" cmd /k dotnet run --project src\Tools\Parking.KioskSimulator\Parking.KioskSimulator.csproj -- --site 9001 --group 2 --device-number 201 --url http://localhost:5200 --complete
pause

echo [5/8] Parking.Operator
start "Parking.Operator" cmd /k dotnet run --project src\Edge\Parking.Operator\Parking.Operator.csproj
pause

echo [6/8] Parking.TerminalAgent
start "Parking.TerminalAgent" cmd /k dotnet run --project src\Edge\Parking.TerminalAgent\Parking.TerminalAgent.csproj
pause

echo [7/8] Parking.Worker
start "Parking.Worker" cmd /k dotnet run --project src\Central\Parking.Worker\Parking.Worker.csproj
pause

echo [8/8] Parking.FeeTester
start "Parking.FeeTester" cmd /k dotnet run --project src\Tools\Parking.FeeTester\Parking.FeeTester.csproj
pause

endlocal
