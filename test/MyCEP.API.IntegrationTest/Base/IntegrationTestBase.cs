using System.Text.Json;
using MyCEP.API.Core.Shared.Infrastructure.Database.Postgres;
using MyCEP.API.IntegrationTest.Infrastructure;

namespace MyCEP.API.IntegrationTest.Base
{
    /// <summary>
    /// Classe base para todos os testes de integração.
    /// Fornece infraestrutura comum e métodos utilitários para testes.
    /// </summary>
    [Collection("IntegrationTestCollection")]
    public abstract class IntegrationTestBase : IAsyncLifetime
    {
        protected readonly TestDatabaseManager DatabaseManager;
        protected readonly IntegrationTestServer TestServer;

        // Propriedades de conveniência para acesso rápido
        protected HttpClient Client => TestServer.Client;
        protected Microsoft.AspNetCore.TestHost.TestServer Server => TestServer.Server;
        protected MyCepDbContext DbContext => DatabaseManager.DbContext;

        protected IntegrationTestBase()
        {
            DatabaseManager = new TestDatabaseManager();
            TestServer = new IntegrationTestServer(DatabaseManager);
        }

        /// <summary>
        /// Inicialização padrão que pode ser sobrescrita por classes filhas
        /// </summary>
        public virtual async Task InitializeAsync()
        {
            await TestServer.InitializeAsync();
        }

        /// <summary>
        /// Limpeza padrão que pode ser sobrescrita por classes filhas
        /// </summary>
        public virtual async Task DisposeAsync()
        {
            await TestServer.CleanDatabaseAsync();
            await TestServer.DisposeAsync();
        }

        #region Database Helper Methods

        /// <summary>
        /// Limpa o banco de dados
        /// </summary>
        protected async Task CleanDatabaseAsync()
        {
            await TestServer.CleanDatabaseAsync();
        }

        /// <summary>
        /// Faz seed dos dados de teste padrão
        /// </summary>
        protected async Task SeedTestDataAsync()
        {
            await TestServer.SeedTestDataAsync();
        }

        #endregion

        #region HTTP Helper Methods

        /// <summary>
        /// Faz uma requisição GET e deserializa a resposta
        /// </summary>
        protected async Task<T?> GetAsync<T>(string requestUri)
        {
            var response = await Client.GetAsync(requestUri);
            var json = await response.Content.ReadAsStringAsync();

            return string.IsNullOrEmpty(json)
                ? default
                : JsonSerializer.Deserialize<T>(json, GetJsonSerializerOptions());
        }

        /// <summary>
        /// Faz uma requisição GET e retorna a resposta HTTP
        /// </summary>
        protected async Task<HttpResponseMessage> GetHttpResponseAsync(string requestUri)
        {
            return await Client.GetAsync(requestUri);
        }

        /// <summary>
        /// Faz uma requisição POST com JSON e retorna a resposta
        /// </summary>
        protected async Task<HttpResponseMessage> PostJsonAsync<T>(string requestUri, T data)
        {
            var json = JsonSerializer.Serialize(data, GetJsonSerializerOptions());
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            return await Client.PostAsync(requestUri, content);
        }

        /// <summary>
        /// Faz uma requisição PUT com JSON e retorna a resposta
        /// </summary>
        protected async Task<HttpResponseMessage> PutJsonAsync<T>(string requestUri, T data)
        {
            var json = JsonSerializer.Serialize(data, GetJsonSerializerOptions());
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            return await Client.PutAsync(requestUri, content);
        }

        /// <summary>
        /// Faz uma requisição DELETE e retorna a resposta
        /// </summary>
        protected async Task<HttpResponseMessage> DeleteAsync(string requestUri)
        {
            return await Client.DeleteAsync(requestUri);
        }

        #endregion

        #region Service Helper Methods

        /// <summary>
        /// Obtém um serviço do container de DI
        /// </summary>
        protected T GetService<T>() where T : notnull
        {
            return TestServer.GetService<T>();
        }

        /// <summary>
        /// Obtém um serviço com escopo do container de DI
        /// </summary>
        protected T GetScopedService<T>() where T : notnull
        {
            return TestServer.GetScopedService<T>();
        }

        /// <summary>
        /// Executa uma ação com um serviço em escopo
        /// </summary>
        protected async Task ExecuteWithScopedServiceAsync<T>(Func<T, Task> action) where T : notnull
        {
            await TestServer.ExecuteWithScopedServiceAsync(action);
        }

        /// <summary>
        /// Executa uma função com um serviço em escopo e retorna resultado
        /// </summary>
        protected async Task<TResult> ExecuteWithScopedServiceAsync<TService, TResult>(Func<TService, Task<TResult>> func) where TService : notnull
        {
            return await TestServer.ExecuteWithScopedServiceAsync(func);
        }

        #endregion

        #region JSON Configuration

        /// <summary>
        /// Configurações padrão para serialização JSON nos testes
        /// </summary>
        protected virtual JsonSerializerOptions GetJsonSerializerOptions()
        {
            return new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            };
        }

        #endregion
    }
}
