param(
    [switch]$ConfirmAWSExecution,
    [string]$ServiceName = '',
    [string]$ClusterName = 'smartsales-prod-cluster',
    [string]$Region = 'us-east-1'
)

$ErrorActionPreference = 'Stop'

Write-Output "=== PROCEDIMIENTO DE ROLLBACK — SMARTSALES CRM ==="

if (-not $ConfirmAWSExecution) {
    Write-Warning @"
========================================================================
BARRERA DE SEGURIDAD ACTIVA:
No se proporcionó el parámetro explícito -ConfirmAWSExecution.
Ninguna operación de rollback en AWS será ejecutada.

PRINCIPIOS DE ROLLBACK APROBADOS (ARCH-001 v1 / ADR-09):
1. El rollback revierte tráfico y binarios de contenedor hacia la versión anterior estable.
2. NUNCA se ejecutan migraciones destructivas hacia atrás en la base de datos (DOWN / DROP).
   Todas las migraciones EF Core son estrictamente aditivas (expand/contract).
3. La versión anterior del contenedor sigue siendo compatible con el esquema de base de datos.
4. Para revertir un despliegue de ECS:
   - Si se usa CodeDeploy: aws deploy stop-deployment --deployment-id <ID> --auto-rollback-enabled
   - Si se usa ECS Circuit Breaker: el rollback ocurre automáticamente si la tarea falla su health check.
   - Si se fuerza manualmente:
     aws ecs update-service --cluster $ClusterName --service <SERVICE> --task-definition <PREVIOUS_TASKDEF>
========================================================================
"@
    return
}

if (-not $ServiceName) {
    throw "Debes indicar el parámetro -ServiceName (smartsales-prod-contacts o smartsales-prod-sales)."
}

Write-Output "Iniciando rollback autorizado para $ServiceName en cluster $ClusterName ($Region)..."
