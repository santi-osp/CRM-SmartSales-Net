# Requisitos y trazabilidad — baseline ARCH-001 v1

Consulta de Azure DevOps: 2026-11-08 (America/Bogota). Snapshot exacto de campos relevantes en
[source/backlog-snapshot.json](source/backlog-snapshot.json). Instrucciones del usuario en
[source/user-brief.md](source/user-brief.md). Ningún requisito fue implementado en esta fase.

## Confirmado y propuesto

Confirmado en el contexto: proyecto compacto universitario; .NET/Angular; adaptación del stack permitida;
RDS; migración a Agile y MVP actual. Los 13 User Story activos continúan New; HU14/HU15 Removed.
La guía 181128 fue aportada después de esta baseline. El alcance académico aplicable se resume en
[source/lab-guide-summary.md](source/lab-guide-summary.md); la aplicación monolítica queda excluida por el usuario.

Los campos de las HU actuales son la baseline funcional. Las aclaraciones P-01..P-08 de [decisions](decisions.md)
son propuestas explícitas: no se convierten en reglas confirmadas por aparecer en contratos o tablas.
En caso de discrepancia, detener solo la parte dependiente y solicitar decisión.

## Matriz de trazabilidad

| épica | Feature | HU / Azure ID | Descripción | AC | Predecesoras en Boards | Prioridad | Módulos | Estado |
|---|---|---|---|---|---|---|---|---|
| EP01 #120 | #122 | [HU01 #129](https://dev.azure.com/mengrau/CRM%20SmartSales%20.Net/_workitems/edit/129) | Acceder al CRM con dos roles y cuentas de demostración | HU01-AC01, HU01-AC02, HU01-AC03 | HU09 | 1 | Contacts/Auth, Angular/Auth, Sales/JWT | New; sin implementación |
| EP01 #120 | #122 | [HU02 #132](https://dev.azure.com/mengrau/CRM%20SmartSales%20.Net/_workitems/edit/132) | Registrar, buscar y actualizar prospectos y clientes | HU02-AC01, HU02-AC02, HU02-AC03 | HU09 | 1 | Contacts/Contacts, Angular/Contacts | New; sin implementación |
| EP01 #120 | #123 | [HU03 #135](https://dev.azure.com/mengrau/CRM%20SmartSales%20.Net/_workitems/edit/135) | Crear y editar oportunidades con responsable y prioridad manual | HU03-AC01, HU03-AC02, HU03-AC03 | HU01, HU02 | 1 | Sales/Opportunities, Contacts/Directory, Angular/Opportunities | New; sin implementación |
| EP01 #120 | #123 | [HU04 #138](https://dev.azure.com/mengrau/CRM%20SmartSales%20.Net/_workitems/edit/138) | Gestionar el Kanban y cerrar oportunidades ganadas o perdidas | HU04-AC01, HU04-AC02, HU04-AC03, HU04-AC04 | HU03 | 1 | Sales/Opportunities, Angular/Kanban | New; sin implementación |
| EP01 #120 | #124 | [HU05 #142](https://dev.azure.com/mengrau/CRM%20SmartSales%20.Net/_workitems/edit/142) | Programar y completar seguimientos desde una agenda | HU05-AC01, HU05-AC02, HU05-AC03, HU05-AC04 | HU02, HU01 | 2 | Sales/FollowUps, Angular/Agenda | New; sin implementación |
| EP01 #120 | #124 | [HU06 #145](https://dev.azure.com/mengrau/CRM%20SmartSales%20.Net/_workitems/edit/145) | Registrar interacciones manuales y consultar el historial | HU06-AC01, HU06-AC02, HU06-AC03 | HU01, HU02 | 2 | Sales/Interactions, Angular/Interactions | New; sin implementación |
| EP01 #120 | #124 | [HU07 #148](https://dev.azure.com/mengrau/CRM%20SmartSales%20.Net/_workitems/edit/148) | Consultar un dashboard comercial con filtros simples | HU07-AC01, HU07-AC02, HU07-AC03 | HU03 | 2 | Sales/Dashboard, Angular/Dashboard | New; sin implementación |
| EP01 #120 | #125 | [HU08 #151](https://dev.azure.com/mengrau/CRM%20SmartSales%20.Net/_workitems/edit/151) | Presentar una interfaz consistente y datos de demostración | HU08-AC01, HU08-AC02, HU08-AC03 | HU01 | 2 | Angular/Shared/Layout, seeds de ambas APIs | New; sin implementación |
| EP02 #121 | #126 | [HU09 #154](https://dev.azure.com/mengrau/CRM%20SmartSales%20.Net/_workitems/edit/154) | Definir dos microservicios y persistencia PostgreSQL en RDS | HU09-AC01, HU09-AC02, HU09-AC03 | — | 1 | Arquitectura, ambas bases, contratos | New; sin implementación |
| EP02 #121 | #126 | [HU10 #158](https://dev.azure.com/mengrau/CRM%20SmartSales%20.Net/_workitems/edit/158) | Empaquetar componentes y publicar imágenes versionadas en ECR | HU10-AC01, HU10-AC02 | HU09, HU03, HU02 | 1 | Build de Contacts/Sales/Web, ECR | New; sin implementación |
| EP02 #121 | #127 | [HU11 #161](https://dev.azure.com/mengrau/CRM%20SmartSales%20.Net/_workitems/edit/161) | Desplegar servicios en ECS con balanceador y logs | HU11-AC01, HU11-AC02, HU11-AC03 | HU10, HU09 | 1 | ECS/ALB/RDS/CloudWatch | New; sin implementación |
| EP02 #121 | #127 | [HU12 #165](https://dev.azure.com/mengrau/CRM%20SmartSales%20.Net/_workitems/edit/165) | Automatizar validación y despliegue con rollback | HU12-AC01, HU12-AC02, HU12-AC03 | HU11 | 1 | Azure Pipelines/S3/ECR/CodePipeline/CodeDeploy | New; sin implementación |
| EP02 #121 | #127 | [HU13 #169](https://dev.azure.com/mengrau/CRM%20SmartSales%20.Net/_workitems/edit/169) | Preparar evidencias y demostración del entregable | HU13-AC01, HU13-AC02, HU13-AC03 | HU04, HU07, HU05, HU08, HU12, HU06 | 1 | Demostración/documentación/evidencias | New; sin implementación |
| EP01 #120 | #128 | [HU14 #172](https://dev.azure.com/mengrau/CRM%20SmartSales%20.Net/_workitems/edit/172) | Integrar WhatsApp empresarial después del entregable | HU14-AC01 | — | 4 | Fuera MVP | Removed; sin implementación |
| EP01 #120 | #128 | [HU15 #173](https://dev.azure.com/mengrau/CRM%20SmartSales%20.Net/_workitems/edit/173) | Incorporar Lead Score después del entregable | HU15-AC01 | — | 4 | Fuera MVP | Removed; sin implementación |

