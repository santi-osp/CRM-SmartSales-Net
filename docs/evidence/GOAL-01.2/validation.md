# GOAL-01.2 — Registro de Validación Técnica y Pruebas

Fecha: **2026-11-09, America/Bogota**  
Ejecución: Auto Dev / OpenCode  

---

## 1. Pruebas Automatizadas de Catálogo y Seeders (12/12 PASS)

| Suite de Pruebas | Casos Ejecutados | Resultado | Verificación Observable |
|---|---|---|---|
| `DemoContactsSeederTests` | 3 | **PASS** | - Basic: 8 contactos originales intactos.<br>- Showcase: 60 contactos B2B colombianos, 100% emails únicos en dominio `.example`, tipos (`Prospect`/`Customer`) y fuentes válidas.<br>- Validación opt-in: falla si `Demo:Enabled=false`. |
| `UserSeederTests` | 2 | **PASS** | - 4 identidades deterministas estables (`019a6010-...-0001` a `0004`).<br>- Validación de seguridad: rechaza contraseñas menores a 14 caracteres. |
| `DemoSalesCatalogTests` | 7 | **PASS** | - Basic: 8 oportunidades originales con etapas y montos intactos.<br>- Showcase Oportunidades: **100 oportunidades exactas** con distribución precisa: **25 New, 25 Contacted, 25 Proposal, 15 Won, 10 Lost**. Reglas de cierre (Won: `FinalAmount >= 0`, `ClosedAt != null`, `LossReason == null`; Lost: `LossReason != null`, `FinalAmount == null`).<br>- Showcase Seguimientos: **120 registros exactos** distribuidos en **30 Overdue, 30 Today, 30 Upcoming, 30 Completed** al sembrar. FK compuesta `(OpportunityId, ContactId)` verificada. Regla de negocio: ningún seguimiento pendiente vinculado a oportunidad cerrada, incluso en un rerun después de cierre de oportunidad. Distribución Today verificada también a las 23:55 hora Bogotá.<br>- Showcase Interacciones: **150 registros exactos** (50 Call, 50 Email, 50 Meeting), fechas pasadas (`OccurredAt <= now`) y FK compuesta validada.<br>- Validador de perfil: rechaza perfiles inválidos o banderas desactivadas. |

---

## 2. Pruebas de Frontend (Angular 22 + Vitest)

| Componente | Comando | Resultado | Tiempo / Detalles |
|---|---|---|---|
| **Compilación Angular** | `ng build` (Node portable 24.22.1) | **PASS** | Compiló en 24.0s. Bundle inicial: 485.00 kB. Chunks lazy para pipeline, opportunities, follow-ups, contacts y home. |
| **Pruebas Unitarias Web** | `ng test --watch=false` (Vitest 4.1.12) | **PASS** | **5/5 tests pasaron** en 12.95s (Dashboard, Contactos, Pipeline, Follow-ups, Auth). |

---

## 3. Estado de Pruebas Dependientes de Entorno

| Prueba | Comando | Estado | Causa del Bloqueo |
|---|---|---|---|
| **Tests PostgreSQL Testcontainers** | `dotnet test SmartSales.slnx` (IntegrationHost) | **BLOCKED** | Daemon de Docker Desktop inactivo en Windows (`//./pipe/dockerDesktopLinuxEngine` no encontrado). |
| **Playwright E2E** | `tests/e2e/run.ps1` | **BLOCKED** | Requiere contenedor PostgreSQL temporal vía Docker daemon. |
| **Docker Compose Local** | `deploy/local/run.ps1 -DemoProfile Showcase` | **BLOCKED** | Requiere Docker daemon para build y ejecución de contenedores. |

*Nota:* No se simularon salidas exitosas para pruebas bloqueadas. Se mantuvo `global.json` restaurado en `10.0.300 / latestPatch` sin diff residual.

## 4. Revisión independiente (`goal-reviewer`)

Se solicitó revisión de integridad/idempotencia/seguridad. El revisor detectó dos fallos: (P1) seguimientos pendientes vinculados a oportunidades que habían sido cerradas por el usuario entre ejecuciones, y (P2) categoría «Hoy» calculada con fecha fija que podía estar vencida cerca de medianoche. Ambos fueron corregidos y se agregaron dos tests unitarios específicos. El resto de las garantías sobre conteos persistidos e idempotencia siguen pendientes de validación PostgreSQL real (BLOCKED por Docker).

