@echo off
setlocal
chcp 65001 >nul

if "%PARKING_RUNTIME_CONNECTION%"=="" (
  echo PARKING_RUNTIME_CONNECTION environment variable is required.
  pause
  exit /b 1
)

if "%LPR_TEST_HOST%"=="" set "LPR_TEST_HOST=localhost"
if "%LPR_TEST_PORT%"=="" set "LPR_TEST_PORT=29200"
if "%LPR_TEST_SITE%"=="" set "LPR_TEST_SITE=9001"
if "%LPR_TEST_GROUP%"=="" set "LPR_TEST_GROUP=2"
if "%LPR_TEST_LANE%"=="" set "LPR_TEST_LANE=9010"
if "%LPR_TEST_DEVICES%"=="" set "LPR_TEST_DEVICES=411,412,413,414"
if "%LPR_TEST_COUNT%"=="" set "LPR_TEST_COUNT=20"
if "%LPR_TEST_INTERVAL_MS%"=="" set "LPR_TEST_INTERVAL_MS=50"
if "%LPR_TEST_TIMEOUT_SECONDS%"=="" set "LPR_TEST_TIMEOUT_SECONDS=10"

set "ConnectionStrings__ParkingDatabase=%PARKING_RUNTIME_CONNECTION%"
set "Gateway__BaseUrl=http://localhost:5100/"
set "EdgeService__BaseUrl=http://localhost:5200/"
set "ParkingApi__BaseUrl=http://localhost:5000/"

echo [1/6] Build complete solution
dotnet build ParkingSystem.sln
if errorlevel 1 goto :build_failed
pause

echo [2/6] Run complete automated tests
dotnet test ParkingSystem.sln --no-build
if errorlevel 1 goto :test_failed
pause

echo [3/6] Start Parking.Api
set "ASPNETCORE_URLS=http://localhost:5000"
start "Parking.Api" cmd /k dotnet run --no-build --no-launch-profile --project src\Central\Parking.Api\Parking.Api.csproj
pause

echo [4/6] Start Parking.EdgeGateway
set "ASPNETCORE_URLS=http://localhost:5100"
start "Parking.EdgeGateway" cmd /k dotnet run --no-build --no-launch-profile --project src\Central\Parking.EdgeGateway\Parking.EdgeGateway.csproj
pause

echo [5/6] Start Parking.EdgeService
set "ASPNETCORE_URLS=http://localhost:5200"
start "Parking.EdgeService" cmd /k dotnet run --no-build --no-launch-profile --project src\Edge\Parking.EdgeService\Parking.EdgeService.csproj
pause

echo Validate LPR camera devices and wait for EdgeService synchronization
powershell -NoProfile -Command "$required=@(%LPR_TEST_DEVICES%); $site=[long]%LPR_TEST_SITE%; $lane=[long]%LPR_TEST_LANE%; $config=Invoke-RestMethod ('http://localhost:5000/api/v1/config/sites/' + $site); foreach($number in $required){ $deviceId=3600+[long]$number; $found=@($config.Devices | Where-Object { $_.DeviceNumber -eq $number }); if($found.Count -ne 1 -or $found[0].DeviceId -ne $deviceId -or $found[0].DeviceType -ne 'LPR' -or $found[0].LaneId -ne $lane -or -not $found[0].Enabled){ throw ('Enabled LPR device ' + $deviceId + '/' + $number + ' is not configured on lane ' + $lane + '.') } }; $deadline=(Get-Date).AddSeconds(30); do { Start-Sleep -Seconds 1; $local=Invoke-RestMethod 'http://localhost:5200/api/v1/local/config'; $configured=@($local.Devices | Where-Object { $_.Enabled -and $_.DeviceType -eq 'LPR' -and $_.LaneId -eq $lane } | ForEach-Object { $_.DeviceNumber }); $missing=@($required | Where-Object { $_ -notin $configured }) } while($missing.Count -gt 0 -and (Get-Date) -lt $deadline); if($missing.Count -gt 0){ throw ('Configuration synchronization timed out. Missing: ' + ($missing -join ',')) }"
if errorlevel 1 (
  echo LPR camera validation or configuration synchronization failed.
  pause
  exit /b 2
)

echo [6/6] Run four-camera continuous LPR test
echo Devices %LPR_TEST_DEVICES%, %LPR_TEST_COUNT% events per camera
dotnet run --no-build --project src\Tools\Parking.LprStressTester\Parking.LprStressTester.csproj -- ^
  --host %LPR_TEST_HOST% --port %LPR_TEST_PORT% ^
  --site %LPR_TEST_SITE% --group %LPR_TEST_GROUP% --lane %LPR_TEST_LANE% ^
  --devices %LPR_TEST_DEVICES% --count %LPR_TEST_COUNT% ^
  --interval-ms %LPR_TEST_INTERVAL_MS% --timeout-seconds %LPR_TEST_TIMEOUT_SECONDS%
set "LPR_TEST_EXIT=%ERRORLEVEL%"
pause

where mysql >nul 2>nul
if errorlevel 1 (
  echo mysql.exe was not found. Database verification skipped.
) else (
  echo Enter the MySQL root password to verify recent test entries.
  mysql -u root -p -e "USE parking000test; SELECT indeviceid,COUNT(*) eventcount FROM parking_session WHERE sitenum=%LPR_TEST_SITE% AND groupnum=%LPR_TEST_GROUP% AND carnum LIKE '시험%%' AND indate >= UTC_TIMESTAMP(6)-INTERVAL 10 MINUTE GROUP BY indeviceid ORDER BY indeviceid;"
)
pause

if not "%LPR_TEST_EXIT%"=="0" (
  echo LPR four-camera test failed. ExitCode=%LPR_TEST_EXIT%
  exit /b %LPR_TEST_EXIT%
)

echo LPR four-camera test completed successfully.
exit /b 0

:build_failed
echo Solution build failed.
pause
exit /b 1

:test_failed
echo Automated tests failed.
pause
exit /b 1