Los vínculos de Boards son predecesoras de historias agregadas. El roadmap distingue la disponibilidad
de fundamentos de su cierre completo para evitar dependencias circulares de verificación (D-01 y D-02).

## Requisitos no funcionales y restricciones

| ID | Exigencia | Fuente / estado |
|---|---|---|
| NF-01 | Servicios .NET compilables, desplegables y migrables por separado | HU09/HU11 confirmado |
| NF-02 | PostgreSQL RDS privado, TLS y acceso propio por servicio | HU09 confirmado; versión17 propuesta |
| NF-03 | UTC almacenado, America/Bogota mostrado, COP | HU03/HU05/HU07 confirmado |
| NF-04 | Autorización en ambas APIs, hash de contraseñas, secretos externos | HU01/HU09 confirmado |
| NF-05 | Responsive1440/390, teclado, estados de carga/vacío/error, seeds idempotentes | HU08 confirmado |
| NF-06 | Imagen/commit/digest y release trazables; rollback e independencia | HU10/HU11/HU12 confirmado |
| NF-07 | Consultas paginadas, timeouts y fallos parciales sin escritura incompleta | Decisión técnica propuesta; sin SLA de carga inventado |
| NF-08 | WCAG2.2 AA como objetivo a verificar | Objetivo técnico propuesto; no certificación |
| NF-09 | HTTPS para exposición pública autenticada | Propuesta P-05; gate G-03 |
| NF-10 | Pruebas reales y progreso respaldado por evidencia | Instrucción del usuario |

No se ha acordado SLA, presupuesto numérico, concurrencia objetivo, alta disponibilidad ni volumen productivo.
Se medirán en el laboratorio; no se prometen cifras sin datos. RDS Single-AZ y una tarea por servicio son
una simplificación académica propuesta, no cumplimiento de un SLA de disponibilidad.

## Historias: alcance y criterios íntegros

## [HU01] Acceder al CRM con dos roles y cuentas de demostración — #129

Estado leído: **New**; implementación: ninguna. Revisión Azure: 1.

Como usuario, quiero iniciar y cerrar sesión según mi rol, para proteger la información.

### Alcance

- Roles fijos Administrador y Comercial y cuentas de prueba precargadas. El CRUD de usuarios de #2 queda fuera del MVP.

- Correo y contraseña, hash de contraseñas y validación de cuenta activa; autorización tanto en Angular como en ambas APIs.

- CRM comercial compartido. Solo Administrador reasigna oportunidades; Comercial las crea a su nombre. Cerrar sesión elimina la sesión local y vuelve a proteger las rutas.

### Finalización

- Criterios verificados con evidencia y cambios vinculados a esta HU.

