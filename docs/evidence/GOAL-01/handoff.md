# GOAL-01 — Handoff y Estado Final

Fecha de corte: **2026-11-09 (America/Bogota)**

---

## 1. Resumen Ejecutivo

Se completó la preparación técnica local de SmartSales CRM para su futuro despliegue en AWS Academy, adaptando las 9 fases del curso a la arquitectura aprobada ARCH-001 v1. Se preservó Azure Repos como fuente Git canónica, y no se desplegó ni creó ningún recurso AWS.

**Estado final:**
- **LOCAL_PREPARATION:** PARTIAL (Docker daemon inactivo impide verificar contenedores, PostgreSQL y E2E; SDK .NET host no coincide con `global.json`).
- **AWS_DEPLOYMENT:** BLOCKED_BY_GATES (No desplegado. G-02 permisos, G-03 dominio/certificado, G-04 integración Azure Pipelines y resolución ADR-12 pendiente).

---

## 2. Cambios y archivos implementados

### Frontend Angular 22 (refinamiento visual y accesibilidad)
- `src/Web/src/index.html`: Idioma `lang="es"`, título y meta theme-color, enlace «Saltar al contenido principal».
- `src/Web/src/styles.scss`: Tokens visuales refinados, estilos de foco visible, `tabular-nums`, skip-link, respeto a `prefers-reduced-motion`.
- `src/Web/src/app/home/home.html`, `.scss`, `.ts`: Navegación Dashboard/Contactos/Oportunidades/Tablero/Agenda, rol visible, estados vacíos con acción, filtros en URL (`owner`, `from`, `to`).
- `src/Web/src/app/contacts/contacts-page.html`, `.scss`: Nav consistente, búsqueda/estados vacíos con acciones, cifras tabulares.
- `src/Web/src/app/opportunities/opportunities-page.html`, `.scss`: Nav consistente, estados vacíos con acción, cifras COP tabulares.
- `src/Web/src/app/pipeline/pipeline-page.html`, `.scss`, `.ts`: Nav consistente, mejoras de Kanban CDK y filtros compartibles mediante query params.
- `src/Web/src/app/follow-ups/follow-ups-page.html`, `.scss`: Nav consistente, fechas Bogotá tabulares, estados vacíos mejorados.
- `src/Web/src/app/interactions/interaction-panel.html`, `.scss`: Fechas tabulares y estado vacío explícito.
- `src/Web/src/app/auth/login.html`, `.scss`: Skip-link de main content y eliminación de eyebrow repetitivo.

### Infraestructura como Código (AWS CloudFormation)
- `infra/cloudformation/network.yaml`: VPC, 2 subredes públicas, 2 privadas, Internet Gateway, route tables y Security Groups restrictivos (ALB/Fargate/RDS).
- `infra/cloudformation/data.yaml`: RDS PostgreSQL 17 Single-AZ, almacenamiento gp3 cifrado, subnet group privado, `rds.force_ssl=1`, DeletionPolicy Snapshot.
- `infra/cloudformation/application.yaml`: ALB único, listeners HTTP/HTTPS, reglas por path, Target Groups tipo IP, clúster ECS Fargate, Task Definitions y servicios Contacts/Sales/Web, CloudWatch Logs.
- `infra/cloudformation/delivery.yaml`: Tres repositorios ECR inmutables, S3 versionado y CodeBuild para migraciones.
- `infra/cloudformation/parameters.example.json`: Ejemplos no reales de parámetros de stacks.

### Manifiestos y CI/CD
- `deploy/contacts/taskdef.json`, `appspec.yaml`: Task Definition y AppSpec CodeDeployToECS para Contacts.
- `deploy/sales/taskdef.json`, `appspec.yaml`: Task Definition y AppSpec CodeDeployToECS para Sales.
- `deploy/web/taskdef.json`, `imagedefinitions.json`: Task Definition e imagen para ECS estándar Web.
- `pipelines/azure-pipelines.yml`: CI de build/test + etapa de release y gate de publicación AWS.
- `pipelines/buildspec-migrations.yml`: Esqueleto de orquestación de tarea efímera de migración EF Core.
- `pipelines/release/package-release.ps1`: Generación local de 3 ZIPs de release con `release.json`, hashes SHA-256 y banderas `isPublishable=false` mientras no haya digest ECR real.

### Scripts seguros
- `deploy/aws/preflight.ps1`, `validate.ps1`: Chequeos locales, read-only por defecto.
- `deploy/aws/bootstrap.ps1`, `verify.ps1`, `rollback.ps1`, `scale.ps1`, `teardown.ps1`: Procedimientos documentados con barreras de seguridad (`-ConfirmAWSExecution` / `-ConfirmAWSTeardown` para operaciones remotas).

