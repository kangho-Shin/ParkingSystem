@echo off
setlocal
cd /d "%~dp0"

if "%PARKING_RUNTIME_CONNECTION%"=="" set "PARKING_RUNTIME_CONNECTION=server=localhost;port=3306;uid=test;pwd=test;database=parking000test;Charset=utf8mb4;SslMode=none;"
echo %PARKING_RUNTIME_CONNECTION% | findstr /i "database=parking000test" >nul
if errorlevel 1 (
  echo PARKING_RUNTIME_CONNECTION must use database=parking000test.
  goto :failed
)
set "ConnectionStrings__ParkingDatabase=%PARKING_RUNTIME_CONNECTION%"
set "Gateway__BaseUrl=http://localhost:5100/"
set "ParkingApi__BaseUrl=http://localhost:5000/"
set "EdgeService__BaseUrl=http://localhost:5200/"
set "EDGE_SITE_AUTH_KEY=site-9001-key"
if "%EDGE_IMAGE_SERVER_URL%"=="" set "EDGE_IMAGE_SERVER_URL=http://localhost:5300/"
if "%EDGE_IMAGE_WATCH_PATH%"=="" set "EDGE_IMAGE_WATCH_PATH=%~dp0artifacts\EdgeImages"
if not exist "%EDGE_IMAGE_WATCH_PATH%" mkdir "%EDGE_IMAGE_WATCH_PATH%"
set "ASPNETCORE_ENVIRONMENT=Development"

echo [1/8] Stop previously running Parking programs
powershell -NoProfile -Command "$names=@('APSMain','Parking.Api','Parking.EdgeGateway','Parking.EdgeService','Parking.EdgeManager','Parking.Operator','Parking.KioskSimulator','Parking.TerminalAgent'); Get-Process -ErrorAction SilentlyContinue | Where-Object { $names -contains $_.ProcessName } | Stop-Process -Force -ErrorAction SilentlyContinue"
pause

echo [2/8] Restore all projects
dotnet restore ParkingSystem.sln
if errorlevel 1 goto :failed
dotnet restore "APSMain_C(API)V2\APSMain.csproj"
if errorlevel 1 goto :failed
dotnet restore tests\APSMain.EdgeIntegration.Tests\APSMain.EdgeIntegration.Tests.csproj
if errorlevel 1 goto :failed
pause

echo [3/8] Build full solution and APSMain
dotnet build ParkingSystem.sln --no-restore
if errorlevel 1 goto :failed
dotnet build "APSMain_C(API)V2\APSMain.csproj" --no-restore
if errorlevel 1 goto :failed
pause

echo [4/8] Run all tests, then APSMain integration tests
dotnet test ParkingSystem.sln --no-build --filter "Category!=DatabaseMutation"
if errorlevel 1 goto :failed
dotnet test tests\APSMain.EdgeIntegration.Tests\APSMain.EdgeIntegration.Tests.csproj --no-restore
if errorlevel 1 goto :failed
pause

echo [5/8] Start Parking.Api on http://localhost:5000
set "ASPNETCORE_URLS=http://localhost:5000"
start "Parking.Api" cmd /k dotnet run --no-launch-profile --project src\Central\Parking.Api\Parking.Api.csproj
pause

echo [6/8] Start Parking.EdgeGateway on http://localhost:5100
set "ASPNETCORE_URLS=http://localhost:5100"
start "Parking.EdgeGateway" cmd /k dotnet run --no-launch-profile --project src\Central\Parking.EdgeGateway\Parking.EdgeGateway.csproj
pause

echo [7/8] Start Parking.EdgeService on http://localhost:5200
set "ASPNETCORE_URLS=http://localhost:5200"
start "Parking.EdgeService" cmd /k dotnet run --no-launch-profile --project src\Edge\Parking.EdgeService\Parking.EdgeService.csproj
pause

echo Configure EdgeService for Site 9001 and wait for configuration sync
powershell -NoProfile -Command "$body=@{SiteId=9001;CentralServerUrl='http://localhost:5100/';ImageServerUrl=$env:EDGE_IMAGE_SERVER_URL;ImageWatchPath=$env:EDGE_IMAGE_WATCH_PATH;SiteAuthKey=$env:EDGE_SITE_AUTH_KEY}|ConvertTo-Json; Invoke-RestMethod -Method Put -Uri 'http://localhost:5200/api/v1/local/setup' -ContentType 'application/json' -Body $body | Out-Null; $ok=$false; foreach($i in 1..30) { try { Invoke-RestMethod -Uri 'http://localhost:5200/api/v1/local/config' | Out-Null; $ok=$true; break } catch { Start-Sleep -Seconds 1 } }; if (-not $ok) { throw 'EdgeService configuration sync timed out.' }"
if errorlevel 1 goto :failed
pause

echo [8/8] Start APSMain with SITENUM, GROUPNUM and APSNUM from App.config.
start "APSMain" cmd /k dotnet run --no-launch-profile --project "APSMain_C(API)V2\APSMain.csproj"
echo Use the existing LPR simulator to send an exit event, then verify APSMain LOG\year\month EDGE logs and the payment screen.
pause
goto :eof

:failed
echo APSMain EdgeService integration test failed.
pause
exit /b 1
