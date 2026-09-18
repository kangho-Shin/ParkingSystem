dotnet new classlib -n Parking.Central.Data -f net9.0 -o src\Central\Parking.Central.Data
dotnet new xunit -n Parking.Api.Tests -f net9.0 -o tests\Parking.Api.Tests

dotnet sln add src\Central\Parking.Central.Data\Parking.Central.Data.csproj
dotnet sln add tests\Parking.Api.Tests\Parking.Api.Tests.csproj

dotnet add src\Central\Parking.Central.Data\Parking.Central.Data.csproj reference src\BuildingBlocks\Parking.Contracts\Parking.Contracts.csproj
dotnet add tests\Parking.Api.Tests\Parking.Api.Tests.csproj reference src\Central\Parking.Central.Data\Parking.Central.Data.csproj

dotnet add src\Central\Parking.Central.Data\Parking.Central.Data.csproj package Dapper
dotnet add src\Central\Parking.Central.Data\Parking.Central.Data.csproj package MySqlConnector