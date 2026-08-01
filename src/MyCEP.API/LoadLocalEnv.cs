using System.Diagnostics.CodeAnalysis;
using DotNetEnv;
namespace MyCEP.API
{
    [ExcludeFromCodeCoverage]
    public static class LoadLocalEnv
    {
        public static void Load()
        {
            // Verifica se está em ambiente de testes
            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            if (environment == "Testing")
            {
                // Não carrega arquivo .env em ambiente de testes
                return;
            }

            const string FileName = ".env";
            LoadSoollarEnviroment(FileName);
            var file = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, FileName);
            if (File.Exists(file))
                Env.Load(file);
        }

        private static void LoadSoollarEnviroment(in string fileName)
        {
            // Verifica se está em ambiente de testes antes de tentar criar arquivos
            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            if (environment == "Testing")
            {
                return;
            }

            var file = Path.Combine(GetPath(fileName)?.FullName ?? string.Empty, fileName);

            var dirTarget = AppDomain.CurrentDomain.BaseDirectory;
            var dirSource = Path.Combine(dirTarget, fileName);

            if (!File.Exists(file))
                return;

            try
            {
                var streamReader = File.OpenText(file);
                string fileInText = streamReader.ReadToEnd();
                streamReader.Close();

                var fileInfo = new FileInfo(dirSource);
                var streamWriter = fileInfo.CreateText();
                streamWriter.Write(fileInText);
                streamWriter.Close();
            }
            catch (IOException)
            {
                // Se houver problema de acesso ao arquivo, ignora silenciosamente
                // Isso evita problemas em ambiente de testes
            }
        }

        private static DirectoryInfo GetPath(string fileName)
        {
            var dir = Directory.GetParent(Directory.GetCurrentDirectory());
            while (dir != null && !dir.GetFiles(fileName).Any())
            {
                dir = dir.Parent;
            }

            return dir;
        }
    }

}
