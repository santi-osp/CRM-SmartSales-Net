param(
    [switch]$ConfirmAWSTeardown,
    [string]$EnvironmentName = 'smartsales-prod',
    [string]$Region = 'us-east-1'
)

$ErrorActionPreference = 'Stop'

Write-Output "=== TEARDOWN Y LIMPIEZA DE COSTOS — SMARTSALES CRM ==="

if (-not $ConfirmAWSTeardown) {
    Write-Warning @"
========================================================================
BARRERA DE SEGURIDAD ACTIVA:
No se proporcionó el parámetro explícito -ConfirmAWSTeardown.
Ningún recurso de AWS será modificado ni eliminado.

PROCEDIMIENTO DE TEARDOWN CONTROLADO (ORDEN INVERSO DE DEPENDENCIAS):
Para evitar cargos remanentes en el laboratorio de AWS Academy:
1. Eliminar stack de entrega: aws cloudformation delete-stack --stack-name ${EnvironmentName}-delivery
   (Nota: Vaciar previamente el bucket S3 de releases si contiene versiones).
2. Eliminar stack de aplicacion: aws cloudformation delete-stack --stack-name ${EnvironmentName}-app
   (Detiene tareas Fargate, elimina ALB, Target Groups y listeners).
3. Eliminar stack de datos: aws cloudformation delete-stack --stack-name ${EnvironmentName}-data
   (Toma snapshot final de RDS segun DeletionPolicy: Snapshot).
4. Eliminar stack de red: aws cloudformation delete-stack --stack-name ${EnvironmentName}-network
   (Libera subredes, tablas de ruteo, security groups y VPC).
5. Eliminar imagenes remanentes en repositorios ECR si aplica.
========================================================================
"@
    return
}

Write-Output "ALERTA: Se solicitó Teardown para el ambiente $EnvironmentName en $Region."
$confirmation = Read-Host "Escribe 'ELIMINAR' para confirmar la destrucción de recursos"
if ($confirmation -ne 'ELIMINAR') {
    Write-Output "Operación cancelada por el usuario."
    return
}

Write-Output "Iniciando proceso de limpieza autorizado..."
