namespace MyCEP.API.Core.Shared.Infrastructure.Options
{
    public sealed class ViaCepOptions
    {
        public const string SectionName = "ViaCep";
        public string BaseUrlApi { get; set; } = string.Empty;

        /// <summary>
        /// Timeout do HttpClient do ViaCEP. O padrão de 10s é deliberadamente
        /// curto: a consulta é interativa e segurar a requisição por mais tempo
        /// só acumula conexões penduradas quando o ViaCEP degrada.
        /// </summary>
        public int TimeoutInSeconds { get; set; } = 10;
    }
}
