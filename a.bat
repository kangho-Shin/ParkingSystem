dotnet new classlib -n Parking.Domain -f net9.0 -o src\BuildingBlocks\Parking.Domain
dotnet new classlib -n Parking.Contracts -f net9.0 -o src\BuildingBlocks\Parking.Contracts
dotnet new xunit -n Parking.Domain.Tests -f net9.0 -o tests\Parking.Domain.Tests

dotnet sln add src\BuildingBlocks\Parking.Domain\Parking.Domain.csproj
dotnet sln add src\BuildingBlocks\Parking.Contracts\Parking.Contracts.csproj
dotnet sln add tests\Parking.Domain.Tests\Parking.Domain.Tests.csproj

dotnet add tests\Parking.Domain.Tests\Parking.Domain.Tests.csproj reference src\BuildingBlocks\Parking.Domain\Parking.Domain.csproj
dotnet add tests\Parking.Domain.Tests\Parking.Domain.Tests.csproj reference src\BuildingBlocks\Parking.Contracts\Parking.Contracts.csproj