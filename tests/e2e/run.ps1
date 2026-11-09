param([string]$Grep = '')

$ErrorActionPreference = 'Stop'
$workspace = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$output = Join-Path $workspace '.tools\e2e'
New-Item -ItemType Directory -Path $output -Force | Out-Null
$node = Join-Path $workspace '.tools\node-v24.22.1-win-x64\node.exe'
$containerId = $null
$contactsProcess = $null
$salesProcess = $null
$webProcess = $null
$keyPath = Join-Path $output 'jwt-private.pem'
$publicPath = Join-Path $output 'jwt-public.pem'
$previous = @{}

function Set-RunEnvironment([string]$name, [string]$value) {
    if (-not $previous.ContainsKey($name)) {
        $previous[$name] = [Environment]::GetEnvironmentVariable($name, 'Process')
    }
    [Environment]::SetEnvironmentVariable($name, $value, 'Process')
}

function Get-FreePort {
    $listener = [Net.Sockets.TcpListener]::new([Net.IPAddress]::Loopback, 0)
    $listener.Start()
    try { return $listener.LocalEndpoint.Port }
    finally { $listener.Stop() }
}

function New-RandomPassword {
    $bytes = New-Object byte[] 24
    $generator = [Security.Cryptography.RandomNumberGenerator]::Create()
    try { $generator.GetBytes($bytes) }
    finally { $generator.Dispose() }
    return [Convert]::ToBase64String($bytes)
}

function Wait-Url([string]$url, $process, [int]$seconds) {
    $deadline = (Get-Date).AddSeconds($seconds)
    while ((Get-Date) -lt $deadline) {
        if ($process.HasExited) { throw "El proceso terminó antes de responder: $url" }
        try {
            $response = Invoke-WebRequest -Uri $url -UseBasicParsing -TimeoutSec 2
            if ($response.StatusCode -eq 200) { return }
        } catch { }
        Start-Sleep -Milliseconds 500
    }
    throw "Tiempo agotado esperando $url"
}