- Pruebas pertinentes de reglas y errores; revisión del equipo antes de cerrar.

- Estado New: planificación, sin implementación ni pruebas ejecutadas.

### Trazabilidad al proyecto Basic

- #11 [SCRUM-10] [PBI 101] Iniciar sesión y visualizar únicamente las funciones permitidas por mi rol

- #2 [SCRUM-9] [PBI 102] Crear, editar, activar o desactivar usuarios y asignarles un rol

### Criterios de aceptación

#### HU01-AC01

Acceso y salida
Dado credenciales válidas y cuenta activa
Cuando inicia sesión y luego sale
Entonces accede según su rol y debe autenticarse nuevamente para entrar.

#### HU01-AC02

Credenciales inválidas
Dado credenciales incorrectas o cuenta inactiva
Cuando intenta ingresar
Entonces se rechaza con mensaje genérico sin revelar si el correo existe.

#### HU01-AC03

Permisos
Dado un Comercial autenticado
Cuando intenta reasignar por interfaz o API
Entonces la API lo deniega sin modificar datos y Angular solo muestra opciones autorizadas.

Casos de aceptación vinculados: #174.
Tareas: #130, #131.

## [HU02] Registrar, buscar y actualizar prospectos y clientes — #132

Estado leído: **New**; implementación: ninguna. Revisión Azure: 1.

Como Comercial, quiero gestionar una ficha unificada de contactos, para mantener la información comercial en una sola sección.

### Alcance

- Listado con búsqueda por nombre/correo/teléfono, creación, detalle y edición.

- Nombre y tipo Prospecto/Cliente obligatorios; correo o teléfono obligatorio; origen de catálogo fijo. Advertir duplicados normalizados.

- Actualizar conserva relaciones e historial. Sin eliminación física ni importación masiva.

### Finalización

- Criterios verificados con evidencia y cambios vinculados a esta HU.

- Pruebas pertinentes de reglas y errores; revisión del equipo antes de cerrar.

- Estado New: planificación, sin implementación ni pruebas ejecutadas.

### Trazabilidad al proyecto Basic

- #12 [SCRUM-11] [PBI 103] Registrar un prospecto o cliente con sus datos de contacto y canal de origen

- #14 [SCRUM-13] [PBI 104] Buscar, actualizar y consultar una ficha comercial de prospectos y clientes

### Criterios de aceptación

#### HU02-AC01

Crear y buscar
Dado datos válidos con al menos un medio de contacto
Cuando guarda y busca por coincidencia parcial
Entonces el contacto persiste y se encuentra sin distinguir mayúsculas.

#### HU02-AC02

Validar y advertir
Dado datos incompletos o coincidentes con otro contacto
Cuando intenta guardar
Entonces bloquea la información incompleta y advierte el posible duplicado antes de confirmarlo.

#### HU02-AC03

Conservar relaciones
Dado un contacto con historial y oportunidades
Cuando actualiza datos válidos
Entonces la ficha cambia y conserva sus relaciones.

Casos de aceptación vinculados: #175.
Tareas: #133, #134.

## [HU03] Crear y editar oportunidades con responsable y prioridad manual — #135

Estado leído: **New**; implementación: ninguna. Revisión Azure: 1.

Como Comercial, quiero registrar oportunidades de un contacto, para organizar ventas con una prioridad comprensible.

### Alcance

- Título, contacto existente, monto COP no negativo, fecha esperada, responsable activo y prioridad Alta/Media/Baja (Media predeterminada).

- Etapa inicial Nuevo. Comercial es responsable de lo que crea; Administrador puede elegir y cambiar responsable de oportunidades abiertas. Conservar autor/fecha de reasignación.

- Filtros por responsable y prioridad. Validar referencias mediante API; un fallo no deja oportunidades incompletas.

### Finalización

- Criterios verificados con evidencia y cambios vinculados a esta HU.

- Pruebas pertinentes de reglas y errores; revisión del equipo antes de cerrar.

- Estado New: planificación, sin implementación ni pruebas ejecutadas.

### Trazabilidad al proyecto Basic

- #13 [SCRUM-12] [PBI 201] Crear una oportunidad asociada a un prospecto o cliente

- #15 [SCRUM-14] [PBI 202] Asignar o reasignar un vendedor responsable a una oportunidad

- #25 [SCRUM-24] [PBI 402] Visualizar y filtrar oportunidades por prioridad alta, media o baja

### Criterios de aceptación

#### HU03-AC01

Crear
Dado un contacto y usuario activos
Cuando registra datos válidos
Entonces inicia en Nuevo con responsable activo y prioridad manual.

#### HU03-AC02

Validaciones
Dado monto negativo, contacto inexistente o responsable inactivo
Cuando intenta guardar
Entonces se rechaza sin persistir datos incompletos.

#### HU03-AC03

