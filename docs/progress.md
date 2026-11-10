# Progreso real — SmartSales

Fecha de corte: **2026-11-08, America/Bogota**. [ARCH-001 v1](approval.md) y el [roadmap](implementation-plan.md) aprobados. El usuario indicó que el entregable debe desplegar **su CRM en microservicios**; la guía AWS Academy es referencia técnica, sin línea base monolítica ni prioridad en capturas o presentación. [Alcance AWS](source/lab-guide-summary.md).

## Estado de trabajo

| HU | Azure Boards | Implementación y verificación |
|---|---|---|
| HU01 #129 | Active | Auth, roles y sesión Angular; E2E comprueba login real y rechazo 403 de reasignación Comercial |
| HU02 #132 | Active | CRUD y búsqueda de contactos; E2E crea uno y lo usa en Sales, con actividad asociada |
| HU03 #135 | Active | Oportunidades, referencias HTTP, permisos y auditoría; E2E crea y reasigna con ambos servicios |
| HU04 #138 | Active | Etapas y cierre; E2E mueve, recarga, cierra y observa dashboard; faltan casos visuales de drag/drop |
| HU05 #142 | Active | Agenda y seguimiento; E2E crea, clasifica y completa desde navegador |
| HU06 #145 | Active | Actividad compartida; E2E registra una interacción y consulta la demo en ambas fichas |
| HU07 #148 | Active | Dashboard real; E2E comprueba cambio de ganadas tras cierre |
| HU08 #151 | Active | Seeds idempotentes y UI; E2E en Edge a 1440/390 px sin desbordamiento global; falta auditoría de contraste |
| HU09 #154 | Active | Límites y migraciones locales de ambas APIs; faltan RDS y aislamiento en AWS |
| HU10 #158 | New | Dockerfiles/Compose en construcción y validación local; ECR pendiente |
| HU11–HU13 | New | AWS y entrega pendientes; HU13 se centrará en demostración funcional/técnica |
| HU14–HU15 | Removed | Fuera del MVP |

No cerrar HU01–HU04 en Boards hasta comprobar cada criterio con el recorrido integrado y registrar el resultado. Los commits iniciales segmentados y el push a origin/master fueron autorizados por el usuario y completados el 2026-11-08.

## Implementado

- `SmartSales.slnx`: Contacts API y Sales API .NET 10 independientes, EF Core 10/Npgsql 10, PostgreSQL 17, migraciones `InitialUsers`, `AddContacts` e `InitialSales`, pruebas de integración.
- Contacts: login y `/me`, seed idempotente de Administrador/Comercial con secretos externos, contactos y directorio HTTP para Sales. Sin credenciales en repositorio.
- Sales: oportunidades abiertas/cerradas, validación de contacto y responsable activo en Contacts por HTTP, reasignación solo Admin con auditoría, filtros, versiones optimistas, transición de etapas y cierre Won/Lost con autor y fecha.
- Angular 22: login, contactos, oportunidades y tablero en rutas diferidas; formularios reactivos y estado local. El tablero recarga el estado del servidor tras éxito o error de movimiento/cierre.
- HU05: `AddFollowUps` agrega `follow_ups` con FK compuesta a oportunidades. API clasifica Vencidos/Hoy/Próximos/Completados con reloj único y límites de Bogotá; acceso propio/Admin, cierre idempotente. Angular agenda convierte hora local Bogotá a UTC y consulta una categoría a la vez.
- HU06: `AddInteractions` agrega `interactions` con FK compuesta opcional; registra llamada/correo/reunión ya realizada, autor y resumen. El mismo id aparece por contacto y por oportunidad. Un panel Angular compartido consulta actividad de Sales dentro de ambas fichas y deja visible la ficha si falla esa consulta.
- HU07: Dashboard Sales agrupa en una sola consulta por etapa y calcula abiertos/ganados/perdidos y monto estimado abierto sobre una misma cohorte. Filtros por responsable y fecha de creación en Bogotá; Angular muestra tarjetas y tabla de barras con ceros explícitos.
- HU08: barra superior y enlaces a Dashboard/Contactos/Oportunidades/Tablero/Agenda; tablero de tres columnas con scroll horizontal dentro de su contenedor a ancho móvil y menú de acciones por teclado. Seeds `--seed-demo` en Contacts/Sales bajo `Demo:Enabled=true`, IDs fijos y filas ficticias descritas en [datos demo](demo-data.md).
- Toolchain fijado en [lock](toolchain-lock.md); Node 24.22.1 local ignorado por Git. Docker Desktop disponible para Testcontainers.

## Verificación ejecutada

