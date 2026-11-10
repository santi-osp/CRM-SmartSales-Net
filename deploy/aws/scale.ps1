param(
    [switch]$ConfirmAWSExecution,
    [string]$ServiceName = '',
    [int]$DesiredCount = 2,
    [string]$ClusterName = 'smartsales-prod-cluster',
    [string]$Region = 'us-east-1'
)

$ErrorActionPreference = 'Stop'

Write-Output "=== ESCALADO INDEPENDIENTE DE SERVICIOS — SMARTSALES CRM ==="

if (-not $ConfirmAWSExecution) {
    Write-Warning @"
========================================================================
BARRERA DE SEGURIDAD ACTIVA:
No se proporcionó el parámetro explícito -ConfirmAWSExecution.
Ninguna operación de escalado en AWS será ejecutada.

PRINCIPIO DE ESCALABILIDAD (ARCH-001 v1 / NF-01 / HU11):
- Contacts API y Sales API se escalan de forma completamente independiente.
- Cada tarea Fargate ejecuta en su propio contenedor (0.25 vCPU / 512 MiB).
- Escalar Sales API (p. ej. ante alta demanda de Kanban o Dashboard) no requiere
  escalar Contacts API ni Web Nginx.
- Comando para ejecución futura autorizada:
  aws ecs update-service --cluster $ClusterName --service $ServiceName --desired-count $DesiredCount --region $Region
========================================================================
"@
    return
}

if (-not $ServiceName) {
    throw "Debes indicar el parámetro -ServiceName (smartsales-prod-contacts o smartsales-prod-sales)."
}

Write-Output "Ajustando desired-count de $ServiceName a $DesiredCount en $ClusterName..."
aws ecs update-service --cluster $ClusterName --service $ServiceName --desired-count $DesiredCount --region $Region
Write-Output "Comando de escalado emitido."
