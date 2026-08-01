namespace MyCEP.API.Core.Shared.Infrastructure.Options
{
    public sealed class PostgresOptions
    {
        public const string SectionName = "Postgres";
        public string PostgresHost { get; set; } = string.Empty;
        public string PostgresPort { get; set; } = string.Empty;
        public string PostgresDatabase { get; set; } = string.Empty;
        public string PostgresUsername { get; set; } = string.Empty;
        public string PostgresPassword { get; set; } = string.Empty;

        /// <summary>
        /// Prefer em desenvolvimento; Require no RDS, que recusa conexão sem TLS.
        /// </summary>
        public string? PostgresSslMode { get; set; }

        public string ToConnectionString()
        {
            var sslMode = string.IsNullOrWhiteSpace(PostgresSslMode) ? "Prefer" : PostgresSslMode;

            var connectionString =
                $"Host={PostgresHost};Port={PostgresPort};Database={PostgresDatabase};" +
                $"Username={PostgresUsername};Password={PostgresPassword};SSL Mode={sslMode}";

            // O RDS usa uma CA própria da Amazon, que não está no trust store da
            // imagem. Validar a cadeia exigiria embarcar o bundle rds-ca no container.
            if (!sslMode.Equals("Disable", StringComparison.OrdinalIgnoreCase))
            {
                connectionString += ";Trust Server Certificate=true";
            }

            return connectionString;
        }
    }
}
