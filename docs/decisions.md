# Decisiones, aclaraciones y riesgos — ARCH-001 v1

**ADR-01..ADR-10 y P-01..P-08 aprobadas por el usuario el 2026-11-08 como ARCH-001 v1.**
Requisitos de las HU no se alteran al proponer una solución. Registrar aquí respuesta del usuario y fecha
cuando se aprueben. Un cambio importante posterior añade ADR con consecuencias; no reescribe silenciosamente el pasado.

## Registro de decisiones arquitectónicas

| ADR | Propuesta y razón ligada a HU | Alternativas descartadas / costo |
|---|---|---|
| ADR-01 | Dos APIs Contacts y Sales + Angular; límites HU01/02 y HU03–07, despliegue independiente HU11 | Monolito solo no demuestra microservicios; servicio por entidad/identidad/reportes agrega red, despliegues y trabajo sin HU que lo exijan |
| ADR-02 | Capas por carpetas, un csproj por API, DbContext directo; simple de implementar y probar | Clean Architecture de4 proyectos/API, CQRS, MediatR y repositorio genérico añaden indirección; no hay dominios que la justifiquen |
| ADR-03 | PostgreSQL 17/RDS, una instancia con dos bases/usuarios; EF/Npgsql 10 | SQL Server sí funciona en RDS, pero no hay funciones T-SQL ni conocimientos existentes que compensen su licencia/operación; SQLite/EF InMemory no cumplen la persistencia objetivo; Aurora y DB por servicio físico suben costo |
| ADR-04 | .NET 10 LTS y Angular 22.0 con toolchain compatible, parches bloqueados en bootstrap | .NET 8/9 terminan soporte en noviembre2026; .NET 11 no es LTS estable seleccionado a esta fecha. Angular 20 está al final de soporte; upgrades automáticos de major no son una tarea menor |
| ADR-05 | JWT RS25615 min en memoria, roles fijos y hasher de ASP.NET; usuarios seed, sin CRUD | Cookie/BFF central añade proxy y estado compartido; Cognito/OIDC externo es mejor para producto público pero exige permisos/integración no previstos; refresh tokens añaden rotación/revocación y CSRF si usan cookie |
| ADR-06 | Referencias HTTP y transacciones locales; no borrar identificadores externos; version numérica para concurrencia | DB compartida por tablas acopla servicios; eventos/outbox/Saga innecesarios sin flujos asíncronos. Sin concurrency token se perderían movimientos o reasignaciones |
| ADR-07 | Azure CI→ECR/S3 versionado→CodePipeline→CodeBuild migrador→CodeDeploy blue/green APIs; frontend ECS estándar | Conexión nativa Azure Repos/CodePipeline no se presupone. ECR+S3 disparados por separado pueden mezclar releases; usar VersionId. ECS blue/green nativo no conserva CodeDeploy del backlog; GitHub/mirror o CodeCommit duplican repositorios sin necesidad confirmada |
| ADR-08 | Un ALB; Sales llama Contacts por su URL HTTPS. Fargate público con ingress solo desde ALB y RDS privado | Service Connect incompatible con controlador CODE_DEPLOY; un ALB interno adicional o service discovery complica costo/despliegue. Aceptamos salto por ALB y public IP de salida con SG restrictivo |
| ADR-09 | Migraciones aditivas por tarea separada, credencial de migrador, sin migración al arrancar | Migración por réplica produce carreras; rollback destructivo de esquema arriesga datos y contradice blue/green |
| ADR-10 | Material/CDK, signals/Reactive Forms, REST tipado, Luxon 3 para zona de negocio, gráfico sencillo y seeds reales; pruebas reales con PostgreSQL | NgRx/microfrontend/varios kits/Chart.js o dashboard analítico son dependencias innecesarias; Luxon evita conversiones de zona improvisadas y no se usa como framework de estado |

## Aclaraciones aprobadas

Estas completan información ausente y fueron aceptadas junto con ARCH-001 v1.

