param(
    [string]$AlbBaseUrl = 'http://127.0.0.1:8088',
    [string]$AdminEmail = 'admin@demo.local',
    [string]$AdminPassword = ''
)

$ErrorActionPreference = 'Stop'

Write-Output "=== VERIFICACIÓN Y SMOKE TESTS — SMARTSALES CRM ==="
Write-Output "Target ALB URL: $AlbBaseUrl"
Write-Output "Fecha UTC: $([DateTime]::UtcNow.ToString('yyyy-MM-dd HH:mm:ss'))"

# 1. Health checks públicos
$endpoints = @(
    @{ Path = '/healthz'; Expected = 200; Description = 'Web Nginx Health' },
    @{ Path = '/api/contacts/health/ready'; Expected = 200; Description = 'Contacts API Readiness' },
    @{ Path = '/api/sales/health/ready'; Expected = 200; Description = 'Sales API Readiness' }
)

$allPassed = $true

foreach ($ep in $endpoints) {
    $url = "$AlbBaseUrl$($ep.Path)"
    Write-Output "Consultando $($ep.Description) ($url)..."
    try {
        $res = Invoke-WebRequest -Uri $url -UseBasicParsing -TimeoutSec 5
        if ($res.StatusCode -eq $ep.Expected) {
            Write-Output "  [PASS] Status code $($res.StatusCode)"
        } else {
            Write-Warning "  [FAIL] Status code $($res.StatusCode) (esperado: $($ep.Expected))"
            $allPassed = $false
        }
    } catch {
        Write-Warning "  [FAIL] Error de conexión: $($_.Exception.Message)"
        $allPassed = $false
    }
}

# 2. Prueba autenticada si se proporciona contraseña
if ($AdminPassword) {
    Write-Output "`nProbando login autenticado con $AdminEmail..."
    $loginUrl = "$AlbBaseUrl/api/contacts/v1/auth/login"
    $body = @{ email = $AdminEmail; password = $AdminPassword } | ConvertTo-Json
    try {
        $loginRes = Invoke-RestMethod -Uri $loginUrl -Method Post -Body $body -ContentType 'application/json' -TimeoutSec 5
        if ($loginRes.accessToken) {
            Write-Output "  [PASS] Token JWT obtenido exitosamente (expira en $($loginRes.expiresIn) s)"
            
            # Probar llamada a Sales API con Bearer
            $dashUrl = "$AlbBaseUrl/api/sales/v1/dashboard"
            $headers = @{ Authorization = "Bearer $($loginRes.accessToken)" }
            $dashRes = Invoke-RestMethod -Uri $dashUrl -Headers $headers -TimeoutSec 5
            Write-Output "  [PASS] Dashboard consultado: $($dashRes.openCount) abiertas, $($dashRes.wonCount) ganadas."
        }
    } catch {
        Write-Warning "  [FAIL] Falló la prueba autenticada: $($_.Exception.Message)"
        $allPassed = $false
    }
} else {
    Write-Output "`n(Para ejecutar smoke tests autenticados, pasa -AdminPassword)"
}

if ($allPassed) {
    Write-Output "`nTodos los checks respondieron correctamente."
} else {
    Write-Warning "`nUno o más checks no respondieron como se esperaba."
}
