# Script para executar testes com cobertura localmente
param(
    [int]$MinimumCoverage = 80,
    [string]$OutputPath = "TestResults"
)

Write-Host "🚀 Iniciando execução de testes com cobertura..." -ForegroundColor Green
Write-Host "📊 Cobertura mínima exigida: $MinimumCoverage%" -ForegroundColor Yellow

# Limpar resultados anteriores
if (Test-Path $OutputPath) {
    Remove-Item -Path $OutputPath -Recurse -Force
}

# Restaurar dependências
Write-Host "📦 Restaurando dependências..." -ForegroundColor Blue
dotnet restore MyCEP.API.sln

# Build da solução
Write-Host "🔨 Compilando solução..." -ForegroundColor Blue
dotnet build MyCEP.API.sln --no-restore --configuration Release

if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Falha na compilação!" -ForegroundColor Red
    exit 1
}

# Instalar ferramentas de cobertura
Write-Host "🛠️ Instalando ferramentas de cobertura..." -ForegroundColor Blue
dotnet tool install --global dotnet-reportgenerator-globaltool --ignore-failed-sources
dotnet tool install --global coverlet.console --ignore-failed-sources

# Configurar variáveis de ambiente para testes
$env:ASPNETCORE_ENVIRONMENT = "Test"
$env:VIACEP_BASEURL_API = "https://viacep.com.br/ws/"
$env:VIACEP_TIMEOUT_SECONDS = "30"
$env:POSTGRES_HOST = "localhost"
$env:POSTGRES_PORT = "5432"
$env:POSTGRES_DATABASE = "testdb"
$env:POSTGRES_USERNAME = "postgres"
$env:POSTGRES_PASSWORD = "postgres"

# Executar testes unitários
Write-Host "🧪 Executando testes unitários..." -ForegroundColor Cyan
dotnet test test/MyCEP.API.UnitTest/MyCEP.API.UnitTest.csproj `
    --configuration Release `
    --no-build `
    --verbosity normal `
    --collect:"XPlat Code Coverage" `
    --results-directory "./$OutputPath/Unit" `
    --logger trx `
    --logger "console;verbosity=detailed" `
    --settings coverage.runsettings

if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Falha nos testes unitários!" -ForegroundColor Red
    exit 1
}

# Executar testes de integração
Write-Host "🔗 Executando testes de integração..." -ForegroundColor Cyan
dotnet test test/MyCEP.API.IntegrationTest/MyCEP.API.IntegrationTest.csproj `
    --configuration Release `
    --no-build `
    --verbosity normal `
    --collect:"XPlat Code Coverage" `
    --results-directory "./$OutputPath/Integration" `
    --logger trx `
    --logger "console;verbosity=detailed" `
    --settings coverage.runsettings

if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Falha nos testes de integração!" -ForegroundColor Red
    exit 1
}

# Gerar relatório de cobertura consolidado
Write-Host "📊 Gerando relatório de cobertura..." -ForegroundColor Magenta
reportgenerator `
    -reports:"$OutputPath/Unit/**/coverage.cobertura.xml;$OutputPath/Integration/**/coverage.cobertura.xml" `
    -targetdir:"$OutputPath/Merged" `
    -reporttypes:"Cobertura;HtmlInline_AzurePipelines;JsonSummary;TextSummary;Html;MarkdownSummaryGithub" `
    -filefilters:"-**/Migrations/**" `
    -verbosity:Info

if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Falha na geração do relatório!" -ForegroundColor Red
    exit 1
}

# Extrair porcentagem de cobertura
$summaryFile = "$OutputPath/Merged/Summary.txt"
if (Test-Path $summaryFile) {
    $summaryContent = Get-Content $summaryFile
    $coverageLine = $summaryContent | Select-String "Line coverage:"
    if ($coverageLine) {
        $coveragePercent = [regex]::Match($coverageLine, "Line coverage: (\d+(?:\.\d+)?%)").Groups[1].Value
        $coverageValue = [double]([regex]::Match($coveragePercent, "(\d+(?:\.\d+)?)").Groups[1].Value)
        
        Write-Host "" -ForegroundColor White
        Write-Host "📋 RESUMO DE COBERTURA" -ForegroundColor White -BackgroundColor DarkBlue
        Write-Host "========================" -ForegroundColor White -BackgroundColor DarkBlue
        Write-Host "Cobertura atual: $coveragePercent" -ForegroundColor White
        Write-Host "Cobertura mínima: $MinimumCoverage%" -ForegroundColor White
        Write-Host "" -ForegroundColor White
        
        # Mostrar resumo detalhado
        Write-Host "📊 Detalhes da Cobertura:" -ForegroundColor Cyan
        Get-Content $summaryFile | ForEach-Object {
            if ($_ -match "Line coverage:|Branch coverage:|Covered lines:|Uncovered lines:|Coverable lines:") {
                Write-Host "   $_" -ForegroundColor White
            }
        }
        
        Write-Host "" -ForegroundColor White
        Write-Host "📁 Relatório HTML gerado em: $OutputPath/Merged/index.html" -ForegroundColor Green
        Write-Host "" -ForegroundColor White
        
        # Verificar quality gate
        if ($coverageValue -ge $MinimumCoverage) {
            Write-Host "✅ QUALITY GATE PASSED!" -ForegroundColor Green -BackgroundColor DarkGreen
            Write-Host "🚀 Deploy aprovado! A cobertura de $coveragePercent atende ao mínimo exigido." -ForegroundColor Green
            
            # Abrir relatório HTML
            $htmlReport = "$OutputPath/Merged/index.html"
            if (Test-Path $htmlReport) {
                Write-Host "🌐 Abrindo relatório HTML..." -ForegroundColor Cyan
                Start-Process $htmlReport
            }
            
            exit 0
        } else {
            Write-Host "❌ QUALITY GATE FAILED!" -ForegroundColor Red -BackgroundColor DarkRed
            Write-Host "🚫 Deploy bloqueado! A cobertura de $coveragePercent está abaixo do mínimo de $MinimumCoverage%." -ForegroundColor Red
            Write-Host "📈 Adicione mais testes para aumentar a cobertura." -ForegroundColor Yellow
            
            # Abrir relatório HTML mesmo com falha
            $htmlReport = "$OutputPath/Merged/index.html"
            if (Test-Path $htmlReport) {
                Write-Host "🌐 Abrindo relatório HTML para análise..." -ForegroundColor Cyan
                Start-Process $htmlReport
            }
            
            exit 1
        }
    } else {
        Write-Host "❌ Não foi possível extrair a porcentagem de cobertura!" -ForegroundColor Red
        exit 1
    }
} else {
    Write-Host "❌ Arquivo de resumo não encontrado!" -ForegroundColor Red
    exit 1
}