| ID | Decisión concreta propuesta | Afecta / consecuencia |
|---|---|---|
| P-01 | CRM compartido: ambos roles ven y editan contactos y oportunidades; solo Admin reasigna. Agenda propia para Commercial, Admin puede todas. Interacciones visibles en CRM compartido | HU01/03/05/06/07; matriz completa en security.md. Si se desea ventas privadas por vendedor, cambiar consultas/permisos/pruebas antes del código |
| P-02 | Contacto de oportunidad inmutable; cerradas no se editan/reabren. Reasignar/mover a mismo valor es no-op200 sin nueva versión ni auditoría. Completar ya completado es idempotente200; cerrar otra vez sigue409 como exige HU04 | Evita incoherencia de actividades y reglas arbitrarias durante implementación. No eliminar el rechazo de segundo cierre |
| P-03 | Catálogo origen Web/Referral/WhatsApp/Call/Other; límites/normalización del modelo; fecha esperada obligatoria y cualquier fecha válida; dos decimales sin redondeo, monto máximo técnico | HU02/03 y formatos. El backlog no fija todas estas opciones. «WhatsApp» como origen no significa integración |
| P-04 | Sesión en memoria15 min sin refresh: recargar exige login; logout local, sin revocación inmediata. Claves asimétricas configuradas | HU01; aceptación explícita del compromiso académico de sesión/seguridad |
| P-05 | Exposición autenticada pública solo con HTTPS y certificado; ALB/Fargate/CodeBuild y topología descrita; no desplegar HTTP con contraseña por falta de dominio | HU11/12, fortalece propuesta respecto al HTTPS condicionado del backlog. Si no hay certificado, bloquear publicación y proponer alternativa, no inventarla |
| P-06 | Diferenciar fundamento disponible de cierre completo de HU01/HU02/HU09/HU08; roadmap de incrementos parciales sin eliminar criterios ni falsear estados | Resuelve D-01..D-04. Una historia permanece Activa/pendiente hasta ejecutar sus criterios diferidos |
| P-07 | Sistema visual/tokens y pruebas definidos, sin SLA numérico ni promesa HA. Tamaños/retención/costo de laboratorio como hipótesis verificables | HU08/11/13; no confundir objetivos con resultados medidos |
| P-08 | Aclarar HU03-AC01: «contacto y usuario activos» se interpreta como contacto existente y usuario activo; HU02 no dispone de estado de contacto | No añadir isActive ni flujo de activación de contactos sin necesidad confirmada. Si se requiere ese estado, ajustar HU02/modelo/contratos y pruebas antes del código. Hasta aprobar, esta interpretación no es requisito confirmado |

P-02 «no-op» aplica solo a recurso abierto/en estado compatible; autorización, versión y validación siguen
obligatorias antes de devolver 200. Para completar uno ya completado, conservar actor/fecha originales;
el acceso se valida incluso cuando la operación es idempotente. P-03 no exige una fecha futura ni impide
agendar un seguimiento pasado: son reglas diferentes sin justificación para añadir restricciones.

## Gates externos, sin respuesta inventada

| Gate | Falta comprobar | Bloquea | Cómo resolverlo |
|---|---|---|---|
| G-01 | Resuelto el 2026-11-08: guía 181128 pegada por el usuario y alcance del docente aclarado. Desplegar SmartSales en microservicios; sin línea base monolítica ni reproducción obligatoria de Cloud9/CodeCommit/capturas | No bloquea desarrollo local; HU09-AC03 requiere una matriz breve guía→arquitectura y permisos reales | Ver [resumen de guía](source/lab-guide-summary.md); confirmar G-02 antes de AWS |
| G-02 | Permisos/región/cuotas AWS: RDS17, Fargate, ALB, S3 versioning, SSM, CodeBuild, CodeDeploy, CodePipeline y PassRole; roles amplios LabRole | Aprovisionamiento y pipeline | Inventario read-only de cuenta autorizada; documentar capacidades y limitaciones. Variante EC2 requiere decisión y revisión de costo |
| G-03 | Dominio controlado, validación DNS y certificado ACM en región ALB | Exposición pública segura HU11 | Confirmar disponibilidad; si no existe, presentar ADR de alternativa HTTPS (p. ej. CloudFront si permitido), esperar aprobación |
| G-04 | Agente/minutos de Azure Pipelines y credenciales AWS temporales o federación permitida | Ejecución real HU12 | Verificar conexión autorizada, expiración de sesión y capacidad; registrar bloqueo real si no hay acceso |

G-01 está resuelto por la aclaración del usuario. G-02..G-04 siguen sin evidencia. La aprobación de la
arquitectura puede dejar estos gates pendientes para su fase. La implementación local requiere aprobación
y autorización de alcance, aun si no necesita AWS.

## Riesgos y respuesta

