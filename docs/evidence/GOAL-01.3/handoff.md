# GOAL-01.3 — Rediseño visual y calendario real

Fecha: 2026-11-09 (America/Bogota). Rama `master`, HEAD inicial `011c57b`. Plan de `goal-architect` consultado antes de editar; revisión independiente `goal-reviewer` al final.

## Resultado y decisiones

- **UX_READY: PARTIAL**: diseño, calendario y gráficos implementados y compilados; no se obtuvo el conjunto completo de capturas autenticadas 1440/390 ni una medición automática WCAG 2.2 AA. **AWS_NOT_DEPLOYED**.
- ADR-13 documentada en `docs/decisions.md`: la instrucción expresa de GOAL-01.3 autoriza Chart.js 4.x, FullCalendar Standard v6.1.x, revisión de tokens y shell; ADR-12 sobre AWS continúa pendiente y no se modificaron los scripts de infraestructura.
- Dos APIs autónomas, PostgreSQL 17 y el flujo comercial existente conservados; ningún seed sobre la base persistente.

## Implementado

| Área | Cambios |
|---|---|
| Identidad | SVG originales `src/Web/public/smartsales-mark.svg` y `smartsales-logo.svg`; favicon local SVG, tokens semánticos CSS en `src/Web/src/styles.scss`, sin fuente/CDN. |
| Shell | `src/Web/src/app/layout/app-shell.{ts,html,scss}` y `app.routes.ts`: navegación lateral autenticada, menú móvil, cierre de sesión, `main` único y rutas originales; se eliminaron cabeceras repetidas de dashboard/contactos/oportunidades/Kanban/agenda. |
| Dashboard | `home.{ts,html,scss}`: cuatro KPI desde API real, doughnut y barras Chart.js, tabla textual equivalente, proporción ganadas / (ganadas + perdidas) solo cuando hay cierres, filtros preservados en URL; destrucción de instancias Chart.js. |
| Agenda | `follow-ups-page.{ts,html,scss}` y `follow-ups.api.ts`: FullCalendar mes, semana, día y lista con `es`, lunes y hora Bogotá; alternancia calendario/lista antigua; eventos obtenidos por rango real de API, clic a detalle y completar, selección de día al formulario, Mine/All protegido en backend; no hay edición ficticia por drag. |
| Sales API | `FollowUpService.GetCalendarAsync` y `FollowUpEndpoints.CalendarAsync`: GET autenticado `follow-ups/calendar?from=yyyy-MM-dd&to=yyyy-MM-dd&scope=Mine|All`, rango local Bogotá inclusivo máximo 365 días, 500 resultados máximo (413 si excede), orden por fecha/ID, no-store y 403 Comercial para `All`. Contrato aditivo en `docs/api-contracts.md`, sin migración. |
| Kanban, fichas y login | `pipeline-page.{html,scss}` y estilos de contactos/oportunidades: columnas semánticas, tarjetas ricas, formularios y listas con estética consistente; CDK drag/drop y acciones originales conservadas. `login.{html,scss}`: marca propia y panel de acceso; no credenciales embebidas. |

Paquetes instalados con lock exacto: `chart.js@4.6.1` (MIT), `@fullcalendar/angular`, `@fullcalendar/core`, `@fullcalendar/daygrid`, `@fullcalendar/timegrid`, `@fullcalendar/list`, `@fullcalendar/interaction` todos `6.1.22` (MIT, peer Angular 12–22). `npm audit`: **0 avisos** al ejecutar. Se ampliaron budgets en `angular.json` (inicial advertencia 600 kB, error 1 MB; estilo componente advertencia 10 kB, error 16 kB); bundle inicial observado 540.01 kB, calendar chunk 309.76 kB y dashboard chunk 199.04 kB lazy.

## Inspiración — sin copiar código ni marcas

Los CRMs Twenty/EspoCRM fueron referencias declaradas por el propietario; no se clonaron ni importaron componentes. Se aplicaron principios: (1) navegación lateral de contexto persistente; (2) ficha dual lista/detalle con acción primaria; (3) embudo con etapas visibles y tarjetas manipulables; (4) dashboard que permite comparar estados con leyenda y tabla; (5) agenda calendario/lista para acciones temporales. La atribución de patrones a capturas específicas queda **no verificada**: no se inspeccionaron capturas de esas aplicaciones en esta sesión. Los SVG, estilos y textos son propios.

Skills cargadas con herramienta `skill`: `frontend-design` y `web-design-guidelines`; se aplicaron jerarquía, contraste previsto, foco, navegación, semántica, fechas y estados vacíos. **No se afirma cumplimiento WCAG AA medido**.

