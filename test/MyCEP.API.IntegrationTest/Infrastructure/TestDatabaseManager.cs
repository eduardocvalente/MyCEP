using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using MyCEP.API.Core.Shared.Infrastructure.Database.Postgres;
using MyCEP.API.Core.Shared.Infrastructure.Schemas;
using Testcontainers.PostgreSql;

namespace MyCEP.API.IntegrationTest.Infrastructure
{
    /// <summary>
    /// Gerenciador centralizado do banco de dados para testes de integração.
    /// Responsável por criar, configurar e gerenciar o container PostgreSQL de teste.
    /// </summary>
    public class TestDatabaseManager : IAsyncDisposable
    {
        private readonly PostgreSqlContainer _postgresContainer;
        private bool _isInitialized = false;
        private MyCepDbContext? _dbContext;

        public string ConnectionString { get; private set; } = string.Empty;
        public IConfiguration Configuration { get; private set; } = null!;
        public MyCepDbContext DbContext => _dbContext ??= CreateDbContext();

        public TestDatabaseManager()
        {
            _postgresContainer = new PostgreSqlBuilder()
                .WithDatabase("testdb")
                .WithUsername("testuser")
                .WithPassword("testpass")
                .WithCleanUp(true)
                .Build();
        }

        /// <summary>
        /// Inicializa o container PostgreSQL e prepara a configuração
        /// </summary>
        public async Task InitializeAsync()
        {
            if (_isInitialized) return;

            await _postgresContainer.StartAsync();
            ConnectionString = _postgresContainer.GetConnectionString();

            Configuration = CreateTestConfiguration();
            _isInitialized = true;
        }

        /// <summary>
        /// Cria a configuração para testes baseada nas configurações da Lambda
        /// </summary>
        private IConfiguration CreateTestConfiguration()
        {
            // Usar as configurações da Lambda como base
            var lambdaConfig = LambdaConfiguration.GetConfiguration();

            var configBuilder = new ConfigurationBuilder()
                .AddConfiguration(lambdaConfig) // Adiciona todas as configurações da Lambda
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    // Sobrescrever apenas as configurações específicas do banco de teste
                    ["Postgres:PostgresHost"] = _postgresContainer.Hostname,
                    ["Postgres:PostgresPort"] = _postgresContainer.GetMappedPublicPort(5432).ToString(),
                    ["Postgres:PostgresDatabase"] = "testdb",
                    ["Postgres:PostgresUsername"] = "testuser",
                    ["Postgres:PostgresPassword"] = "testpass",

                    // Configurações da API externa (fake) - mantém o mesmo formato da Lambda
                    ["ViaCep:BaseUrlApi"] = "https://fake.viacep.local",
                    ["ViaCep:TimeoutInSeconds"] = "30",

                    // Configurações de logging específicas para testes
                    ["Logging:LogLevel:Microsoft.EntityFrameworkCore"] = "Warning"
                });

            return configBuilder.Build();
        }

        /// <summary>
        /// Cria um contexto de banco de dados para operações diretas
        /// </summary>
        public MyCepDbContext CreateDbContext()
        {
            if (!_isInitialized)
                throw new InvalidOperationException("Database manager not initialized. Call InitializeAsync first.");

            var options = new DbContextOptionsBuilder<MyCepDbContext>()
                .UseNpgsql(ConnectionString)
                .EnableSensitiveDataLogging()
                .EnableDetailedErrors()
                .Options;

            return new MyCepDbContext(options);
        }

        /// <summary>
        /// Limpa todos os dados do banco, mantendo a estrutura
        /// </summary>
        public async Task CleanDatabaseAsync()
        {
            try
            {
                using var dbContext = CreateDbContext();

                // Verificar se a tabela existe antes de limpar
                var connection = dbContext.Database.GetDbConnection();
                await dbContext.Database.OpenConnectionAsync();

                using var command = connection.CreateCommand();
                command.CommandText = @"
                    SELECT COUNT(*) 
                    FROM information_schema.tables 
                    WHERE table_schema = 'public' AND table_name = 'address'";

                var tableExists = Convert.ToInt32(await command.ExecuteScalarAsync()) > 0;

                if (tableExists)
                {
                    await dbContext.Database.ExecuteSqlRawAsync("TRUNCATE TABLE address RESTART IDENTITY CASCADE");
                }
            }
            catch (Exception)
            {
                // Ignora erros de limpeza - tabela pode não existir ainda
            }
        }

        /// <summary>
        /// Faz seed de dados de teste padrão
        /// </summary>
        public async Task SeedTestDataAsync()
        {
            using var dbContext = CreateDbContext();

            // Verificar se já existem dados para evitar duplicatas
            if (await dbContext.Addresses.AnyAsync())
                return;

            var testAddresses = CreateTestAddresses();
            await dbContext.Addresses.AddRangeAsync(testAddresses);
            await dbContext.SaveChangesAsync();
        }

        /// <summary>
        /// Cria endereços de teste padrão
        /// </summary>
        private static List<AddressInfra> CreateTestAddresses()
        {
            return new List<AddressInfra>
            {
                new()
                {
                    Id = new Guid("b3b5b50f-8c8f-4f3b-a8fa-cf6e03750df1"),
                    Zipcode = "01001000",
                    Street = "Praça da Sé",
                    Complement = "lado ímpar",
                    Neighborhood = "Sé",
                    City = "São Paulo",
                    State = "SP",
                    IbgeCode = "3550308",
                    GiaCode = "1004",
                    Ddd = "11",
                    SiafiCode = "7107",
                    CreatedAt = new DateTimeOffset(2024, 2, 26, 12, 0, 0, TimeSpan.Zero)
                },
                new()
                {
                    Id = new Guid("bba8c410-1f3a-4b5c-93c3-8317ebaa7f9e"),
                    Zipcode = "20000000",
                    Street = "Rua da Assembleia",
                    Complement = "Sala 202",
                    Neighborhood = "Centro",
                    City = "Rio de Janeiro",
                    State = "RJ",
                    IbgeCode = "3304557",
                    GiaCode = "2001",
                    Ddd = "21",
                    SiafiCode = "6001",
                    CreatedAt = new DateTimeOffset(2024, 2, 26, 12, 0, 0, TimeSpan.Zero)
                }
            };
        }

        /// <summary>
        /// Faz seed de endereços customizados
        /// </summary>
        public async Task SeedCustomAddressesAsync(params AddressInfra[] addresses)
        {
            using var dbContext = CreateDbContext();
            await dbContext.Addresses.AddRangeAsync(addresses);
            await dbContext.SaveChangesAsync();
        }

        public async ValueTask DisposeAsync()
        {
            _dbContext?.Dispose();
            if (_postgresContainer != null)
            {
                await _postgresContainer.DisposeAsync();
            }
        }
    }

    /// <summary>
    /// Collection definition para compartilhar o TestDatabaseManager entre todos os testes
    /// </summary>
    [CollectionDefinition("IntegrationTestCollection")]
    public class IntegrationTestCollection : ICollectionFixture<TestDatabaseManager>
    {
        // Esta classe existe apenas para definir a collection fixture
    }
}