Reasignar y filtrar
Dado un Administrador y oportunidades abiertas
Cuando reasigna y filtra por responsable y prioridad
Entonces registra autor/fecha y muestra solo coincidencias; limpiar restaura resultados.

Casos de aceptación vinculados: #176.
Tareas: #136, #137.

## [HU04] Gestionar el Kanban y cerrar oportunidades ganadas o perdidas — #138

Estado leído: **New**; implementación: ninguna. Revisión Azure: 1.

Como Comercial, quiero mover y cerrar oportunidades, para demostrar el ciclo comercial completo.

### Alcance

- Columnas activas fijas Nuevo, En contacto y Propuesta. Avanzar o retroceder entre ellas; cierre por acción explícita.

- Tarjetas con contacto, monto, responsable y prioridad. Ganada requiere monto final no negativo; Perdida requiere motivo.

- Conservar autor, fecha y resultado. Cerradas fuera del tablero activo; sin reapertura ni configuración dinámica de etapas.

### Finalización

- Criterios verificados con evidencia y cambios vinculados a esta HU.

- Pruebas pertinentes de reglas y errores; revisión del equipo antes de cerrar.

- Estado New: planificación, sin implementación ni pruebas ejecutadas.

### Trazabilidad al proyecto Basic

- #18 [SCRUM-17] [PBI 203] Visualizar y mover oportunidades entre las etapas de un tablero Kanban

- #17 [SCRUM-16] [PBI 204] Cerrar una oportunidad como ganada o perdida y registrar su resultado

### Criterios de aceptación

#### HU04-AC01

Persistir movimiento
Dado una oportunidad abierta
Cuando la mueve a otra etapa activa y recarga
Entonces aparece una sola vez en su etapa persistida.

#### HU04-AC02

Fallo de movimiento
Dado un error al actualizar
Cuando Angular recibe el fallo
Entonces informa el error y conserva o recupera la etapa persistida.

#### HU04-AC03

Cerrar
Dado una oportunidad abierta
Cuando confirma Ganada con monto o Perdida con motivo
Entonces guarda resultado, autor y fecha y sale del tablero activo.

#### HU04-AC04

Rechazar cierre
Dado una perdida sin motivo o una oportunidad cerrada
Cuando intenta cerrar
Entonces se rechaza sin modificar los datos.

Casos de aceptación vinculados: #177.
Tareas: #139, #140, #141.

## [HU05] Programar y completar seguimientos desde una agenda — #142

Estado leído: **New**; implementación: ninguna. Revisión Azure: 1.

Como Comercial, quiero consultar y completar seguimientos, para evitar olvidar el siguiente contacto.

### Alcance

- Asunto, fecha/hora, contacto obligatorio y oportunidad opcional del mismo contacto. Responsable: usuario actual; estados Pendiente y Completado.

- Guardar UTC y mostrar America/Bogota. Vencidos: antes de ahora; Hoy: desde ahora hasta fin del día; Próximos: desde el día siguiente. Categorías sin solapamiento, orden ascendente.

- Cada usuario ve los propios; Administrador puede consultar todos. Completar conserva historial y retira de pendientes. Sin recordatorios externos.

### Finalización

- Criterios verificados con evidencia y cambios vinculados a esta HU.

- Pruebas pertinentes de reglas y errores; revisión del equipo antes de cerrar.

- Estado New: planificación, sin implementación ni pruebas ejecutadas.

### Trazabilidad al proyecto Basic

- #19 [SCRUM-18] [PBI 304] Programar un próximo seguimiento con fecha, hora, prioridad y responsable

- #23 [SCRUM-22] [PBI 305] Consultar una agenda con seguimientos vencidos, de hoy y próximos

### Criterios de aceptación

#### HU05-AC01

Programar
Dado un contacto y oportunidad opcional coherente
Cuando guarda asunto y fecha/hora válidos
Entonces queda Pendiente con el usuario actual como responsable.

#### HU05-AC02

Validar relación
Dado una fecha inválida u oportunidad de otro contacto
Cuando intenta guardar
Entonces se rechaza con un mensaje que identifica el dato a corregir.

#### HU05-AC03

Clasificar
Dado pendientes en distintos instantes
Cuando abre la agenda en America/Bogota
Entonces cada registro aparece una vez en la categoría correcta y dentro del alcance autorizado.

#### HU05-AC04

Completar
Dado un seguimiento pendiente
Cuando lo completa
Entonces desaparece de pendientes y conserva referencias y fecha de finalización.

Casos de aceptación vinculados: #178.
Tareas: #143, #144.

## [HU06] Registrar interacciones manuales y consultar el historial — #145

Estado leído: **New**; implementación: ninguna. Revisión Azure: 1.

Como Comercial, quiero registrar llamadas, correos y reuniones, para mostrar la evolución del contacto sin mensajería externa.

