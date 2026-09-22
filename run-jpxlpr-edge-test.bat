@echo off
setlocal
cd /d "%~dp0"

if not defined PARKING_RUNTIME_CONNECTION set "PARKING_RUNTIME_CONNECTION=server=localhost;port=3306;uid=test;pwd=test;database=parking000test;Charset=utf8mb4;SslMode=none;"
powershell -NoProfile -Command "if ($env:PARKING_RUNTIME_CONNECTION -notmatch '(?i)(^|;)\s*(database|initial catalog)\s*=\s*parking000test\s*(;|$)') { exit 1 }"
if errorlevel 1 (
  echo PARKING_RUNTIME_CONNECTION must use database=parking000test.
  goto :failed
)

set "ConnectionStrings__ParkingDatabase=%PARKING_RUNTIME_CONNECTION%"
set "Gateway__BaseUrl=http://localhost:5100/"
set "ParkingApi__BaseUrl=http://localhost:5000/"
set "EdgeService__BaseUrl=http://localhost:5200/"
if not defined EDGE_SITE_AUTH_KEY (
  echo EDGE_SITE_AUTH_KEY environment variable is required.
  goto :failed
)
set "ASPNETCORE_ENVIRONMENT=Development"

echo [1/8] Stop previously running parking programs
powershell -NoProfile -Command "$names=@('JPXLpr','Parking.Api','Parking.EdgeGateway','Parking.EdgeService'); Get-Process -ErrorAction SilentlyContinue | Where-Object { $names -contains $_.ProcessName } | Stop-Process -Force -ErrorAction SilentlyContinue"
pause

echo [2/8] Build ParkingSystem and JPXLPR
dotnet build ParkingSystem.sln
if errorlevel 1 goto :failed
dotnet build JPXLpr\JPXLpr.csproj
if errorlevel 1 goto :failed
dotnet build tests\JPXLpr.Tests\JPXLpr.Tests.csproj
if errorlevel 1 goto :failed
pause

echo [3/8] Run all tests
dotnet test ParkingSystem.sln --no-build
if errorlevel 1 goto :failed
dotnet test tests\JPXLpr.Tests\JPXLpr.Tests.csproj --no-build
if errorlevel 1 goto :failed
pause

echo [4/8] Start Parking.Api
set "ASPNETCORE_URLS=http://localhost:5000"
start "Parking.Api" cmd /k dotnet run --no-launch-profile --project src\Central\Parking.Api\Parking.Api.csproj
pause

echo [5/8] Start Parking.EdgeGateway
set "ASPNETCORE_URLS=http://localhost:5100"
start "Parking.EdgeGateway" cmd /k dotnet run --no-launch-profile --project src\Central\Parking.EdgeGateway\Parking.EdgeGateway.csproj
pause

echo [6/8] Start Parking.EdgeService
set "ASPNETCORE_URLS=http://localhost:5200"
start "Parking.EdgeService" cmd /k dotnet run --no-launch-profile --project src\Edge\Parking.EdgeService\Parking.EdgeService.csproj
pause

echo [7/8] Configure Site 9001 and wait for sync
powershell -NoProfile -Command "$body=@{SiteId=9001;CentralServerUrl='http://localhost:5100/';ImageServerUrl='http://localhost:5300/';ImageWatchPath='%~dp0artifacts\EdgeImages';SiteAuthKey=$env:EDGE_SITE_AUTH_KEY}|ConvertTo-Json; Invoke-RestMethod -Method Put -Uri 'http://localhost:5200/api/v1/local/setup' -ContentType 'application/json' -Body $body | Out-Null; $ok=$false; foreach($i in 1..30) { try { Invoke-RestMethod -Uri 'http://localhost:5200/api/v1/local/config' | Out-Null; $ok=$true; break } catch { Start-Sleep -Seconds 1 } }; if (-not $ok) { throw 'EdgeService configuration sync timed out.' }"
if errorlevel 1 goto :failed
pause

echo [8/8] Start JPXLPR
start "JPXLPR" cmd /k dotnet run --no-launch-profile --project JPXLpr\JPXLpr.csproj
echo Confirm ACK logs for Site 9001 Group 2 Lane 9010/9020 Device 401/402.
pause
goto :eof

:failed
echo JPXLPR EdgeService integration test failed.
pause
exit /b 1