### Documentación
- `docs/aws/architecture.md`, `cost-estimate.md`, `resource-inventory.md`, `permissions.md`, `deploy-guide.md`, `rollback-guide.md`, `teardown-guide.md`, `course-phases-matrix.md`.
- `docs/decisions.md`: ADR-12 propuesto (No aprobado).
- `docs/aws-delivery.md`: Corrección documental de readiness (`/api/<servicio>/health/ready`).
- `docs/evidence/GOAL-01/baseline.md`, `validation.md`, `handoff.md`.

---

## 3. Matriz de las nueve fases (IMPLEMENTED / VERIFIED / PARTIAL / BLOCKED)

| Fase | Tema | Estado local | Razón |
|---|---|---|---|
| 1 | Análisis del monolito | **VERIFIED** | Se demostró equivalencia del CRM ya desacoplado; sin crear monolito artificial. |
| 2 | Entorno y control de versiones | **VERIFIED** | Azure Repos original, toolchain portable Node; SDK host pendiente ajuste. |
| 3 | Desacoplamiento de datos | **IMPLEMENTED** | Migraciones EF independientes y template RDS listos. Sin pruebas RDS reales. |
| 4 | Contenedorización | **PARTIAL** | Tres Dockerfiles presentes y compilación de código validada; Docker no permite build/Compose. |
| 5 | ECR y ECS | **IMPLEMENTED** | IaC para repositorios ECR/clúster ECS y manifiestos de task definition preparados. No publicados. |
| 6 | ALB y Target Groups | **BLOCKED** | IaC preliminar para un ALB con rutas por path; conflicto de CodeDeploy en listener compartido requiere decisión ADR-12. |
| 7 | Servicios ECS Fargate/RDS | **IMPLEMENTED** | Plantillas CloudFormation y scripts de bootstrap; no desplegados. |
| 8 | CI/CD Azure→AWS | **PARTIAL** | YAML de Azure Pipelines y buildspec preparados como punto de partida; faltan G-04 y decisión de controlador de despliegue. |
| 9 | Actualización, rollback y escalado | **PARTIAL** | Scripts y guías preparados; pruebas reales AWS pendientes. |

---

## 4. Problemas abiertos priorizados por severidad

| Severidad | Problema | Bloqueo / Decisión pendiente |
|---|---|---|
| **CRÍTICO** | CodeDeploy tradicional gestiona listeners completos; dos deployment groups sobre HTTPS 443 compartido no garantizan aislamiento de reglas `/api/contacts/*` y `/api/sales/*`. | **ADR-12 pendiente:** Elegir controlador ECS nativo (recomendado) o modificación de topología con listeners/ALBs dedicados. No desplegar con la topología ambigua. |
| **ALTO** | Docker Desktop daemon inactivo en el host. | Abrir Docker Desktop (sin borrar volúmenes ni bases ajenas) y ejecutar tests de integración, Compose y Playwright. |
| **ALTO** | Host tiene SDK .NET 10.0.204, pero `global.json` exige 10.0.300. | Instalar SDK 10.0.300 localmente con permiso del propietario o usar entorno portable compatible. |
| **MEDIO** | Falta sesión de AWS Academy autorizada. | G-02, G-03 y G-04 deben resolverse antes de cualquier operación remota. |
| **MEDIO** | Frontend refinado pero sin medición dinámica de contraste o test E2E de drag real. | Ejecutar Playwright E2E y auditoría WCAG al disponer del daemon Docker y backend levantado. |

---

## 5. Skills utilizadas

- `frontend-design` (Anthropic) instalada localmente en `.opencode/skills/frontend-design/`.
- `web-design-guidelines` (Vercel Labs) instalada localmente en `.opencode/skills/web-design-guidelines/`.
- Aplicadas para definir jerarquía de información, paleta, accesibilidad, navegación, tipografía y estados útiles en Angular 22.

---

## 6. Recursos AWS requeridos y costos estimados

- **Recursos:** VPC, Internet Gateway, 2 subredes públicas (ALB/Fargate), 2 subredes privadas (RDS), 3 Security Groups, un ALB con listeners 80/443 y 5 Target Groups IP, 1 ECS Cluster y 3 servicios Fargate, 1 RDS PostgreSQL 17 Single-AZ 20 GiB gp3, 3 repositorios ECR, S3 versionado, CodeBuild migrador, CloudWatch Logs, SSM SecureString y certificado ACM.
- **Estimación región us-east-1:** ~$0.0984 USD/hora, ~$0.39 USD por sesión de 4 horas, ~$75.06 USD/mes continuo. Ver [cost-estimate.md](../../aws/cost-estimate.md) para supuestos y desglose.
- **Permisos:** `ecr:*` limitado, `ecs:RunTask/DescribeTasks`, `iam:PassRole`, `logs:*`, `ssm:GetParameter`, `s3:PutObject`, `codepipeline:StartPipelineExecution`, `cloudformation:*` según rol. Ver [permissions.md](../../aws/permissions.md). No se asumió que `LabRole` o `voclabs` estén habilitados.