### Alcance

- Tipo Llamada/Correo/Reunión, fecha/hora, resumen obligatorio, autor y contacto; oportunidad opcional del mismo contacto.

- Historial descendente visible en contacto y oportunidad; datos de actividad en Sales API.

- Solo actividades ya realizadas: rechazar fechas futuras. La ficha mantiene la información del contacto si falla la consulta de actividad.

### Finalización

- Criterios verificados con evidencia y cambios vinculados a esta HU.

- Pruebas pertinentes de reglas y errores; revisión del equipo antes de cerrar.

- Estado New: planificación, sin implementación ni pruebas ejecutadas.

### Trazabilidad al proyecto Basic

- #24 [SCRUM-23] [PBI 306] Registrar manualmente una interacción realizada por llamada, correo o reunión

### Criterios de aceptación

#### HU06-AC01

Registrar
Dado una actividad realizada con contacto y resumen
Cuando guarda la interacción
Entonces conserva autor/fecha y aparece en el historial.

#### HU06-AC02

Relacionar
Dado una oportunidad del contacto
Cuando asocia la actividad
Entonces se consulta desde ambas fichas sin duplicar el registro.

#### HU06-AC03

Rechazar errores
Dado fecha futura, resumen vacío u oportunidad de otro contacto
Cuando intenta guardar
Entonces rechaza y no crea una actividad incompleta.

Casos de aceptación vinculados: #179.
Tareas: #146, #147.

## [HU07] Consultar un dashboard comercial con filtros simples — #148

Estado leído: **New**; implementación: ninguna. Revisión Azure: 1.

Como usuario, quiero consultar indicadores de oportunidades, para entender el estado de ventas con datos consistentes.

### Alcance

- Tarjetas: abiertas, ganadas, perdidas y monto estimado de abiertas; gráfico por etapa. Mismo acceso comercial compartido del MVP.

- Filtros por responsable y fecha de creación, inclusiva según America/Bogota y claramente etiquetada. Aplicar a todos los componentes.

- Agregados en Sales API y montos COP; ceros y vacío útil cuando no hay datos. Sin scoring, exportación ni almacén analítico.

### Finalización

- Criterios verificados con evidencia y cambios vinculados a esta HU.

- Pruebas pertinentes de reglas y errores; revisión del equipo antes de cerrar.

- Estado New: planificación, sin implementación ni pruebas ejecutadas.

### Trazabilidad al proyecto Basic

- #26 [SCRUM-25] [PBI 403] Consultar un dashboard con indicadores básicos y embudo de ventas

- #27 [SCRUM-26] [PBI 404] Filtrar el dashboard por periodo y vendedor

### Criterios de aceptación

#### HU07-AC01

Totales consistentes
Dado un conjunto conocido de oportunidades
Cuando consulta indicadores y gráfico
Entonces coinciden con los datos de prueba sin duplicados.

#### HU07-AC02

Filtros comunes
Dado responsable y rango válido de fecha de creación
Cuando aplica y limpia filtros
Entonces todos los componentes usan la misma intersección y restauran el alcance inicial.

#### HU07-AC03

Vacíos y fechas
Dado un filtro sin resultados o rango invertido
Cuando consulta
Entonces muestra ceros y vacío para el primer caso y exige corregir el rango para el segundo.

Casos de aceptación vinculados: #180.
Tareas: #149, #150.

## [HU08] Presentar una interfaz consistente y datos de demostración — #151

Estado leído: **New**; implementación: ninguna. Revisión Azure: 1.

Como evaluador, quiero recorrer una interfaz cuidada con datos ficticios, para entender el valor del CRM durante una demostración breve.

### Alcance

- Navegación: Dashboard, Contactos, Pipeline y Agenda; fichas coherentes de contacto y oportunidad.

- Sistema visual de tipografía, espaciado, colores y componentes; español y foco de teclado visible.

- Datos idempotentes: dos roles, al menos ocho contactos, oportunidades en cada etapa y ambos resultados, seguimientos vencidos/de hoy/próximos e interacciones.

- Solo datos ficticios; credenciales de demostración para el evaluador fuera del repositorio público.

### Finalización

- Criterios verificados con evidencia y cambios vinculados a esta HU.

- Pruebas pertinentes de reglas y errores; revisión del equipo antes de cerrar.

- Estado New: planificación, sin implementación ni pruebas ejecutadas.

### Criterios de aceptación

#### HU08-AC01

Diseño adaptable
Dado pantallas de 1440 px y 390 px de ancho
Cuando recorre las secciones
Entonces no hay solapamientos y el Kanban permite desplazamiento horizontal dentro de su contenedor.

#### HU08-AC02

Estados útiles
Dado una consulta en carga, vacía o fallida
Cuando se presenta la vista
Entonces muestra progreso, vacío con acción o error recuperable.

