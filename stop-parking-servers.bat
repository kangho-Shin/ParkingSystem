@echo off
setlocal

powershell -NoProfile -Command "$names=@('Parking.Api','Parking.EdgeGateway','Parking.EdgeService'); $processes=Get-Process -ErrorAction SilentlyContinue | Where-Object { $names -contains $_.ProcessName }; if ($processes) { $processes | Stop-Process -Force; Write-Host 'Parking servers stopped.' } else { Write-Host 'Parking servers are not running.' }"

endlocal