- `dotnet test SmartSales.slnx --configuration Release --no-restore`: pasó conjuntamente, Contacts 2/2 y Sales 1/1 contra PostgreSQL 17; compiló ambas APIs y tests sin advertencias. La primera repetición dentro del sandbox falló por acceso denegado al pipe de Docker, sin fallo de aplicación; la ejecución con acceso al daemon pasó.
- `npm --prefix src/Web run build`: pasó con chunk inicial de 477.22 kB y tablero en carga diferida.
- `dotnet test tests/Sales.IntegrationTests/SmartSales.Sales.IntegrationTests.csproj --configuration Release --no-restore`: 2/2 tras HU05, PostgreSQL 17; límites de día, permisos Mine/All, relación contacto/oportunidad, finalización idempotente y persistencia.
- `dotnet test tests/Sales.IntegrationTests/SmartSales.Sales.IntegrationTests.csproj --configuration Release --no-restore`: 3/3 tras HU06, PostgreSQL 17; añade fecha futura, resumen vacío, relación incorrecta, directorio 503 sin escritura e id compartido en dos consultas.
- `npm --prefix src/Web test -- --watch=false`: 4/4 tras HU06; sesión, recuperación del tablero, conversión Bogotá→UTC/alcance y fallo parcial de actividad. `npm run build` pasó con historial compartido en chunks diferidos.
- `dotnet test tests/Sales.IntegrationTests/SmartSales.Sales.IntegrationTests.csproj --configuration Release --no-restore`: 4/4 tras HU07, PostgreSQL 17; dataset de cinco etapas, límites de día Bogotá, filtro intersección, ceros y rango invertido.
- `npm --prefix src/Web test -- --watch=false`: 5/5 tras HU07; añade aplicar/limpiar filtros y rechazo local de rango invertido. `npm run build` pasó con dashboard diferido e inicial de 476.09 kB.
- `dotnet test SmartSales.slnx --configuration Release --no-restore`: 3/3 Contacts y 4/4 Sales tras HU08 contra PostgreSQL 17. Ejecutar cada seed dos veces conservó 8 contactos, 8 oportunidades demo, 3 seguimientos y 3 interacciones, sin duplicados.
- `npm run build`: pasó tras HU08 con chunk inicial de 476.09 kB. `npm test -- --watch=false`: 5/5. Después se comprobó el flujo en Edge a 1440/390 px; falta auditoría visual y contraste medido.
- `powershell -NoProfile -ExecutionPolicy Bypass -File tests/e2e/run.ps1`: **3/3 en Edge** con PostgreSQL 17 desechable, ambas APIs y Angular reales. Recorre login, contactos, actividad, oportunidad, reasignación, etapas, cierre, dashboard, agenda y permiso Comercial; verifica diseño a 1440/390 px. RDS, ECR, ECS y canalización siguen sin ejecutar.

## Criterios y límites pendientes

- HU01-AC03 y HU03: token real Contacts→Sales y 403 Comercial comprobados en E2E; falta revisar cada criterio restante antes de cerrar en Boards.
- HU02-AC03: contacto creado se referencia desde Sales; falta edición posterior del contacto con relación conservada.
- HU04-AC01/02/03/04: se comprobó movimiento por menú de teclado, recarga, cierre y dashboard; falta gesto de arrastre real y revisión de estados de error en navegador.
- HU05/HU06: se comprobó alta y completado de seguimiento, registro de actividad y lectura de actividad demo en ambas fichas; falta comprobar la misma interacción recién creada en la ficha de oportunidad.
- HU07: se comprobó indicador tras cierre; faltan filtros combinados en navegador, aunque API/Angular tienen pruebas propias.
- HU08: faltan contraste medido y revisión visual fina; seeds y anchuras 1440/390 comprobados.
- HU09-AC01/02: faltan contenedores independientes y RDS con bases/usuarios aislados. G-02 permisos AWS, G-03 URL HTTPS y G-04 credenciales de pipeline se verifican antes de sus fases.

## GOAL-00 — baseline local (2026-11-09, America/Bogota)

[Reporte y matriz de resultados](evidence/GOAL-00/baseline.md). En esta sesión, sin cerrar HU ni actualizar Boards: skills locales `frontend-design` y `web-design-guidelines` cargadas; restore/build .NET pasaron con SDK 10.0.204 tras un ajuste **temporal** de `global.json` (se restauró al valor original 10.0.300); tests de integración ejecutados, 0/7 por daemon Docker no disponible. Angular 22.2.x ya estaba bloqueado; Node portable 24.22.1 se descargó con SHA256 verificado y quedó ignorado en `.tools/`; build pasó y Vitest 5/5. `npm audit`: 0 avisos. Docker Compose y E2E integrados no se ejecutaron por daemon ausente. Ninguno de estos resultados suplanta la evidencia histórica; hay discrepancias de rutas y estado documental detalladas en el reporte. Se preservó `.serena/` ajeno y no se modificó código funcional.

## GOAL-01 — Preparación técnica de las 9 fases AWS Academy (2026-11-09, America/Bogota)