| Riesgo | Efecto | Respuesta / responsable de decisión |
|---|---|---|
| RDS y ALB compartidos; una tarea/API | Punto de fallo/capacidad compartida | Aceptar Demo, medir y documentar; no prometer HA |
| Validación síncrona de Contacts | Comandos Sales dependen de directorio | Timeout 3 s,503 sin escritura; lecturas propias independientes; no readiness acoplada |
| Sin revocación inmediata JWT | Token robado/usuario desactivado válido hasta15 min | Memoria, HTTPS y expiración corta; P-04. Para producto real, evaluar OIDC/revocación con ADR |
| Public IP tareas / roles LabRole amplios | Superficie y aislamiento menores que producción | SG solo ALB, secrets separados y permisos efectivos documentados; detener si no se cumple exposición prevista |
| Blue/green + migración incompatible | Rollback de binario no restaura esquema | Expand/contract y prueba de versión anterior; no drops automáticos |
| Releases concurrentes o credencial vencida | Imagen/plantilla mezclada o pipeline fallida | VersionId y digest, QUEUED, trazabilidad y renovación autorizada de sesión |
| Catálogos/permisos no aprobados | Agente barato inventa negocio | P-01..P-03 explícitos y gate en AGENTS.md |
| Estado de Node/Docker local | No se pueden construir/probar componentes | Preparar herramientas tras autorización; no afirmar tests corridos ahora |
| Memoria de contexto o costo de modelo | Desvíos de decisiones/criterios | Lectura por HU, contratos exactos, roadmap, snapshot y handoff con evidencia |

## Control de cambios después de aprobar

### ADR-11 propuesto — actualización menor de Angular 22 por avisos de seguridad

**Estado:** aprobado por el usuario el 2026-11-08; implementación y verificación en curso. Afecta HU08/HU10/HU12 y el lock de frontend.
El audit del 2026-11-08 sobre el lock actual 22.0.x reportó 7 avisos en dependencias de ejecución
(5 moderados, 2 altos) y 31 totales; el `npm audit fix` compatible no resolvió Zod ni Angular.
Las correcciones anunciadas por npm requieren Angular 22.2.x, que cambia el minor fijado por ADR-04.

**Propuesta:** actualizar juntos Angular core/common/compiler/forms/platform/router a 22.2.x y CLI/build,
Material/CDK a parches compatibles de esa línea, regenerar lock; repetir build, unitarias, E2E y Compose.
Mantener .NET, contratos, datos y topología. Alternativa: conservar 22.0.x y registrar avisos
pendientes antes de la entrega. Riesgo principal: incompatibilidad de peer dependencies o regresión UI;
se detecta con las pruebas integradas. No ejecutar `npm audit fix --force` sin revisar el diff.

ADR nuevo: contexto y HU afectadas → propuesta → alternativas → impacto en contratos/datos/seguridad/
costo → pruebas/migración → decisión del usuario y fecha. Parches compatibles y refactor interno no
cambian ADR; registrar lock y resultados. Un criterio de Azure cambiado exige reconciliación explícita
de requirements, pruebas, roadmap y snapshot; nunca usar snapshot viejo para descartar un requisito nuevo.

### ADR-12 propuesto — resolución de compatibilidad entre CodeDeploy ECS Blue/Green y ALB compartido con reglas por path

**Estado:** PROPUESTA (pendiente de aprobación del usuario, registrado el 2026-11-09). Afecta HU11/HU12, ADR-07 y ADR-08.

**Contexto del problema:**
ADR-07 aprobó despliegue Blue/Green con AWS CodeDeploy para Contacts y Sales, y ADR-08 aprobó un único Application Load Balancer (ALB) con enrutamiento basado en rutas (`/api/contacts/*`, `/api/sales/*`) y Web en el fallback default.
Al investigar la especificación oficial de la API de AWS CodeDeploy (`TrafficRoute.ListenerArns` y `TargetGroupPairInfo`), se evidencia que CodeDeploy para ECS conmuta el tráfico modificando la configuración del **Listener completo** (ARN de Listener), no una regla individual basada en ruta (`ListenerRule`).
Si dos Deployment Groups independientes (Contacts y Sales) administran el mismo Listener HTTPS 443 del ALB:
1. Habrá colisión inevitable entre despliegues: el primer servicio que conmute el listener sobreescribirá la acción por defecto del listener, rompiendo la regla de enrutamiento por path del otro servicio y del frontend Web.
2. Un test listener secundario tampoco soluciona la titularidad de las reglas del listener productivo principal.

