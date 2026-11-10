# GOAL-01 — Registro de Pruebas y Validación Técnica

Fecha: **2026-11-09, America/Bogota**.
Entorno de ejecución: Windows 11 x64, Node portable 24.22.1, .NET SDK 10.0.204 / 10.0.300, PowerShell Core 7.

---

## 1. Batería de Pruebas Ejecutadas Localmente

| Categoría | Comando / Prueba | Resultado | Detalle y Evidencia |
|---|---|---|---|
| **Frontend Build** | `ng build` (Node portable 24.22.1) | **PASS** | Bundle compilado en 6.47s. Initial chunk total: 485.00 kB (Raw) / 112.95 kB (Transferencia estimada). Chunks lazy generados para pipeline, opportunities, follow-ups, contacts y home. |
| **Frontend Unit Tests**| `ng test --watch=false` (Vitest 4.1.12) | **PASS** | **5/5 tests pasaron** (100% éxito en 392ms de ejecución). Validó Dashboard, Contacts, Pipeline y formatters. |
| **Validación CloudFormation**| `pwsh -File deploy/aws/validate.ps1` | **PASS** | **4/4 plantillas YAML validadas** sintácticamente (`network.yaml`, `data.yaml`, `application.yaml`, `delivery.yaml`). Cabeceras y recursos conformes. |
| **Validación JSON Manifiestos**| `pwsh -File deploy/aws/validate.ps1` | **PASS** | **5/5 archivos JSON validados**: `deploy/contacts/taskdef.json`, `deploy/sales/taskdef.json`, `deploy/web/taskdef.json`, `deploy/web/imagedefinitions.json`, `parameters.example.json`. |
| **Preflight Check** | `pwsh -File deploy/aws/preflight.ps1` | **PASS** | 5/5 comprobaciones completadas. Detectó estado de Git, toolchain local, estado del daemon Docker y existencia de todos los artefactos requeridos. |
| **Barreras de Seguridad**| `bootstrap.ps1`, `rollback.ps1`, `scale.ps1`, `teardown.ps1` | **PASS** | Todos los scripts mutantes **rechazaron ejecución** por defecto al invocar sin `-ConfirmAWSExecution`, emitiendo advertencia de seguridad y abortando sin efectos secundarios. |
| **Empaquetado de Release**| `pwsh -File pipelines/release/package-release.ps1` | **PASS** | Generó 3 paquetes ZIP inmutables en `artifacts/releases/` con sus SHA-256 calculados (`release-contacts.zip`, `release-sales.zip`, `release-web.zip`), marcados como `isPublishable: false` debido a placeholders de digest. |
| **Backend Build (.NET)**| `dotnet build SmartSales.slnx --configuration Release` | **PASS** (Verificado en GOAL-00) | 0 errores, 0 advertencias en Release. |
| **Backend Integration Tests**| `dotnet test SmartSales.slnx` | **BLOCKED (Entorno)** | 0/7 tests (Contacts 0/3, Sales 0/4). Bloqueado por ausencia de daemon de Docker Desktop (`DockerUnavailableException`). |
| **Playwright E2E** | `tests/e2e/run.ps1` | **BLOCKED (Entorno)** | Bloqueado porque requiere iniciar un contenedor PostgreSQL temporal vía `docker run`. |
| **Escaneo de Secretos** | Búsqueda regex en `infra/`, `deploy/`, `pipelines/` | **PASS** | **0 secretos expuestos**, 0 contraseñas o tokens hardcodeados en los archivos preparados. |

---

## 2. Hallazgos y Correcciones Aplicadas Durante la Validación

1. **Corrección de tipado en plantilla Angular (`home.html`):**
   - El compilador de Angular detectó: `TS2367: This comparison appears to be unintentional because the types 'Role | undefined' and '"ADMIN"' have no overlap.`
   - Se corrigió la comparación al tipo exacto del modelo (`Role = 'Administrator' | 'Commercial'`), restaurando la compilación limpia.
2. **Reconciliación de Rutas de Readiness:**
   - Se actualizó `docs/aws-delivery.md:29` para que coincida exactamente con la ruta real implementada en Kestrel (`/api/<servicio>/health/ready`).
3. **Bloqueo Arquitectónico de CodeDeploy Identificado:**
   - Se formuló formalmente **ADR-12** en `docs/decisions.md` documentando la limitación de CodeDeploy tradicional sobre listeners compartidos y las alternativas técnicas viables (ECS native circuit breaker vs puertos/ALBs dedicados).