[Reporte y handoff completo](evidence/GOAL-01/handoff.md) y [evidencia de validación](evidence/GOAL-01/validation.md). En esta sesión, sin desplegar en AWS, sin commit/push y sin modificar Azure Boards:
- **Frontend Angular 22:** Refinado con `frontend-design` y `web-design-guidelines`. Shell unificado y navegación compartida (Dashboard, Contactos, Oportunidades, Tablero, Agenda), idioma `lang="es"`, skip-link para teclado, cifras COP y fechas tabulares (`tabular-nums`), estados vacíos con acciones útiles (HU08-AC02), sincronización de filtros con query params en URL. Compilación Angular en 6.47s y Vitest 5/5 **PASS**.
- **IaC CloudFormation:** 4 plantillas modulares creadas en `infra/cloudformation/` (`network.yaml`, `data.yaml`, `application.yaml`, `delivery.yaml`, `parameters.example.json`). Validación sintáctica **PASS**.
- **Despliegue y CI/CD:** Manifiestos de tarea y AppSpec preparados en `deploy/contacts/`, `deploy/sales/`, `deploy/web/`. Pipeline YAML en `pipelines/azure-pipelines.yml`, migrador EF en `pipelines/buildspec-migrations.yml`. Empaquetador local determinista `pipelines/release/package-release.ps1` generó 3 ZIPs con SHA-256 inmutables (marcados `isPublishable: false` mientras el digest sea placeholder).
- **Scripts de Operación:** `deploy/aws/` (`preflight.ps1`, `validate.ps1`, `bootstrap.ps1`, `verify.ps1`, `rollback.ps1`, `scale.ps1`, `teardown.ps1`) validados; scripts mutantes con barreras activas que rechazan ejecución sin `-ConfirmAWSExecution`.
- **Arquitectura y Documentación:** Reconciliada ruta real de readiness (`/api/<servicio>/health/ready`). Propuesto **ADR-12** (pendiente decisión: CodeDeploy tradicional vs ECS native circuit breaker en ALB compartido). Guías operativas completas en `docs/aws/` y matriz de correspondencia con las 9 fases del curso.
- **Entorno:** Pruebas de integración .NET, Compose y E2E Playwright siguen bloqueadas por daemon Docker Desktop inactivo. Sin operaciones de escritura en AWS.

## GOAL-01.2 — Dataset académico Showcase (2026-11-09, America/Bogota)

[Baseline](evidence/GOAL-01.2/baseline.md), [validación](evidence/GOAL-01.2/validation.md) y [handoff](evidence/GOAL-01.2/handoff.md). Se extendieron los seeders existentes a Basic/Showcase (`Demo:Profile`): Basic conserva 2/8/8/3/3; Showcase define 4 usuarios, 60 contactos, 100 oportunidades (25 New, 25 Contacted, 25 Proposal, 15 Won, 10 Lost), 120 seguimientos (30 por categoría al sembrar) y 150 interacciones (50 por tipo). IDs deterministas y campos editados conservados al resembrar. `deploy/local/run.ps1 -DemoProfile Showcase` genera contraseñas locales adicionales en `.tools/local/` e inyecta `DEMO_PROFILE` a Compose; no se ejecutó Compose por daemon Docker inactivo. Build .NET Release con SDK host 10.0.204 mediante ajuste temporal de global.json, luego restaurado al lock 10.0.300: 0 errores/0 warnings. Tests puros de catálogos 12/12 PASS (incluidos dos de regresión tras revisión independiente); tests PostgreSQL/Playwright BLOCKED por Docker; Angular build PASS y Vitest 5/5 PASS tras un intento previo fallido por timeout de workers. No se ejecutó AWS ni se tocó RDS/Azure Boards. Guion en [showcase-script.md](demo/showcase-script.md).

## GOAL-01.3 — Rediseño SaaS, dashboard gráfico y agenda con calendario (2026-11-09)

[Handoff y pruebas](evidence/GOAL-01.3/handoff.md). ADR-13 aprobada por instrucción expresa del usuario: logo SVG propio, AppShell autenticado único, Chart.js 4.6.1 para dos gráficos del Dashboard, FullCalendar 6.1.22 para agenda mes/semana/día/lista, GET de solo lectura Sales `follow-ups/calendar` con alcance y rango acotados; ficha, Kanban y login refinados. DB local respaldada antes de cambios; tras recrear solo Sales y Web, conteos 4/61/101/121/152 sin variación y tres health HTTP 200. .NET build PASS y tests Contacts 8/8, Sales 12/12 con PostgreSQL desechable; Angular build PASS, Vitest 8/8, Playwright E2E aislado 3/3; npm audit 0. SDK host 10.0.204 temporal para tests y `global.json` restaurado a 10.0.300. Capturas solo login 1440/390; auditoría visual completa y contraste medido pendientes. No AWS/Azure, no commit/push, no seeds ni migraciones sobre volumen persistente. **UX_READY: PARTIAL; AWS_NOT_DEPLOYED**.

## Siguiente incremento

Cerrar evidencias visuales autenticadas y pruebas de calendario integral/drag real en ambiente aislado, medir contraste y revisar defensa de overflow global sin alterar la semántica del scroll. SDK host 10.0.300 aún pendiente; ADR-12 y gates AWS continúan fuera de esta HU. Sin autorización AWS no desplegar.