try {
    if (-not (Test-Path $node)) { throw 'Falta Node portable en .tools; ver docs/toolchain-lock.md.' }
    $dbPort = Get-FreePort
    $contactsPort = Get-FreePort
    $salesPort = Get-FreePort
    $webPort = Get-FreePort
    $dbPassword = New-RandomPassword
    $adminPassword = New-RandomPassword
    $commercialPassword = New-RandomPassword
    $name = 'smartsales-e2e-' + ([Guid]::NewGuid().ToString('N').Substring(0, 10))
    $containerId = (docker run -d --name $name -e "POSTGRES_PASSWORD=$dbPassword" -p "127.0.0.1:${dbPort}:5432" postgres:17).Trim()
    if ($LASTEXITCODE -ne 0 -or -not $containerId) { throw 'No se pudo iniciar PostgreSQL de E2E.' }
    $deadline = (Get-Date).AddSeconds(60)
    do {
        try { docker exec $containerId pg_isready -U postgres *> $null }
        catch { }
        if ($LASTEXITCODE -eq 0) { break }
        Start-Sleep -Milliseconds 500
    } while ((Get-Date) -lt $deadline)
    if ($LASTEXITCODE -ne 0) { throw 'PostgreSQL de E2E no estuvo listo.' }
    for ($attempt = 0; $attempt -lt 60; $attempt++) {
        try { docker exec $containerId psql -U postgres -c 'CREATE DATABASE contacts_db' *> $null }
        catch { }
        if ($LASTEXITCODE -eq 0) { break }
        Start-Sleep -Seconds 1
    }
    if ($LASTEXITCODE -ne 0) { throw 'No se creó contacts_db.' }
    for ($attempt = 0; $attempt -lt 60; $attempt++) {
        try { docker exec $containerId psql -U postgres -c 'CREATE DATABASE sales_db' *> $null }
        catch { }
        if ($LASTEXITCODE -eq 0) { break }
        Start-Sleep -Seconds 1
    }
    if ($LASTEXITCODE -ne 0) { throw 'No se creó sales_db.' }

    $openssl = 'C:\Program Files\Git\usr\bin\openssl.exe'
    if (-not (Test-Path $openssl)) { throw 'Falta OpenSSL de Git para claves efímeras E2E.' }
    & $openssl genpkey -quiet -algorithm RSA -pkeyopt rsa_keygen_bits:3072 -out $keyPath -outpubkey $publicPath
    if ($LASTEXITCODE -ne 0) { throw 'No se generó la clave de prueba.' }
    Set-RunEnvironment 'ConnectionStrings__Contacts' "Host=127.0.0.1;Port=$dbPort;Database=contacts_db;Username=postgres;Password=$dbPassword"
    Set-RunEnvironment 'ConnectionStrings__Sales' "Host=127.0.0.1;Port=$dbPort;Database=sales_db;Username=postgres;Password=$dbPassword"
    Set-RunEnvironment 'Jwt__PublicKeyPem' (Get-Content -LiteralPath $publicPath -Raw -Encoding ascii)
    Set-RunEnvironment 'Jwt__PrivateKeyPem' (Get-Content -LiteralPath $keyPath -Raw -Encoding ascii)
    Set-RunEnvironment 'Jwt__KeyId' 'local-e2e'
    Set-RunEnvironment 'ASPNETCORE_ENVIRONMENT' 'Development'
    Set-RunEnvironment 'Logging__LogLevel__Microsoft.EntityFrameworkCore' 'Warning'
    Set-RunEnvironment 'Demo__Enabled' 'true'
    Set-RunEnvironment 'Seed__Administrator__Email' 'e2e-admin@demo.example'
    Set-RunEnvironment 'Seed__Administrator__DisplayName' 'Administradora Demo'
    Set-RunEnvironment 'Seed__Administrator__Password' $adminPassword
    Set-RunEnvironment 'Seed__Commercial__Email' 'e2e-commercial@demo.example'
    Set-RunEnvironment 'Seed__Commercial__DisplayName' 'Comercial Demo'
    Set-RunEnvironment 'Seed__Commercial__Password' $commercialPassword
    Set-RunEnvironment 'E2E_ADMIN_EMAIL' 'e2e-admin@demo.example'
    Set-RunEnvironment 'E2E_ADMIN_PASSWORD' $adminPassword
    Set-RunEnvironment 'E2E_COMMERCIAL_EMAIL' 'e2e-commercial@demo.example'
    Set-RunEnvironment 'E2E_COMMERCIAL_PASSWORD' $commercialPassword
    Set-RunEnvironment 'ContactsBaseUrl' "http://127.0.0.1:$contactsPort"

    Push-Location $workspace
    try {
        dotnet build SmartSales.slnx --configuration Release --no-restore
        if ($LASTEXITCODE -ne 0) { throw 'Falló el build de .NET.' }
        dotnet tool run dotnet-ef database update --project src/Contacts/SmartSales.Contacts.Api/SmartSales.Contacts.Api.csproj --startup-project src/Contacts/SmartSales.Contacts.Api/SmartSales.Contacts.Api.csproj --context ContactsDbContext
        if ($LASTEXITCODE -ne 0) { throw 'Falló la migración Contacts.' }
        dotnet tool run dotnet-ef database update --project src/Sales/SmartSales.Sales.Api/SmartSales.Sales.Api.csproj --startup-project src/Sales/SmartSales.Sales.Api/SmartSales.Sales.Api.csproj --context SalesDbContext
        if ($LASTEXITCODE -ne 0) { throw 'Falló la migración Sales.' }
        dotnet src/Contacts/SmartSales.Contacts.Api/bin/Release/net10.0/SmartSales.Contacts.Api.dll --seed-users
        if ($LASTEXITCODE -ne 0) { throw 'Falló el seed de usuarios.' }
        dotnet src/Contacts/SmartSales.Contacts.Api/bin/Release/net10.0/SmartSales.Contacts.Api.dll --seed-demo
        if ($LASTEXITCODE -ne 0) { throw 'Falló el seed de contactos.' }
        dotnet src/Sales/SmartSales.Sales.Api/bin/Release/net10.0/SmartSales.Sales.Api.dll --seed-demo
        if ($LASTEXITCODE -ne 0) { throw 'Falló el seed de ventas.' }

        $proxy = @{
            '/api/contacts' = @{ target = "http://127.0.0.1:$contactsPort"; secure = $false; changeOrigin = $true }
            '/api/sales' = @{ target = "http://127.0.0.1:$salesPort"; secure = $false; changeOrigin = $true }
        } | ConvertTo-Json -Depth 4
        [IO.File]::WriteAllText((Join-Path $output 'proxy.conf.json'), $proxy,
            [Text.UTF8Encoding]::new($false))
        Set-RunEnvironment 'ASPNETCORE_URLS' "http://127.0.0.1:$contactsPort"
        $contactsStart = @{
            FilePath = 'dotnet'
            ArgumentList = @('src/Contacts/SmartSales.Contacts.Api/bin/Release/net10.0/SmartSales.Contacts.Api.dll')
            WorkingDirectory = $workspace
            WindowStyle = 'Hidden'
            PassThru = $true
            RedirectStandardOutput = (Join-Path $output 'contacts.log')
            RedirectStandardError = (Join-Path $output 'contacts-error.log')
        }
        $contactsProcess = Start-Process @contactsStart
        Wait-Url "http://127.0.0.1:$contactsPort/api/contacts/health/ready" $contactsProcess 60
        Set-RunEnvironment 'ASPNETCORE_URLS' "http://127.0.0.1:$salesPort"
        $salesStart = @{
            FilePath = 'dotnet'
            ArgumentList = @('src/Sales/SmartSales.Sales.Api/bin/Release/net10.0/SmartSales.Sales.Api.dll')
            WorkingDirectory = $workspace
            WindowStyle = 'Hidden'
            PassThru = $true
            RedirectStandardOutput = (Join-Path $output 'sales.log')
            RedirectStandardError = (Join-Path $output 'sales-error.log')
        }
        $salesProcess = Start-Process @salesStart
        Wait-Url "http://127.0.0.1:$salesPort/api/sales/health/ready" $salesProcess 60

        $webStart = @{
            FilePath = $node
            ArgumentList = @('node_modules/@angular/cli/bin/ng.js', 'serve', '--host', '127.0.0.1',
                '--port', "$webPort", '--ssl', 'false', '--proxy-config',
                '../../.tools/e2e/proxy.conf.json')
            WorkingDirectory = (Join-Path $workspace 'src/Web')
            WindowStyle = 'Hidden'
            PassThru = $true
            RedirectStandardOutput = (Join-Path $output 'web.log')
            RedirectStandardError = (Join-Path $output 'web-error.log')
        }
        $webProcess = Start-Process @webStart
        Wait-Url "http://127.0.0.1:$webPort/login" $webProcess 120
        Set-RunEnvironment 'E2E_BASE_URL' "http://127.0.0.1:$webPort"
        Push-Location (Join-Path $workspace 'src/Web')
        try {
            if ($Grep) { & $node 'node_modules/@playwright/test/cli.js' test --grep $Grep }
            else { & $node 'node_modules/@playwright/test/cli.js' test }
            if ($LASTEXITCODE -ne 0) { throw 'Fallaron las pruebas E2E.' }
        } finally { Pop-Location }
    } finally { Pop-Location }
} finally {
    foreach ($process in @($webProcess, $salesProcess, $contactsProcess)) {
        if ($null -ne $process -and -not $process.HasExited) {
            Stop-Process -Id $process.Id -Force -ErrorAction SilentlyContinue
        }
    }
    if ($containerId) { docker rm -f $containerId | Out-Null }
    if (Test-Path $keyPath) { Remove-Item -LiteralPath $keyPath -Force }
    if (Test-Path $publicPath) { Remove-Item -LiteralPath $publicPath -Force }
    foreach ($name in $previous.Keys) {
        [Environment]::SetEnvironmentVariable($name, $previous[$name], 'Process')
    }
}
