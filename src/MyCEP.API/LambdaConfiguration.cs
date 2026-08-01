namespace MyCEP.API
{
    using System.Collections.Concurrent;

    public static class LambdaConfiguration
    {
        public static IConfiguration GetConfiguration()
        {
            var configurationBuild = new ConfigurationBuilder();
#if DEBUG
            // Carrega variáveis de ambiente locais para desenvolvimento
            // (LoadLocalEnv.Load() já verifica se está em ambiente de testes)
            LoadLocalEnv.Load();
#endif
            configurationBuild.AddInMemoryCollection(new ConcurrentDictionary<string, string?>(new Dictionary<string, string?>
            {
                // Logging
                { "Logging:LogLevel:Default", "Information" },
                { "Logging:LogLevel:Microsoft.AspNetCore", "Warning" },
                { "Logging:LogLevel:System.Net.Http.HttpClient", "Warning" },

                // ViaCEP
                { "ViaCep:BaseUrlApi", Environment.GetEnvironmentVariable("VIACEP_BASEURL_API")},
                { "ViaCep:TimeoutInSeconds", Environment.GetEnvironmentVariable("VIACEP_TIMEOUT_SECONDS")},

                // Postgres
                { "Postgres:PostgresHost", Environment.GetEnvironmentVariable("POSTGRES_HOST")},
                { "Postgres:PostgresPort", Environment.GetEnvironmentVariable("POSTGRES_PORT")},
                { "Postgres:PostgresDatabase", Environment.GetEnvironmentVariable("POSTGRES_DATABASE")},
                { "Postgres:PostgresUsername", Environment.GetEnvironmentVariable("POSTGRES_USERNAME")},
                { "Postgres:PostgresPassword", Environment.GetEnvironmentVariable("POSTGRES_PASSWORD")},
                { "Postgres:PostgresSslMode", Environment.GetEnvironmentVariable("POSTGRES_SSLMODE")},

                // Aplica as migrations no boot. Só faz sentido com instância única —
                // com várias réplicas duas subiriam migrando ao mesmo tempo.
                { "Database:MigrateOnStartup", Environment.GetEnvironmentVariable("DATABASE_MIGRATE_ON_STARTUP")},
            }));

            return configurationBuild.Build();
        }
    }
}
