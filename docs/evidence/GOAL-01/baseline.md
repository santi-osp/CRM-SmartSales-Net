# GOAL-01 — Baseline de Partida

Fecha: **2026-11-09, America/Bogota**.
Repositorio: `CRM SmartSales .Net` en rama `master` (`origin/master` en Azure Repos, commit `011c57b`).
Arquitectura vinculante: **ARCH-001 v1** (ADR-01 a ADR-11, P-01 a P-08 aprobadas).

---

## 1. Estado al inicio de la sesión

1. **Backend:**
   - Dos microservicios ASP.NET Core 10 (`Contacts API` y `Sales API`) independientes en `src/Contacts` y `src/Sales`.
   - Migraciones EF Core independientes (`InitialUsers`, `AddContacts`, `InitialSales`, `AddFollowUps`, `AddInteractions`).
   - Herramientas locales: `global.json` especifica .NET SDK `10.0.300` con `latestPatch`; host dispone de `10.0.204`.
2. **Frontend:**
   - Angular 22.2.5 con CLI/Material/CDK 22.2.6 y TypeScript 6.0.2.
   - Node portable v24.22.1 descargado y verificado en `.tools/` (ignorado por Git).
   - Componentes funcionales pero con inconsistencias visuales detectadas en GOAL-00:
     - Atributo `lang="en"` en `index.html`.
     - Falta de skip-link accesible para navegación por teclado.
     - Cliché repetitivo de etiquetas en mayúsculas sostenidas (`.eyebrow`).
     - Barras de navegación superior desalineadas entre vistas.
     - Cifras monetarias COP y fechas sin formato `tabular-nums`.
     - Estados vacíos sin llamadas a la acción (HU08-AC02).
3. **Entorno Docker:**
   - Daemon de Docker Desktop inactivo en el host (`//./pipe/dockerDesktopLinuxEngine` no encontrado).
   - Pruebas de integración Testcontainers C# y E2E Playwright bloqueadas por falta de daemon.
4. **Infraestructura AWS:**
   - No existía código de infraestructura ejecutable (`infra/` inexistente).
   - No existían manifiestos de tarea ni de despliegue (`deploy/contacts`, `deploy/sales`, `deploy/web` inexistentes).
   - No existían canalizaciones de CI/CD (`pipelines/` inexistente).
   - Discrepancia documental: `docs/aws-delivery.md:29` especificaba `/api/<servicio>/v1/health/ready` mientras el código real implementa `/api/<servicio>/health/ready`.