---

## 7. Comandos de preparación y verificación local

```powershell
# Preflight local sin AWS
pwsh -File deploy/aws/preflight.ps1

# Validación sintáctica CloudFormation y JSON
pwsh -File deploy/aws/validate.ps1

# Generar ZIPs de release no publicables (sin digest ECR real)
pwsh -File pipelines/release/package-release.ps1

# Build/Test Angular 22
& '.tools/node-v24.22.1-win-x64/node.exe' 'src/Web/node_modules/@angular/cli/bin/ng.js' build
& '.tools/node-v24.22.1-win-x64/node.exe' 'src/Web/node_modules/@angular/cli/bin/ng.js' test --watch=false
```

---

## 8. Orden exacto del futuro despliegue AWS (requiere autorización posterior)

1. Confirmar ADR-12 aprobada por el propietario (controlador de despliegue y topología).
2. Resolver G-02 (permisos AWS efectivos, cuotas, región, límites de sesión).
3. Resolver G-03 (dominio y certificado ACM HTTPS activo).
4. Resolver G-04 (Azure Pipelines minutos, credenciales temporales o federación aprobada).
5. Validar CloudFormation con CLI en modo lectura antes de crear change sets.
6. Crear stack de red `network.yaml`.
7. Crear stack de datos `data.yaml` con contraseña maestra fuera del repositorio.
8. Inicializar `contacts_db` y `sales_db` con usuarios runtime y migradores separados.
9. Ejecutar bundles EF Core migrations con task one-off.
10. Crear stack de delivery `delivery.yaml` (ECR, S3 versionado).
11. Publicar imágenes ECR por commit SHA y capturar digest inmutable.
12. Crear stack de aplicación `application.yaml` con ACM y TaskDefinitions reales.
13. Verificar health checks de ALB y rutas públicas HTTPS.
14. Activar Azure Pipelines → ECR/S3 → CodePipeline (tras ADR-12).
15. Ejecutar smoke tests autenticados y documentar resultados HU11/HU12/HU13.
16. Demostrar escalado 1→2→1, rollback binario y persistencia RDS.

---

## 9. Procedimiento de rollback y limpieza

- **Rollback:** Revertir tráfico/binarios; nunca ejecutar migración `DOWN` sobre la base RDS. Consultar [rollback-guide.md](../../aws/rollback-guide.md) y `deploy/aws/rollback.ps1` (modo read-only por defecto).
- **Limpieza:** Eliminar stacks en orden inverso `delivery → app → data → network`; `data.yaml` retiene snapshot final de RDS por defecto. Consultar [teardown-guide.md](../../aws/teardown-guide.md) y `deploy/aws/teardown.ps1` (requiere doble confirmación).

---

## 10. Evidencia de ausencia de operaciones AWS de escritura

Durante GOAL-01:
- No se ejecutó `aws cloudformation create-stack`, `aws ecr`, `aws s3 cp`, `aws ecs update-service`, `aws deploy`, `aws codepipeline start-pipeline-execution`, ni comandos equivalentes de escritura remota.
- Se ejecutaron exclusivamente comandos de creación de archivos locales, compilación/test del frontend Angular y scripts PowerShell de preflight/validación local con barreras de seguridad activas.
- No se generó commit/push, no se modificó Azure Boards ni credenciales.

---

## 11. Acciones reservadas al propietario

1. Aprobar o rechazar ADR-12 (decisión sobre CodeDeploy tradicional vs ECS nativo con Circuit Breaker).
2. Autorizar explícitamente acceso y despliegue en AWS Academy, una vez resueltos G-02, G-03 y G-04.
3. Instalar .NET SDK 10.0.300 en el host o facilitar toolchain portable compatible.
4. Abrir Docker Desktop cuando sea seguro, conservando volúmenes y bases de desarrollo existentes.
5. Proporcionar dominio controlado y certificado ACM para HTTPS público (G-03).
6. Proporcionar credenciales renovables/federación autorizada para Azure Pipelines si la política del laboratorio lo permite (G-04).

---

## Estados separados

**LOCAL_PREPARATION: PARTIAL** (Infraestructura definida localmente, frontend refinado; verificación de contenedores y E2E bloqueada por entorno).

**AWS_DEPLOYMENT: BLOCKED_BY_GATES** (Sin despliegue, recursos AWS no creados; G-02, G-03, G-04 y ADR-12 pendientes).
