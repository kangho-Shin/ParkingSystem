@echo off
setlocal
cd /d "%~dp0"

if not defined PARKING_RUNTIME_CONNECTION (
    echo PARKING_RUNTIME_CONNECTION is not set.
    echo Example: set PARKING_RUNTIME_CONNECTION=Server=localhost;Database=parking000test;User ID=root;Password=your_password
    pause
    exit /b 1
)

if not defined PARKING_IMAGE_ROOT set "PARKING_IMAGE_ROOT=E:\ParkingImage"
if not defined PARKING_EDGE_DATA set "PARKING_EDGE_DATA=E:\ParkingEdgeData"
set "ConnectionStrings__ParkingDatabase=%PARKING_RUNTIME_CONNECTION%"
set "ImageServer__RootPath=%PARKING_IMAGE_ROOT%"
set "Edge__DataDirectory=%PARKING_EDGE_DATA%"
set "Edge__LprListenPort=29200"

echo Before the first run, apply database\mysql\011_edge_configuration_sync.sql.
echo Register the site key in parking_site_sync with SHA2('site-key',256).
pause

echo [1/6] Starting ParkImageServer on http://localhost:5400
start "ParkImageServer" cmd /k "set ASPNETCORE_URLS=http://localhost:5400&& dotnet run --no-launch-profile --project ImageServer\ParkImageServer\ParkImageServer.csproj"
pause

echo [2/6] Starting Parking.Api on http://localhost:5000
start "Parking.Api" cmd /k "set ASPNETCORE_URLS=http://localhost:5000&& dotnet run --no-launch-profile --project src\Central\Parking.Api\Parking.Api.csproj"
pause

echo [3/6] Starting Parking.EdgeGateway on http://localhost:5100
start "Parking.EdgeGateway" cmd /k "set ASPNETCORE_URLS=http://localhost:5100&& set ParkingApi__BaseUrl=http://localhost:5000/&& dotnet run --no-launch-profile --project src\Central\Parking.EdgeGateway\Parking.EdgeGateway.csproj"
pause

echo [4/6] Starting Parking.EdgeService on http://localhost:5200
start "Parking.EdgeService" cmd /k "set ASPNETCORE_URLS=http://localhost:5200&& dotnet run --no-launch-profile --project src\Edge\Parking.EdgeService\Parking.EdgeService.csproj"
pause

echo [5/6] Starting ImageUploadAgent
start "ImageUploadAgent" cmd /k "dotnet run --project ImageServer\ImageUploadAgent\ImageUploadAgent.csproj"
pause

echo [6/6] Starting Parking.EdgeManager
start "Parking.EdgeManager" cmd /k "dotnet run --project src\Edge\Parking.EdgeManager\Parking.EdgeManager.csproj"
pause

echo All local-configuration and image-test programs have been started.
endlocal