**Alternativas analizadas:**
1. **Alternativa A (Recomendada — Controlador ECS Rolling / Blue-Green nativo con Deployment Circuit Breaker):**
   Usar el controlador de despliegue nativo de ECS (`deploymentController: ECS`) con `deploymentCircuitBreaker` (con rollback automático activado) y alarmas de CloudWatch. Conserva estrictamente el **ALB único** (ADR-08), el enrutamiento por rutas relativas del frontend, sin costos adicionales de balanceadores, y garantiza rollback automático si una nueva tarea no pasa el health check. Requiere ajustar ADR-07 para permitir el controlador nativo de ECS en lugar de CodeDeploy.
2. **Alternativa B (CodeDeploy tradicional con Listeners y Puertos Dedicados):**
   Conservar CodeDeploy tradicional asignando listeners en puertos separados del mismo ALB (ej. 443 para Web, 8443 para Contacts, 9443 para Sales). Requiere modificar la configuración de red y certificados, e impide URLs relativas homogéneas en el frontend SPA.
3. **Alternativa C (Dos o Tres ALBs independientes):**
   Crear un ALB por servicio o un ALB para APIs y otro para Web. Aumenta significativamente el costo de infraestructura del laboratorio AWS Academy (~$22.50 USD/mes por ALB adicional) y contradice ADR-08.

**Propuesta concreta:**
Mantener la preparación de CloudFormation de red, datos y aplicación (`network.yaml`, `data.yaml`, `application.yaml`) lista con el ALB único y los Target Groups IP correspondientes. Dejar la plantilla de entrega (`delivery.yaml`) y el gate de despliegue en estado `BLOCKED` documentado hasta que el usuario elija entre la Alternativa A (ECS nativo con Circuit Breaker) o la Alternativa B/C (CodeDeploy con separación de listeners/balanceadores). No aplicar ninguna alternativa de forma unilateral.

### ADR-13 — Rediseño visual profesional: incorporación de Chart.js 4.x, FullCalendar Standard 6.1.x y AppShell centralizado

**Estado:** APROBADA (Instrucción expresa del usuario en GOAL 01.3, registrado el 2026-11-09). Modifica parcialmente ADR-10. Afecta HU05, HU07, HU08 y `src/Web`.

**Contexto:**
ADR-10 aprobó originalmente componentes nativos Angular Material/CDK con gráficos simples SVG/CSS y una agenda en formato lista por categorías. La evaluación de uso del CRM demostró que el aspecto visual resultaba plano, carente de jerarquía SaaS profesional, y la ausencia de una vista de calendario impedía una gestión temporal adecuada de los 120 seguimientos comerciales.

**Decisión:**
1. **Librerías gráficas y calendario:**
   - Incorporar **Chart.js 4.x (licencia MIT)** para la renderización de gráficos interactivos (doughnut de distribución por etapas y barras de volumen de oportunidades) en el Dashboard, con ciclo de vida que destruye explícitamente las instancias de canvas en `ngOnDestroy` para evitar fugas de memoria.
   - Incorporar **FullCalendar Standard v6.1.x (licencia MIT)** y su adaptador oficial `@fullcalendar/angular` (paquetes `@fullcalendar/core`, `@fullcalendar/daygrid`, `@fullcalendar/timegrid`, `@fullcalendar/list`, `@fullcalendar/interaction`) para proveer vistas mensuales, semanales, diarias y de lista en la Agenda. Se prohíbe explícitamente FullCalendar Scheduler/Premium o dependencias comerciales.
   - Conservar **Angular Material/CDK 22** y **Luxon 3** para zona horaria `America/Bogota`.
2. **Layout y Navegación:**
   - Reemplazar las cabeceras duplicadas en cada componente por un componente layout global `AppShell` autenticado con barra lateral colapsable, navegación superior contextual y un único `<router-outlet>` hijo. La ruta `/login` permanece externa al AppShell.
3. **Endpoint de Calendario en Sales API:**
   - Incorporar un endpoint de solo lectura `GET /api/sales/v1/follow-ups/calendar?from=...&to=...&scope=...` para consultar seguimientos en rangos de fechas de hasta 365 días (máx. 500 registros) sin truncamiento artificial, manteniendo la autorización y las reglas de negocio vigentes.

**Consecuencias:**
- El tamaño del bundle frontend se mantiene acotado bajo los budgets definidos en `angular.json` mediante carga diferida (lazy loading).
- No se introducen microfrontends, frameworks de CSS externos (Tailwind/Bootstrap) ni librerías de estado global (NgRx).
- Se preserva el 100% de la funcionalidad preexistente, incluyendo la vista de lista de agenda mediante un selector de vista (toggle).
