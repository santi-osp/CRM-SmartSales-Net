param(
    [switch]$CheckRemote
)

$ErrorActionPreference = 'Stop'
$workspace = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path

Write-Output "=== PREFLIGHT CHECK — SMARTSALES CRM AWS DEPLOYMENT ==="
Write-Output "Workspace: $workspace"
Write-Output "Fecha UTC: $([DateTime]::UtcNow.ToString('yyyy-MM-dd HH:mm:ss'))"

# 1. Repositorio Git
Write-Output "`n[1/5] Verificando Git..."
try {
    $commit = (git -C $workspace rev-parse HEAD).Trim()
    $branch = (git -C $workspace rev-parse --abbrev-ref HEAD).Trim()
    Write-Output "  - Rama actual: $branch"
    Write-Output "  - Commit HEAD: $commit"
} catch {
    Write-Warning "  - Git no responde o no hay commits disponibles."
}

# 2. Toolchain local
Write-Output "`n[2/5] Verificando Toolchain local..."
$globalJson = Get-Content -LiteralPath "$workspace\global.json" -Raw | ConvertFrom-Json
Write-Output "  - .NET SDK en global.json: $($globalJson.sdk.version) ($($globalJson.sdk.rollForward))"

$node = Join-Path $workspace '.tools\node-v24.22.1-win-x64\node.exe'
if (Test-Path $node) {
    $nodeVer = (& $node -v)
    Write-Output "  - Node portable en .tools/: $nodeVer"
} else {
    Write-Warning "  - Node portable no encontrado en .tools/ (ver docs/toolchain-lock.md)"
}

# 3. Docker daemon
Write-Output "`n[3/5] Verificando Docker Desktop..."
try {
    $dockerInfo = docker version --format '{{.Server.Version}}' 2>$null
    if ($dockerInfo) {
        Write-Output "  - Docker daemon activo (Server v$dockerInfo)"
    } else {
        Write-Output "  - Docker daemon: INACTIVO / NO CONECTADO (Pruebas de contenedor bloqueadas localmente)"
    }
} catch {
    Write-Output "  - Docker daemon: INACTIVO"
}

# 4. Plantillas CloudFormation
Write-Output "`n[4/5] Verificando plantillas CloudFormation..."
$templates = @('network.yaml', 'data.yaml', 'application.yaml', 'delivery.yaml', 'parameters.example.json')
foreach ($tpl in $templates) {
    $path = Join-Path $workspace "infra\cloudformation\$tpl"
    if (Test-Path $path) {
        $size = (Get-Item $path).Length
        Write-Output "  - $tpl : OK ($size bytes)"
    } else {
        Write-Error "  - $tpl : FALTA en infra/cloudformation/"
    }
}

# 5. Artefactos de despliegue
Write-Output "`n[5/5] Verificando manifiestos deploy/..."
$manifests = @(
    'deploy/contacts/taskdef.json',
    'deploy/contacts/appspec.yaml',
    'deploy/sales/taskdef.json',
    'deploy/sales/appspec.yaml',
    'deploy/web/taskdef.json',
    'deploy/web/imagedefinitions.json'
)
foreach ($m in $manifests) {
    $path = Join-Path $workspace $m
    if (Test-Path $path) {
        Write-Output "  - $m : OK"
    } else {
        Write-Error "  - $m : FALTA"
    }
}

# Verificacion remota opcional (solo lectura)
if ($CheckRemote) {
    Write-Output "`n[EXTERNO] Comprobando identidad AWS CLI (Read-Only)..."
    try {
        $identity = aws sts get-caller-identity 2>&1
        if ($LASTEXITCODE -eq 0) {
            Write-Output "  - Identidad AWS: $identity"
        } else {
            Write-Warning "  - Sesión AWS no configurada o expirada (Gate G-02/G-04 pendiente)."
        }
    } catch {
        Write-Warning "  - aws CLI no disponible en el path."
    }
}

Write-Output "`nPreflight local completado. Sin operaciones de escritura en AWS."
