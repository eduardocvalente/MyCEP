using FluentAssertions;
using Microsoft.Extensions.Configuration;
using MyCEP.API.Core.Features.UseCase.IZipCodeLookup;
using MyCEP.API.Core.Shared.Domain.Repository;
using MyCEP.API.Core.Shared.Domain.Service.ViaCep;
using MyCEP.API.Core.Shared.Infrastructure.CrossCutting.UnitofWork;
using MyCEP.API.Core.Shared.Infrastructure.Database.Postgres;
using MyCEP.API.Core.Shared.Infrastructure.IoC;
using MyCEP.API.IntegrationTest.Base;

namespace MyCEP.API.IntegrationTest.Shared.Infrastructure.IoC;

public class BootStrapperIntegrationTests : IntegrationTestBase
{
    [Fact(DisplayName = "Deve registrar todos os serviços essenciais no container")]
    public void ShouldRegisterAllEssentialServices()
    {
        // Arrange & Act - já inicializado pelo IntegrationTestBase

        // Assert
        GetService<IZipCodeLookupUseCase>().Should().NotBeNull();
        GetService<IViaCepService>().Should().NotBeNull();
        GetService<IAddressRepository>().Should().NotBeNull();
        GetService<IUnitOfWork>().Should().NotBeNull();
        GetService<MyCepDbContext>().Should().NotBeNull();
    }

    [Fact(DisplayName = "Deve lançar exceção se configuração do ViaCep for inválida")]
    public void ShouldThrowException_WhenViaCepConfigIsInvalid()
    {
        // Arrange
        var configBuilder = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Postgres:PostgresHost"] = "localhost",
                ["Postgres:PostgresPort"] = "5432",
                ["Postgres:PostgresDatabase"] = "testdb",
                ["Postgres:PostgresUsername"] = "postgres",
                ["Postgres:PostgresPassword"] = "postgres"
            });
        var configuration = configBuilder.Build();
        var services = new ServiceCollection();

        // Act
        Action act = () => services.RegisterServicesToLambda(configuration);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*ViaCEP*inválidas*");
    }

    [Fact(DisplayName = "Deve lançar exceção se configuração do Postgres for inválida")]
    public void ShouldThrowException_WhenPostgresConfigIsInvalid()
    {
        // Arrange
        var configBuilder = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ViaCep:BaseUrlApi"] = "https://viacep.com.br/"
            });
        var configuration = configBuilder.Build();
        var services = new ServiceCollection();

        // Act
        Action act = () => services.RegisterServicesToLambda(configuration);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Postgres*inválidas*");
    }
}