## Protección de datos y Docker

Antes de intervenir se midieron **4 usuarios, 61 contactos, 101 oportunidades, 121 seguimientos, 152 interacciones** en el volumen local. Se crearon respaldos lógicos en `artifacts/backups/` (ignorado por Git): SQL y archivos custom `.dump` por base; se verificaron cabeceras y `pg_restore --list`. Copia de los `.dump` en `C:\Users\santi\AppData\Local\Temp\opencode\backups\SmartSales\`. No se incluyeron contraseñas en el informe. Solo se reconstruyeron/recrearon **Sales y Web** con `docker compose build sales web` y `docker compose up -d --no-deps sales web`, variables existentes cargadas en la sesión desde `.tools/local/secrets.json`; **no se ejecutaron migraciones, seeds ni `down -v`** en el volumen persistente. Conteos posteriores **idénticos**: 4/61/101/121/152. `/healthz`, `/api/contacts/health/ready` y `/api/sales/health/ready`: **HTTP 200** cada una. URL: http://127.0.0.1:8088.

## Pruebas ejecutadas

| Prueba | Resultado |
|---|---|
| `dotnet build SmartSales.slnx --configuration Release` y `dotnet test SmartSales.slnx --configuration Release` usando SDK host 10.0.204 con cambio temporal y restaurado de `global.json` | **PASS**: 0 errores/avisos; Contacts 8/8 y Sales 12/12, PostgreSQL 17 Testcontainers desechable. SDK 10.0.300 fijado no está en host. |
| Angular `ng build` Node portable 24.22.1 | **PASS** bundle inicial 540.01 kB, chunks diferidos; budget ajustado explícitamente. |
| Angular Vitest `ng test --watch=false` | **PASS** 8/8 (antes 5); incluye ratio, alcance y rango/calendario. |
| `npm audit` lock actualizado | **PASS** 0 vulnerabilidades reportadas en esta consulta puntual. |
| `tests/e2e/run.ps1` en PostgreSQL desechable | **PASS** 3/3 tras reparar selectors y scroll móvil; verifica login, Contacts/Sales, agenda-lista, Kanban teclado, permisos Comercial y 390px. Primeras corridas **FAIL** por selector ambiguo y overflow; corregidas. No comprueba todavía drag real ni todas las vistas FullCalendar. |
| Docker build `sales web`, recreación `--no-deps`, GET readiness y conteos pre/post | **PASS** (salvo SDK host; Docker utiliza su propio SDK). |
| GET calendario real en demo existente | **PASS**: 61 seguimientos reales en rango de 45 días para Admin `All`, no los primeros 20 de la lista. |
| Capturas después | **PARCIAL**: `artifacts/evidence/GOAL-01.3/login-desktop-after.png` y `login-mobile-after.png`; capturas autenticadas antes/después de todas las vistas **NOT_RUN**. No se registraron PII ni secretos en evidencia versionada. |

El `goal-reviewer` señaló fecha máxima `9999-12-31` que causaba 500; se añadió rechazo 400 y test integrado. Falta cubrir 500/501 y probar gesto CDK real con navegador.

## Pendientes y próximos pasos autorizados

1. Medir WCAG 2.2 AA (colores/foco) y revisar visualmente las siete vistas a 1440/390 con usuario de prueba en entorno aislado. No usar capturas del volumen compartido con PII.
2. Agregar pruebas E2E aisladas de calendario mes/semana/día, clic/completar y 120+ seguimientos del Showcase, además de drag-and-drop real y filtros del Dashboard; la cobertura actual es 3/3 del flujo original más pruebas unitarias de calendario.
3. Investigar desbordamiento de raíz *sin ocultación global*: el primer fallo medía 411px a viewport 390 mientras el drawer móvil aún estaba en transición. Tras esperar a que la barra lateral salga del viewport, E2E aislado pasa. `html/body { overflow-x: hidden; }` continúa como defensa; revisar si puede retirarse sin perder el gate y mantener el calendario en su contenedor desplazable.
4. El despliegue AWS permanece fuera del alcance; G-02/03/04 y ADR-12 sin aprobación. Ninguna operación AWS/Azure de escritura, commit ni push se realizó.

**Abrir demo:** http://127.0.0.1:8088/login (credenciales existentes fuera de Git en `.tools/local/secrets.json`). Recorrer Dashboard → Contactos → Oportunidades → Tablero Kanban → Agenda; filtro y consulta de calendario son lectura, pero crear/editar/completar modifica datos persistentes. Para pruebas mutantes usar `tests/e2e/run.ps1`, no la base persistente del propietario.
