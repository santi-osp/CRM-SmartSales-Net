param([switch]$Down)

$ErrorActionPreference = 'Stop'
$workspace = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$localState = Join-Path $workspace '.tools\local'
$secretsPath = Join-Path $localState 'secrets.json'
$privateKeyPath = Join-Path $localState 'jwt-private.pem'
$publicKeyPath = Join-Path $localState 'jwt-public.pem'
$openssl = 'C:\Program Files\Git\usr\bin\openssl.exe'
$environmentNames = @('LOCAL_DB_PASSWORD', 'LOCAL_ADMIN_PASSWORD', 'LOCAL_COMMERCIAL_PASSWORD',
    'LOCAL_JWT_PRIVATE_PEM', 'LOCAL_JWT_PUBLIC_PEM')
$previous = @{}

function New-RandomPassword {
    $bytes = New-Object byte[] 24
    $generator = [Security.Cryptography.RandomNumberGenerator]::Create()
    try { $generator.GetBytes($bytes) }
    finally { $generator.Dispose() }
    return [Convert]::ToBase64String($bytes)
}

try {
    if ($Down) {
        Push-Location $workspace
        try { docker compose down; if ($LASTEXITCODE -ne 0) { throw 'Docker Compose down falló.' } }
        finally { Pop-Location }
        return
    }
    New-Item -ItemType Directory -Path $localState -Force | Out-Null
    if (-not (Test-Path $secretsPath)) {
        $secrets = @{
            database = New-RandomPassword
            administrator = New-RandomPassword
            commercial = New-RandomPassword
        }
        [IO.File]::WriteAllText($secretsPath, ($secrets | ConvertTo-Json),
            [Text.UTF8Encoding]::new($false))
    }
    if (-not (Test-Path $privateKeyPath) -or -not (Test-Path $publicKeyPath)) {
        if (-not (Test-Path $openssl)) { throw 'Se necesita OpenSSL de Git para crear claves locales.' }
        & $openssl genpkey -quiet -algorithm RSA -pkeyopt rsa_keygen_bits:3072 -out $privateKeyPath -outpubkey $publicKeyPath
        if ($LASTEXITCODE -ne 0) { throw 'No se pudieron crear las claves locales.' }
    }
    $secrets = Get-Content -LiteralPath $secretsPath -Raw | ConvertFrom-Json
    $values = @{
        LOCAL_DB_PASSWORD = $secrets.database
        LOCAL_ADMIN_PASSWORD = $secrets.administrator
        LOCAL_COMMERCIAL_PASSWORD = $secrets.commercial
        LOCAL_JWT_PRIVATE_PEM = Get-Content -LiteralPath $privateKeyPath -Raw -Encoding ascii
        LOCAL_JWT_PUBLIC_PEM = Get-Content -LiteralPath $publicKeyPath -Raw -Encoding ascii
    }
    foreach ($name in $environmentNames) {
        $previous[$name] = [Environment]::GetEnvironmentVariable($name, 'Process')
        [Environment]::SetEnvironmentVariable($name, $values[$name], 'Process')
    }

    Push-Location $workspace
    try {
        docker compose build contacts sales web
        if ($LASTEXITCODE -ne 0) { throw 'Falló el build de contenedores.' }
        docker compose up -d postgres
        if ($LASTEXITCODE -ne 0) { throw 'No inició PostgreSQL.' }
        docker compose run --rm --entrypoint /app/migrate contacts
        if ($LASTEXITCODE -ne 0) { throw 'Falló la migración Contacts.' }
        docker compose run --rm --entrypoint /app/migrate sales
        if ($LASTEXITCODE -ne 0) { throw 'Falló la migración Sales.' }
        docker compose run --rm --entrypoint dotnet contacts SmartSales.Contacts.Api.dll --seed-users
        if ($LASTEXITCODE -ne 0) { throw 'Falló el seed de usuarios.' }
        docker compose run --rm --entrypoint dotnet contacts SmartSales.Contacts.Api.dll --seed-demo
        if ($LASTEXITCODE -ne 0) { throw 'Falló el seed de contactos.' }
        docker compose run --rm --entrypoint dotnet sales SmartSales.Sales.Api.dll --seed-demo
        if ($LASTEXITCODE -ne 0) { throw 'Falló el seed de Sales.' }
        docker compose up -d contacts sales web
        if ($LASTEXITCODE -ne 0) { throw 'No iniciaron los servicios.' }

        $deadline = (Get-Date).AddSeconds(90)
        foreach ($path in @('/healthz', '/api/contacts/health/ready', '/api/sales/health/ready')) {
            $ready = $false
            while ((Get-Date) -lt $deadline) {
                try {
                    $response = Invoke-WebRequest -Uri "http://127.0.0.1:8088$path" -UseBasicParsing -TimeoutSec 2
                    if ($response.StatusCode -eq 200) { $ready = $true; break }
                } catch { }
                Start-Sleep -Milliseconds 500
            }
            if (-not $ready) { throw "No respondió salud: $path" }
        }
        Write-Output 'SmartSales local listo en http://127.0.0.1:8088'
        Write-Output "Cuentas: admin@demo.local y comercial@demo.local; contraseñas en $secretsPath"
    } finally { Pop-Location }
} finally {
    foreach ($name in $environmentNames) {
        [Environment]::SetEnvironmentVariable($name, $previous[$name], 'Process')
    }
}
