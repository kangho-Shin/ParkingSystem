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
set "TERMINAL_TEST_ROOT=%CD%\artifacts\TerminalAgentTest"
set "KIOSK_PUBLISH=%TERMINAL_TEST_ROOT%\KioskSimulator"
set "AGENT_PUBLISH=%TERMINAL_TEST_ROOT%\TerminalAgent"

echo [1/11] Build entire solution
dotnet build ParkingSystem.sln
if errorlevel 1 goto :failed
pause

echo [2/11] Test entire solution
dotnet test ParkingSystem.sln --no-build
if errorlevel 1 goto :failed
pause

echo [3/11] Publish background programs
dotnet publish src\Tools\Parking.KioskSimulator\Parking.KioskSimulator.csproj -c Debug -o "%KIOSK_PUBLISH%"
if errorlevel 1 goto :failed
dotnet publish src\Edge\Parking.TerminalAgent\Parking.TerminalAgent.csproj -c Debug -o "%AGENT_PUBLISH%"
if errorlevel 1 goto :failed
pause

echo [4/11] Start Parking.Api
set "ASPNETCORE_URLS=http://localhost:5000"
start "Parking.Api" cmd /k dotnet run --no-launch-profile --project src\Central\Parking.Api\Parking.Api.csproj
pause

echo [5/11] Start Parking.EdgeGateway
set "ASPNETCORE_URLS=http://localhost:5100"
start "Parking.EdgeGateway" cmd /k dotnet run --no-launch-profile --project src\Central\Parking.EdgeGateway\Parking.EdgeGateway.csproj
pause

echo [6/11] Start Parking.EdgeService
set "ASPNETCORE_URLS=http://localhost:5200"
start "Parking.EdgeService" cmd /k dotnet run --no-launch-profile --project src\Edge\Parking.EdgeService\Parking.EdgeService.csproj
pause

echo [7/11] Start TerminalAgent and automatically start KioskSimulator
set "TerminalAgent__MonitorIntervalSeconds=2"
set "TerminalAgent__MaxRestarts=3"
set "TerminalAgent__RestartWindowMinutes=5"
set "TerminalAgent__StableRunMinutes=5"
set "TerminalAgent__LogDirectory=%TERMINAL_TEST_ROOT%\logs"
set "TerminalAgent__Programs__0__Name=KioskSimulator"
set "TerminalAgent__Programs__0__ProcessName=Parking.KioskSimulator"
set "TerminalAgent__Programs__0__ExecutablePath=%KIOSK_PUBLISH%\Parking.KioskSimulator.exe"
set "TerminalAgent__Programs__0__Arguments=--site 9001 --group 2 --device-number 201 --url http://localhost:5200 --complete"
set "TerminalAgent__Programs__0__WorkingDirectory=%KIOSK_PUBLISH%"
set "TerminalAgent__Programs__0__Enabled=true"
set "TerminalAgent__Programs__0__ProgramType=Background"
start "Parking.TerminalAgent" cmd /k ""%AGENT_PUBLISH%\Parking.TerminalAgent.exe""
pause

echo [8/11] Confirm automatic start, then force abnormal exit 1
powershell -NoProfile -Command "$limit=(Get-Date).AddSeconds(15); do { if(Get-Process -Name 'Parking.KioskSimulator' -ErrorAction SilentlyContinue){exit 0}; Start-Sleep -Milliseconds 500 } while((Get-Date)-lt $limit); exit 1"
if errorlevel 1 goto :failed
taskkill /F /IM Parking.KioskSimulator.exe
if errorlevel 1 goto :failed
pause

echo [9/11] Confirm restart, then force abnormal exit 2
powershell -NoProfile -Command "$limit=(Get-Date).AddSeconds(15); do { if(Get-Process -Name 'Parking.KioskSimulator' -ErrorAction SilentlyContinue){exit 0}; Start-Sleep -Milliseconds 500 } while((Get-Date)-lt $limit); exit 1"
if errorlevel 1 goto :failed
taskkill /F /IM Parking.KioskSimulator.exe
if errorlevel 1 goto :failed
pause

echo [10/11] Confirm restart, then force abnormal exit 3
powershell -NoProfile -Command "$limit=(Get-Date).AddSeconds(15); do { if(Get-Process -Name 'Parking.KioskSimulator' -ErrorAction SilentlyContinue){exit 0}; Start-Sleep -Milliseconds 500 } while((Get-Date)-lt $limit); exit 1"
if errorlevel 1 goto :failed
taskkill /F /IM Parking.KioskSimulator.exe
if errorlevel 1 goto :failed
pause

echo [11/11] Confirm restart limit and inspect logs
powershell -NoProfile -Command "1..20 | ForEach-Object { if(Get-Process -Name 'Parking.KioskSimulator' -ErrorAction SilentlyContinue){exit 1}; Start-Sleep -Milliseconds 500 }; exit 0"
if errorlevel 1 goto :failed
echo Log root: %TERMINAL_TEST_ROOT%\logs
explorer "%TERMINAL_TEST_ROOT%\logs"
pause
goto :eof

:failed
echo TerminalAgent test failed.
pause
exit /b 1
