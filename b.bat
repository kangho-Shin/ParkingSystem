mysql -u root -p -e "CREATE DATABASE IF NOT EXISTS parking000test CHARACTER SET utf8mb4;"

mysql -u root -p -e "use uparkdb;desc tperiodinout;"

git add .
git commit -m "Image Server& Image Uploader"
git push

git push origin codex/server-edge-foundation

set "PARKING_RUNTIME_CONNECTION=Server=localhost;Database=parking000test;User ID=root;Password=실제비밀번호;CharSet=utf8mb4;"
dotnet test tests\Parking.Api.Tests\Parking.Api.Tests.csproj --filter "FullyQualifiedName~ParkingExitRepositoryTests"

현장인증키 : site-9001-key

git add . && git commit -m "chore: add existing fee engine source" && git push

git fetch origin && git switch codex/server-edge-foundation && git pull && dotnet build ParkingSystem.sln

dotnet user-secrets init --project src\Central\Parking.Api && dotnet user-secrets set "ConnectionStrings:ParkingDatabase" "%PARKING_RUNTIME_CONNECTION%" --project src\Central\Parking.Api

mysql -u root -p -e "use parking000test; SELECT * FROM parking_site_sync p;"
mysql -u root -p parking000test < database\mysql\010_device_links.sql

git pull origin codex/server-edge-foundation
dotnet build ParkingSystem.sln
dotnet build ImageServer\ParkImageServer\ParkImageServer.csproj
dotnet test ParkingSystem.sln

GitHub 저장소 https://github.com/kangho-Shin/ParkingSystem의 codex/server-edge-foundation 브랜치 작업을 계속 진행해줘. 먼저 docs/04-development-status.md와 docs/superpowers/specs/2026-09-20-remaining-program-foundations-design.md를 읽어줘. 완료된 작업은 반복하지 말고 다음 우선순위인 Parking.Operator, Parking.TerminalAgent, Parking.Worker, Parking.FeeTester 실행 기반을 순서대로 진행해줘. 여러 프로그램 실행시험은 환경변수와 연결 문자열을 포함한 배치 파일 하나로 만들고 각 단계 사이에 pause를 넣어줘. 테스트는 기능 뼈대를 모두 만든 뒤 순서대로 몰아서 진행해줘.

입차
dotnet run --project src\Tools\Parking.Simulator\Parking.Simulator.csproj -- entry --site 9001 --group 2 --lane 9010 --device 9101 --car 34사5679
출차
powershell -NoProfile -Command "$c=New-Object Net.Sockets.TcpClient('127.0.0.1',29200);$s=$c.GetStream();$n='9001_002_201_9020_Exit_'+(Get-Date -Format 'yyyyMMddHHmmssfff')+'_34사5679_'+[Guid]::NewGuid().ToString('N')+'.jpg';$b=[Text.Encoding]::GetEncoding(949).GetBytes($n);$s.WriteByte(2);$s.Write($b,0,$b.Length);$s.WriteByte(3);$s.Flush();$r=New-Object byte[] 1024;$l=$s.Read($r,0,$r.Length);Write-Host ([Text.Encoding]::GetEncoding(949).GetString($r,0,$l));$c.Close()"


git am E:\Image\0001-fix-initialize-legacy-edge-bootstrap-database.patch

set "ConnectionStrings__ParkingDatabase=%PARKING_RUNTIME_CONNECTION%" && dotnet run --no-launch-profile --project src\Central\Parking.Api\Parking.Api.csproj

git pull
dotnet test tests\Parking.EdgeManager.Tests\Parking.EdgeManager.Tests.csproj

mysql -u root -p -e "use parking000test; SELECT xindex,carnum,groupnum,outflag,indate FROM parking_session WHERE carnum='12가3456' ORDER BY xindex DESC LIMIT 1;"
