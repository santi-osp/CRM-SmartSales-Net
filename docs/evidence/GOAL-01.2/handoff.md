# GOAL-01.2 — Handoff y Estado Final

Fecha: **2026-11-09 (America/Bogota)**  
Ruta de ejecución: **OpenCode Auto Dev** con planificación obligatoria de `goal-architect`.

---

## 1. Resumen Ejecutivo

Se implementó el perfil de demostración **Showcase** en SmartSales CRM, ampliando los seeders existentes de Contacts y Sales para proveer un dataset comercial ficticio abundante, coherente e idempotente para demostraciones de 7 a 10 minutos, sin introducir acoplamiento entre servicios ni dependencias externas.

```
========================================================================================
ESTADOS FINALES DE GOAL 01.2:
  A. LOCAL_IMPLEMENTATION: READY    (Código de seeders, perfiles, scripts y guion listos)
  B. LOCAL_VERIFICATION:   PARTIAL  (12/12 tests puros PASS; Docker daemon inactivo)
  C. AWS_DEMO_SEED:        NOT_RUN  (Sin ejecución remota ni publicación en AWS)
========================================================================================
```

---

## 2. Conteos y Distribución: Esperados vs Implementados (Showcase)

| Recurso | Perfil Basic (Original) | Perfil Showcase (Objetivo) | Implementado y Validado | Distribución Específica |
|---|---:|---:|---:|---|
| **Usuarios Demo** | 2 | 4 | **4** (100%) | 1 Administrador (`...0001`), 3 Comerciales (`...0002`, `...0003`, `...0004`). |
| **Contactos** | 8 | 60 | **60** (100%) | 8 originales + 52 empresas B2B colombianas ficticias. Tipos Prospect/Customer y fuentes Web/Referral/WhatsApp/Call/Other. 100% emails únicos en `.example`. |
| **Oportunidades** | 8 | 100 | **100** (100%) | **25 New, 25 Contacted, 25 Proposal, 15 Won, 10 Lost**. Montos entre 1.4M y 65M COP, prioridades High/Medium/Low, cierres validados. |
| **Seguimientos** | 3 | 120 | **120** (100%) | **30 Overdue, 30 Today, 30 Upcoming, 30 Completed** en instante de siembra. Ningún pendiente asociado a oportunidad cerrada. FK compuesta validada. |
| **Interacciones** | 3 | 150 | **150** (100%) | **50 Call, 50 Email, 50 Meeting**. Fechas pasadas (`OccurredAt <= now`). FK compuesta validada. |

---

## 3. Garantías de Idempotencia y Aislamiento

1. **Insert-only por ID:** La verificación se realiza por clave primaria GUID. Si el ID ya existe en la base de datos, se omite la inserción.
2. **Preservación de ediciones del usuario:** Se eliminó la verificación rígida de campos editables como email o nombre en la repetición del seed. Si durante una demo el usuario edita `Café Aurora` o completa un seguimiento, volver a ejecutar el seeder **no revierte ni sobrescribe sus cambios**.
3. **Sin consultas cruzadas:** Contacts y Sales no comparten DbContext, conexiones ni tablas. Los identificadores de contactos se comparten como valores lógicos (`Guid`), manteniendo estricta la autonomía de microservicios de ARCH-001.

---

## 4. Archivos Creados y Modificados

- `src/Contacts/SmartSales.Contacts.Api/Infrastructure/Persistence/DemoSeedProfile.cs`: Creado (validador de perfiles `Basic`/`Showcase` y cálculo de fecha de referencia).
- `src/Contacts/SmartSales.Contacts.Api/Infrastructure/Persistence/UserSeeder.cs`: Modificado (soporte para 4 cuentas en Showcase con contraseñas externas seguras).
- `src/Contacts/SmartSales.Contacts.Api/Infrastructure/Persistence/DemoContactsSeeder.cs`: Modificado (catálogo de 60 contactos B2B colombianos idempotentes).
- `src/Sales/SmartSales.Sales.Api/Infrastructure/Persistence/DemoSeedProfile.cs`: Creado (validador de perfiles en Sales).
- `src/Sales/SmartSales.Sales.Api/Infrastructure/Persistence/DemoSalesSeeder.cs`: Modificado (generadores modulares de 100 oportunidades, 120 seguimientos y 150 interacciones con integridad de FKs).
- `deploy/local/run.ps1`: Modificado (soporte de parámetro `-DemoProfile Basic|Showcase`, generación de credenciales locales en `.tools/local/secrets.json` sin rotar existentes).
- `compose.yaml`: Modificado (inyección de `DEMO_PROFILE` y cuentas adicionales).
- `tests/Contacts.IntegrationTests/DemoContactsSeederTests.cs`: Creado (3 pruebas unitarias de catálogo de contactos).
- `tests/Contacts.IntegrationTests/UserSeederTests.cs`: Creado (2 pruebas unitarias de cuentas e identidades).
- `tests/Sales.IntegrationTests/DemoSalesCatalogTests.cs`: Creado (7 pruebas unitarias de oportunidades, etapas, seguimientos, interacciones, bordes de medianoche y perfiles).
- `docs/demo-data.md`: Modificado (documentación completa de perfiles, tablas, queries de verificación y guía ECS).
- `docs/demo/showcase-script.md`: Creado (guion paso a paso de 7 a 10 minutos para demostración comercial y técnica).
- `docs/evidence/GOAL-01.2/`: Creados `baseline.md`, `validation.md` y `handoff.md`.

---

## 5. Instrucciones de Uso Local y Futuro ECS

### Uso Local:
```powershell
# Ejecutar perfil Showcase (sin borrar volúmenes existentes)
./deploy/local/run.ps1 -DemoProfile Showcase

# Ejecutar perfil Basic predeterminado
./deploy/local/run.ps1 -DemoProfile Basic
```

### Futuro ECS Fargate (Solo tras autorización explícita de AWS):
Ejecutar tarea independiente one-off para Contacts (`--seed-users`, luego `--seed-demo`) y Sales (`--seed-demo`) pasando `Demo__Enabled=true` y `Demo__Profile=Showcase` con credenciales de `SSM SecureString`.

## 6. Conclusión de revisión independiente

`goal-reviewer` encontró dos defectos en la primera versión: seguimientos pendientes vinculados a oportunidades cerradas tras un rerun y clasificación «Hoy» incorrecta al sembrar cerca de medianoche. Se corrigieron y se agregaron tests de regresión. Las garantías sobre filas realmente persistidas e idempotencia tras doble/triple ejecución permanecen **no verificadas** sin PostgreSQL 17; no se afirmó PASS integrado. El código local está preparado pero la verificación de datos reales sigue PARTIAL.