#### HU08-AC03

Datos reproducibles
Dado una base de demostración limpia
Cuando prepara datos dos veces
Entonces obtiene el mismo conjunto coherente sin duplicados y puede completar el flujo comercial.

Casos de aceptación vinculados: #181.
Tareas: #152, #153.

## [HU09] Definir dos microservicios y persistencia PostgreSQL en RDS — #154

Estado leído: **New**; implementación: ninguna. Revisión Azure: 1.

Como equipo, quiero definir límites y persistencia, para construir un alcance pequeño compatible con AWS Academy.

### Alcance

- Dos microservicios .NET: Contacts API (autenticación, cuentas precargadas y contactos) y Sales API (oportunidades, seguimientos, interacciones e indicadores). Un frontend Angular. Una instancia Amazon RDS PostgreSQL con bases de datos y usuarios separados por servicio; relaciones entre dominios validadas por contratos HTTP, sin consultas SQL cruzadas. Servicios desplegables y escalables por separado.

- PostgreSQL con EF Core/Npgsql; confirmar versión y tamaño permitidos en la cuenta antes de aprovisionar.

- Contratos HTTP, autenticación, timeouts, errores y migraciones independientes. Cada servicio solo accede a su propia base.

- Contrastar guía privada 181128 y permisos. Stack .NET/Angular autorizado; preparar línea base monolítica mínima y comparación si la rúbrica la exige.

- RDS privado, acceso solo desde servicios, TLS y credenciales externas al código. La instancia compartida es una simplificación académica y un punto compartido de fallo que debe documentarse.

### Finalización

- Criterios verificados con evidencia y cambios vinculados a esta HU.

- Pruebas pertinentes de reglas y errores; revisión del equipo antes de cerrar.

- Estado New: planificación, sin implementación ni pruebas ejecutadas.

### Criterios de aceptación

#### HU09-AC01

Límites verificables
Dado entidades y funcionalidades del MVP
Cuando revisa contratos y diseño
Entonces cada dato tiene un único propietario y las APIs se compilan, migran y despliegan independientemente.

#### HU09-AC02

Persistencia
Dado RDS y usuarios de servicio configurados
Cuando cada API migra, guarda y consulta con su usuario
Entonces los datos sobreviven a reinicios y un usuario no accede a la base del otro servicio.

#### HU09-AC03

Alineación académica
Dado guía 181128 y permisos disponibles
Cuando completa la matriz requisito-evidencia
Entonces registra servicios exigidos, restricciones y si necesita una línea base monolítica.

Casos de aceptación vinculados: #182.
Tareas: #155, #156, #157.

## [HU10] Empaquetar componentes y publicar imágenes versionadas en ECR — #158

Estado leído: **New**; implementación: ninguna. Revisión Azure: 1.

Como equipo, quiero ejecutar componentes en contenedores, para preparar despliegues reproducibles e independientes.

### Alcance

- Contenedores Linux separados para Contacts API, Sales API y frontend Angular estático; configuración externa.

- Construcciones reproducibles, health checks y ejecución local integrada con PostgreSQL compatible. Imágenes ECR por componente identificadas por commit/digest.

- Monorepo con carpetas y builds por componente; servicios independientes, sin fusionarlos en un único contenedor.

### Finalización

- Criterios verificados con evidencia y cambios vinculados a esta HU.

- Pruebas pertinentes de reglas y errores; revisión del equipo antes de cerrar.

- Estado New: planificación, sin implementación ni pruebas ejecutadas.

### Criterios de aceptación

#### HU10-AC01

Ejecutar
Dado una copia limpia y configuración de desarrollo
Cuando construye y ejecuta el entorno documentado
Entonces Angular y ambas APIs funcionan con comprobaciones de salud.

#### HU10-AC02

Versionar
Dado un commit validado
Cuando publica imágenes en ECR
Entonces cada componente tiene commit y digest identificables sin credenciales incrustadas.

Casos de aceptación vinculados: #183.
Tareas: #159, #160.

## [HU11] Desplegar servicios en ECS con balanceador y logs — #161

Estado leído: **New**; implementación: ninguna. Revisión Azure: 1.

Como evaluador, quiero acceder al CRM en AWS, para comprobar servicios operativos con persistencia y observabilidad.

### Alcance

- Servicios ECS por componente y ALB con rutas de APIs y frontend; elegir Fargate o ECS sobre EC2 según permisos del laboratorio.

- RDS privado, reglas de red mínimas y secretos por mecanismos permitidos. HTTPS si hay dominio/certificado disponible; documentar limitaciones académicas.

- CloudWatch para ambas APIs con correlación y retención; health checks. No registrar contraseñas ni tokens.

- Actualizar o aumentar réplicas de una API sin redesplegar la otra; contemplar costos, salida de red y límites del laboratorio.

### Finalización

