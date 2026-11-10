param(
    [switch]$ConfirmAWSExecution,
    [string]$EnvironmentName = 'smartsales-prod',
    [string]$Region = 'us-east-1'
)

$ErrorActionPreference = 'Stop'

Write-Output "=== BOOTSTRAP DE INFRAESTRUCTURA AWS — SMARTSALES CRM ==="
Write-Output "Ambiente objetivo: $EnvironmentName"
Write-Output "Región: $Region"

if (-not $ConfirmAWSExecution) {
    Write-Warning @"
========================================================================
BARRERA DE SEGURIDAD ACTIVA:
No se proporcionó el parámetro explícito -ConfirmAWSExecution.
Este script NO realizará ninguna modificación, creación ni llamada
destructiva o facturable a AWS en esta sesión.

Para ejecutar en el futuro (cuando exista autorización explícita de AWS):
  pwsh -File deploy/aws/bootstrap.ps1 -ConfirmAWSExecution -EnvironmentName smartsales-prod

SECUENCIA DE BOOTSTRAP DOCUMENTADA:
1. Validar parámetros, cuotas y permisos efectivos de la cuenta (Gate G-02).
2. Crear stack de red: infra/cloudformation/network.yaml.
3. Crear stack de datos: infra/cloudformation/data.yaml (PostgreSQL 17 RDS).
4. Inicializar bases lógicas (contacts_db y sales_db) y roles de mínimo privilegio.
5. Ejecutar migraciones iniciales EF Core (bundle /app/migrate con rol migrador).
6. Crear stack de delivery: infra/cloudformation/delivery.yaml (ECR, S3 versionado).
7. Publicar imágenes Docker en ECR con digest inmutable (Contacts, Sales, Web).
8. Crear stack de aplicación: infra/cloudformation/application.yaml (ALB, TG IP, ECS Cluster/Servicios).
9. Configurar secreto SSM/SecretsManager para cadenas de conexión y claves JWT.
10. Validar Target Sets de Fargate y health checks (/healthz, /api/contacts/health/ready, /api/sales/health/ready).
11. Habilitar listeners y reglas por path en el ALB.
12. Ejecutar smoke tests autenticados con deploy/aws/verify.ps1.
========================================================================
"@
    return
}

# Si ConfirmAWSExecution está presente, verificar sesión
Write-Output "Comprobando credenciales autorizadas en $Region..."
$caller = aws sts get-caller-identity --output json 2>&1 | ConvertFrom-Json
if (-not $caller.Arn) {
    throw "No hay credenciales activas de AWS para ejecutar el bootstrap."
}

Write-Output "Iniciando despliegue autorizado para cuenta: $($caller.Account) con rol: $($caller.Arn)"
# Comandos de despliegue CloudFormation controlados...
