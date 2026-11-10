param(
    [string]$CommitSha = '',
    [string]$OutDir = 'artifacts/releases',
    [string]$ImageDigestContacts = '<PLACEHOLDER_CONTACTS_DIGEST>',
    [string]$ImageDigestSales = '<PLACEHOLDER_SALES_DIGEST>',
    [string]$ImageDigestWeb = '<PLACEHOLDER_WEB_DIGEST>'
)

$ErrorActionPreference = 'Stop'
$workspace = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path

if (-not $CommitSha) {
    try {
        $CommitSha = (git -C $workspace rev-parse HEAD).Trim()
    } catch {
        $CommitSha = '0000000000000000000000000000000000000000'
    }
}

$targetDir = Join-Path $workspace $OutDir
if (-not (Test-Path $targetDir)) {
    New-Item -ItemType Directory -Path $targetDir -Force | Out-Null
}

function Get-FileSha256([string]$filePath) {
    return (Get-FileHash -Path $filePath -Algorithm SHA256).Hash.ToLower()
}

$components = @(
    @{
        Name = 'contacts'
        Container = 'contacts-api'
        Type = 'api'
        DeployDir = Join-Path $workspace 'deploy/contacts'
        Digest = $ImageDigestContacts
        Contract = 'docs/api-contracts.md'
    },
    @{
        Name = 'sales'
        Container = 'sales-api'
        Type = 'api'
        DeployDir = Join-Path $workspace 'deploy/sales'
        Digest = $ImageDigestSales
        Contract = 'docs/api-contracts.md'
    },
    @{
        Name = 'web'
        Container = 'web-app'
        Type = 'web'
        DeployDir = Join-Path $workspace 'deploy/web'
        Digest = $ImageDigestWeb
        Contract = 'src/Web/package.json'
    }
)

$timestamp = [DateTime]::UtcNow.ToString('yyyy-MM-ddTHH:mm:ssZ')

foreach ($comp in $components) {
    $compName = $comp.Name
    $compDir = $comp.DeployDir
    $stageDir = Join-Path $targetDir "stage_$compName"
    if (Test-Path $stageDir) { Remove-Item -Recurse -Force $stageDir }
    New-Item -ItemType Directory -Path $stageDir -Force | Out-Null

    $fileChecksums = @{}

    # Copy deployment manifests
    Get-ChildItem -Path $compDir -File | ForEach-Object {
        $dest = Join-Path $stageDir $_.Name
        Copy-Item $_.FullName $dest -Force
        $fileChecksums[$_.Name] = Get-FileSha256 $dest
    }

    $isPublishable = ($comp.Digest -ne '' -and -not $comp.Digest.StartsWith('<PLACEHOLDER'))

    $manifest = @{
        component = $compName
        containerName = $comp.Container
        componentType = $comp.Type
        commitSha = $CommitSha
        createdAt = $timestamp
        imageDigest = $comp.Digest
        isPublishable = $isPublishable
        contractReference = $comp.Contract
        files = $fileChecksums
    }

    $manifestPath = Join-Path $stageDir 'release.json'
    [IO.File]::WriteAllText($manifestPath, ($manifest | ConvertTo-Json -Depth 5), [Text.UTF8Encoding]::new($false))
    $manifestChecksum = Get-FileSha256 $manifestPath

    # Package ZIP
    $zipPath = Join-Path $targetDir "release-$compName.zip"
    if (Test-Path $zipPath) { Remove-Item -Force $zipPath }
    Compress-Archive -Path "$stageDir\*" -DestinationPath $zipPath -Force
    Remove-Item -Recurse -Force $stageDir

    $zipChecksum = Get-FileSha256 $zipPath
    Write-Output "Artefacto generado: release-$compName.zip (SHA-256: $zipChecksum, Publishable: $isPublishable)"
}

Write-Output "Todos los artefactos de release empaquetados en $targetDir"