- Criterios verificados con evidencia y cambios vinculados a esta HU.

- Pruebas pertinentes de reglas y errores; revisión del equipo antes de cerrar.

- Estado New: planificación, sin implementación ni pruebas ejecutadas.

### Criterios de aceptación

#### HU11-AC01

Acceso y datos
Dado ECS, ALB y RDS configurados
Cuando recorre el CRM y reemplaza una tarea ECS
Entonces las rutas funcionan y los datos persisten.

#### HU11-AC02

Independencia y logs
Dado las dos APIs desplegadas
Cuando actualiza o escala una API
Entonces la otra conserva su versión y operación; una petición puede rastrearse en CloudWatch.

#### HU11-AC03

Red restringida
Dado reglas de acceso configuradas
Cuando revisa RDS
Entonces no está expuesto públicamente y solo los servicios autorizados alcanzan su puerto.

Casos de aceptación vinculados: #184.
Tareas: #162, #163, #164.

## [HU12] Automatizar validación y despliegue con rollback — #165

Estado leído: **New**; implementación: ninguna. Revisión Azure: 1.

Como equipo, quiero validar y desplegar cada servicio mediante CI/CD, para demostrar una entrega automatizada y recuperable.

### Alcance

- Código en Azure Repos; CI por componente compila .NET/Angular, ejecuta pruebas y produce imágenes/artefactos versionados.

- Diseño propuesto: Azure Pipelines publica ECR y paquete inmutable de release en S3 con taskdef/AppSpec; CodePipeline consume artefactos compatibles y CodeDeploy despliega ECS blue/green. Confirmar permisos antes de implementar.

- No asumir conexión nativa de Azure Repos a CodePipeline: documentar el puente S3/ECR y correlación commit-digest-ejecución. Evitar carreras con releases inmutables.

- Comprobación antes de cambiar tráfico y rollback ante fallo; actualizar una API sin cambiar la otra. Migraciones de datos compatibles con ambas versiones durante blue/green.

### Finalización

- Criterios verificados con evidencia y cambios vinculados a esta HU.

- Pruebas pertinentes de reglas y errores; revisión del equipo antes de cerrar.

- Estado New: planificación, sin implementación ni pruebas ejecutadas.

### Criterios de aceptación

#### HU12-AC01

Bloquear fallos
Dado un cambio con build o pruebas fallidas
Cuando ejecuta CI
Entonces informa el fallo y no publica una release desplegable.

#### HU12-AC02

Desplegar versión
Dado un commit válido de una API
Cuando publica release y ejecuta CodePipeline
Entonces CodeDeploy comprueba salud y cambia tráfico con trazabilidad; la otra API mantiene su versión.

#### HU12-AC03

Rollback
Dado la nueva versión falla una comprobación de salud
Cuando aplica la política de recuperación
Entonces el tráfico sigue o vuelve a una versión sana y se conserva la evidencia del fallo.

Casos de aceptación vinculados: #185.
Tareas: #166, #167, #168.

## [HU13] Preparar evidencias y demostración del entregable — #169

Estado leído: **New**; implementación: ninguna. Revisión Azure: 2.

Como estudiante, quiero presentar el CRM y evidencias técnicas, para demostrar los resultados funcionales y de AWS.

### Alcance

- Guion: acceso, contacto, oportunidad, Kanban, seguimiento, interacción, cierre y dashboard.

- Diagrama final y matriz requisito-evidencia; capturas/video de RDS/ECS/ECR/CloudWatch/CI-CD/rollback y escalado independiente. Comparación monolito/microservicios si se exige.

- Instrucciones reproducibles, limitaciones, costos estimados y limpieza documentada. Eliminar recursos requiere autorización al momento de ejecutarlo.

### Finalización

- Criterios verificados con evidencia y cambios vinculados a esta HU.

- Pruebas pertinentes de reglas y errores; revisión del equipo antes de cerrar.

- Estado New: planificación, sin implementación ni pruebas ejecutadas.

### Criterios de aceptación

#### HU13-AC01

Recorrido funcional
Dado entorno y datos de demo
Cuando realiza el guion
Entonces completa el ciclo comercial y los indicadores coinciden con los cambios.

#### HU13-AC02

Evidencia verificable
Dado la matriz y ejecuciones de pipeline
Cuando prepara la entrega
Entonces cada requisito tiene evidencia concreta y enlaces válidos, con versión identificada.

#### HU13-AC03

Operación académica
Dado los recursos del proyecto
Cuando revisa costos e instrucciones
Entonces documenta estimaciones, límites y limpieza sin borrar recursos en esta fase.

Casos de aceptación vinculados: #186.
Tareas: #170, #171.

## [HU14] Integrar WhatsApp empresarial después del entregable — #172

Estado leído: **Removed**; implementación: ninguna. Revisión Azure: 2.

