param(
    [switch]$UseAwsCli
)

$ErrorActionPreference = 'Stop'
$workspace = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path

Write-Output "=== VALIDACIÓN DE PLANTILLAS Y MANIFIESTOS (ARCH-001 v1) ==="

$cfDir = Join-Path $workspace 'infra/cloudformation'
$templates = @('network.yaml', 'data.yaml', 'application.yaml', 'delivery.yaml')

$allValid = $true

foreach ($t in $templates) {
    $fullPath = Join-Path $cfDir $t
    Write-Output "Analizando $t..."
    $content = Get-Content -LiteralPath $fullPath -Raw
    
    # Comprobar cabecera CloudFormation
    if ($content -notmatch 'AWSTemplateFormatVersion:\s*[\x27\x22]?2010-09-09[\x27\x22]?') {
        Write-Warning "  - $t no tiene cabecera AWSTemplateFormatVersion valida."
        $allValid = $false
    } else {
        Write-Output "  - Formato CloudFormation: OK"
    }

    # Comprobacion sintactica con aws cloudformation validate-template si se solicita
    if ($UseAwsCli) {
        try {
            $valOut = aws cloudformation validate-template --template-body "file://$fullPath" 2>&1
            if ($LASTEXITCODE -eq 0) {
                Write-Output "  - aws cloudformation validate-template: PASS"
            } else {
                Write-Warning "  - aws validate-template fallo o faltan credenciales: $valOut"
            }
        } catch {
            Write-Warning "  - aws CLI no disponible para validacion remota."
        }
    }
}

# Validar JSON de Task Definitions y AppSpec
$jsonFiles = @(
    'deploy/contacts/taskdef.json',
    'deploy/sales/taskdef.json',
    'deploy/web/taskdef.json',
    'deploy/web/imagedefinitions.json',
    'infra/cloudformation/parameters.example.json'
)

foreach ($j in $jsonFiles) {
    $path = Join-Path $workspace $j
    try {
        $parsed = Get-Content -LiteralPath $path -Raw | ConvertFrom-Json
        Write-Output "JSON $j : VALIDO"
    } catch {
        Write-Error "JSON $j : INVALIDO ($($_.Exception.Message))"
        $allValid = $false
    }
}

if ($allValid) {
    Write-Output "`nValidacion sintactica local completada con éxito."
} else {
    Write-Error "`nSe detectaron inconsistencias en las plantillas."
}
