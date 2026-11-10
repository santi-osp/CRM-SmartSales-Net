# Guía de Rollback — SmartSales CRM (ARCH-001 v1)

Este documento describe la estrategia de reversión de versiones en producción ante incidencias críticas o fallos de despliegue.

---

## 1. Principio Fundamental: Migraciones Aditivas (Expand / Contract)

De acuerdo con **ADR-09**, la base de datos PostgreSQL 17 en RDS utiliza exclusivamente **migraciones aditivas**:
1. Las nuevas columnas se agregan con valores por defecto o como opcionales (`NULL`).
2. Ningún despliegue ejecuta operaciones destructivas automáticas como `DROP COLUMN` o `DROP TABLE`.
3. Esto garantiza que la versión de software anterior (N-1) continúe siendo 100% funcional y compatible con el esquema si se produce una reversión binaria.

**REGLA OBLIGATORIA:** Nunca intentar ejecutar migraciones destructivas hacia atrás (`database update <MIGRACION_PREVIA>`) en caso de rollback de aplicación. El esquema de datos se mantiene y solo se revierte la versión del contenedor.

---

## 2. Escenarios de Reversión

### Escenario A: Fallo automático durante el despliegue (Circuit Breaker)
Los servicios ECS tienen activado el `DeploymentCircuitBreaker` con `Rollback: true`.
- Si una nueva tarea Fargate no pasa su health check (`/healthz` o `/api/<servicio>/health/ready`) dentro del periodo de umbral, ECS detecta el fallo automáticamente.
- El despliegue se detiene de inmediato.
- ECS drena las tareas no saludables y restablece el `desired-count` sobre el task set anterior estable, sin requerir intervención manual.

### Escenario B: Reversión manual de emergencia tras despliegue exitoso
Si una nueva versión fue desplegada pero introduce un defecto funcional no detectado en las pruebas de humo:
1. Identificar la Task Definition previa estable (N-1):
   ```bash
   PREVIOUS_TASKDEF=$(aws ecs list-task-definitions --family-prefix smartsales-prod-sales --sort DESC --query "taskDefinitionArns[1]" --output text)
   ```
2. Ejecutar el script seguro de rollback:
   ```powershell
   pwsh -File deploy/aws/rollback.ps1 -ConfirmAWSExecution -ServiceName smartsales-prod-sales -ClusterName smartsales-prod-cluster
   ```
   O mediante AWS CLI directo:
   ```bash
   aws ecs update-service \
     --cluster smartsales-prod-cluster \
     --service smartsales-prod-sales \
     --task-definition "${PREVIOUS_TASKDEF}" \
     --region us-east-1
   ```
3. Verificar que el servicio recupere el estado saludable:
   ```powershell
   pwsh -File deploy/aws/verify.ps1 -AlbBaseUrl "http://<ALB_DNS>"
   ```
