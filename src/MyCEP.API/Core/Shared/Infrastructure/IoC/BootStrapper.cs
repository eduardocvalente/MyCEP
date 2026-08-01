using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using MyCEP.API.Core.Features.UseCase.IZipCodeLookup;
using MyCEP.API.Core.Shared.Domain.Repository;
using MyCEP.API.Core.Shared.Domain.Repository.Base;
using MyCEP.API.Core.Shared.Domain.Service.ViaCep;
using MyCEP.API.Core.Shared.Infrastructure.CrossCutting.UnitofWork;
using MyCEP.API.Core.Shared.Infrastructure.Database.Postgres;
using MyCEP.API.Core.Shared.Infrastructure.Gateways;
using MyCEP.API.Core.Shared.Infrastructure.Options;
using MyCEP.API.Core.Shared.Infrastructure.Repository.Base;
using Refit;

namespace MyCEP.API.Core.Shared.Infrastructure.IoC
{
    public static class BootStrapper
    {
        public static IServiceCollection RegisterServicesToLambda(this IServiceCollection services, IConfiguration configuration, bool isDevelopment = false)
        {
            return services
                .AddServices(configuration)
                .AddPgAdminDatabase(configuration, isDevelopment)
                .AddViaCepGateway(configuration)
                .AddUseCase(configuration)
                .AddRepositories(configuration);
        }

        private static IServiceCollection AddUseCase(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddScoped<IZipCodeLookupUseCase, ZipCodeLookupUseCase>();

            return services;
        }

        private static IServiceCollection AddServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddScoped<IViaCepService, ViaCepService>();

            return services;
        }

        private static IServiceCollection AddRepositories(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddScoped(typeof(IGenericRepository<,>), typeof(GenericBaseRepository<,>));
            services.AddScoped<IAddressRepository, AddressRepository>();
            return services;
        }

        private static IServiceCollection AddViaCepGateway(this IServiceCollection services, IConfiguration configuration)
        {
            var viaCepOptions = configuration.GetSection(ViaCepOptions.SectionName).Get<ViaCepOptions>();

            if (viaCepOptions == null || string.IsNullOrWhiteSpace(viaCepOptions.BaseUrlApi))
            {
                throw new InvalidOperationException("As configurações do ViaCEP não estão definidas ou são inválidas.");
            }

            var timeout = viaCepOptions.TimeoutInSeconds > 0
                ? TimeSpan.FromSeconds(viaCepOptions.TimeoutInSeconds)
                : TimeSpan.FromSeconds(10);

            services
                .AddRefitClient<IViaCepApiGateway>(new RefitSettings
                {
                    ContentSerializer = new SystemTextJsonContentSerializer(
                        new JsonSerializerOptions
                        {
                            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                        }
                    )
                })
                .ConfigureHttpClient(client =>
                {
                    client.BaseAddress = new Uri(viaCepOptions.BaseUrlApi);
                    client.Timeout = timeout;
                })
                // Retry com backoff + circuit breaker + timeout por tentativa.
                // O ViaCEP é um serviço gratuito sem SLA: sem isso, uma
                // degradação dele vira indisponibilidade nossa.
                .AddStandardResilienceHandler();

            return services;
        }

        private static IServiceCollection AddPgAdminDatabase(this IServiceCollection services, IConfiguration configuration, bool isDevelopment)
        {
            var postgresOptions = configuration.GetSection(PostgresOptions.SectionName).Get<PostgresOptions>();

            if (postgresOptions == null ||
                string.IsNullOrWhiteSpace(postgresOptions.PostgresHost) ||
                string.IsNullOrWhiteSpace(postgresOptions.PostgresDatabase) ||
                string.IsNullOrWhiteSpace(postgresOptions.PostgresUsername) ||
                string.IsNullOrWhiteSpace(postgresOptions.PostgresPort))
            {
                throw new InvalidOperationException("As configurações do Postgres não estão definidas ou são inválidas.");
            }

            services.AddScoped<IUnitOfWork, PostgresUnitOfWork>();
            services.AddDbContext<MyCepDbContext>(options =>
            {
                options.UseNpgsql(postgresOptions!.ToConnectionString());

                // Loga o valor dos parâmetros do SQL — inclusive CEPs e endereços.
                // Em produção isso iria parar no CloudWatch.
                if (isDevelopment)
                {
                    options.EnableSensitiveDataLogging();
                }
            });
            return services;
        }
    }
}
