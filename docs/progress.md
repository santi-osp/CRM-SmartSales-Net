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

No cerrar HU01–HU04 en Boards hasta comprobar cada criterio con el recorrido integrado y registrar el resultado. No hay commit ni push por instrucción de [AGENTS.md](../AGENTS.md).

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

## Siguiente incremento

Completar contenedores HU10, probar Compose y preparar ECR cuando haya acceso al laboratorio. Verificar criterios funcionales restantes antes de cerrar HU01–HU08 en Boards. Registrar desviaciones arquitectónicas antes de aplicarlas.