Consolida conexión, webhooks, envío/recepción y asociación de conversaciones. Aplazado por dependencias de configuración y credenciales externas. El MVP usa interacciones manuales; no presenta chat simulado como integración real.
Fuera del MVP. Removed significa excluido/aplazado, no implementado. Tareas y pruebas originales permanecen como archivo de referencia; no se generan tareas activas.

### Trazabilidad al proyecto Basic

- #16 [SCRUM-15] [PBI 301] Vincular un canal empresarial de WhatsApp al CRM

- #20 [SCRUM-19] [PBI 302] Enviar y recibir mensajes de texto de WhatsApp desde la ficha del cliente

- #21 [SCRUM-20] [PBI 303] Asociar una conversación de WhatsApp con un prospecto, cliente y oportunidad

- #31 [SCRUM-30] Configurar canal WhatsApp Business

- #33 [SCRUM-32] Implementar conexión y webhook de WhatsApp

- #34 [SCRUM-33] Probar conexión, errores y reintentos

- #35 [SCRUM-34] Persistir configuración y estado del canal

- #36 [SCRUM-35] Implementar envío de mensajes de texto

- #37 [SCRUM-36] Implementar recepción mediante webhook

- #38 [SCRUM-37] Persistir mensajes y estados

- #39 [SCRUM-38] Definir reglas de asociación de conversaciones

- #40 [SCRUM-39] Integrar conversación en la ficha del cliente

- #41 [SCRUM-40] Construir selección y visualización de asociaciones

- #42 [SCRUM-41] Implementar relación conversación-entidades

- #46 [SCRUM-45] Probar asociaciones y casos límite

### Criterios de aceptación

#### HU14-AC01

Antes de reincorporar: confirmar necesidad, dependencias y nuevos criterios con el equipo. No condiciona la entrega universitaria.

Casos de aceptación vinculados: ninguno; fuera del MVP.
Tareas: ninguna activa.

## [HU15] Incorporar Lead Score después del entregable — #173

Estado leído: **Removed**; implementación: ninguna. Revisión Azure: 2.

Conserva puntaje y explicación de reglas como evolución posible. El MVP utiliza prioridad manual sin motor de scoring.
Fuera del MVP. Removed significa excluido/aplazado, no implementado. Tareas y pruebas originales permanecen como archivo de referencia; no se generan tareas activas.

### Trazabilidad al proyecto Basic

- #22 [SCRUM-21] [PBI 401] Visualizar un Lead Score calculado mediante reglas para cada oportunidad

- #43 [SCRUM-42] Definir reglas y fórmula del Lead Score

- #44 [SCRUM-43] Persistir y exponer el puntaje

- #45 [SCRUM-44] Implementar cálculo del Lead Score

- #47 [SCRUM-46] Validar reglas y resultados del scoring

### Criterios de aceptación

#### HU15-AC01

Antes de reincorporar: confirmar necesidad, dependencias y nuevos criterios con el equipo. No condiciona la entrega universitaria.

Casos de aceptación vinculados: ninguno; fuera del MVP.
Tareas: ninguna activa.

## Ambigüedades y dependencias de verificación

- D-01: HU01-AC03 exige denegar una reasignación real de HU03, pero HU03 depende de HU01. Entregar fundamento de autenticación primero y mantener HU01 pendiente de esa comprobación hasta HU03; no declarar un endpoint inexistente como evidencia.
- D-02: HU02-AC03 verifica conservación de relaciones creadas por HU03/HU06. Completar esta comprobación cuando existan esas relaciones; no eliminar el criterio.
- D-03: HU09 abarca diseño y RDS real, aunque otras HU necesitan sus fundamentos antes del despliegue. Dividir incrementos HU09-A y HU09-B sin cerrar anticipadamente la historia completa.
- D-04: HU08 incluye seeds de entidades de HU03–HU06 y reglas visuales reutilizadas desde HU01. Aplicar tokens desde cada HU y cerrar HU08 solo al validar todo el recorrido.
- D-05: permiso de edición comercial compartida, contacto de oportunidad mutable, catálogo de origen y límites no están completamente fijados. Resolver P-01/P-02/P-03 antes del código afectado.
- D-06: el backlog permite HTTPS condicionado al laboratorio; la propuesta refuerza el requisito para login público. Aprobar P-05 y comprobar G-03 antes de publicar.
- D-07: la disponibilidad de CodeDeploy, roles y credenciales temporales en el laboratorio sigue abierta. HU12 se bloquea si no se puede ejecutar el puente de releases; no simular un pipeline terminado.
- D-08: HU03-AC01 dice «contacto y usuario activos», pero HU02 no define estado activo/inactivo para contactos. P-08 propone interpretar contacto como existente y usuario como activo; requiere aprobación y conserva el texto original del criterio hasta reconciliarlo con Azure.

