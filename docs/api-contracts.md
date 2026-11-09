# Contratos REST — ARCH-001 v1

Propuesta normativa al aprobarse. Rutas y valores exactos; no hay endpoints de borrado ni CRUD de usuarios.
Base Contacts `C=/api/contacts/v1`; Sales `S=/api/sales/v1`. El ALB conserva el path completo, sin rewrite.
Todos requieren JWT salvo login y health; OpenAPI es público solo en Development/Demo aprobado.

## Convenciones y tipos

- UTF-8 `application/json`, camelCase, UUID string, enums string case-sensitive. Rechazar miembros JSON
  desconocidos en DTO de escritura, tipos inválidos y fechas sin offset cuando se exige un instante.
- Instante `2026-11-08T15:00:00Z`; aceptar offset válido, normalizar UTC. Fecha `2026-11-30`.
- Importes JSON number, decimal exacto en backend; UI solo presenta `Intl.NumberFormat('es-CO', currency COP)`.
- Nulos explícitos para campos opcionales. Id, actor, version inicial, fechas de auditoría y campos de cierre
  los asigna el servidor; no recibir entidades EF ni devolver password_hash.
- Lectura/listas 200; creación 201 con Location del detalle y recurso creado. Actualización 200 con
  recurso y nueva version. Sin envelope `success/data`; arrays y objetos tienen tipos documentados.
- `Page<T> = {items:T[],page:int,pageSize:int,totalCount:int}`. page>=1; pageSize=20 predeterminado,
  máximo 100, valores fuera de rango →400. Orden estable, desempate id; no aceptar sort SQL libre.
- Los recursos mutables devuelven `version:integer` (int64, comienza en 1); cada comando de actualización
  debe enviar la versión leída. Desactualizada →409 `concurrency_conflict`; nunca sobreescribir silenciosamente.
- OpenAPI 3.1.0 con nullability correcta, required/enums/formatos/rangos, respuestas de error y security scheme
  bearer. Publicación: `/api/contacts/openapi/v1.json` y `/api/sales/openapi/v1.json`.

### DTO comunes (todos los campos indicados son obligatorios salvo `?`)

| Nombre | Campos |
|---|---|
| UserSummary | id, displayName, role:Administrator\|Commercial, isActive |
| Contact | id, name, type:Prospect\|Customer, email:string\|null, phone:string\|null, source, createdAt, updatedAt, version |
| ContactReference | id, name, type |
| Opportunity | id, contactId, title, estimatedAmount, expectedCloseDate, ownerUserId, priority:High\|Medium\|Low, stage:New\|Contacted\|Proposal\|Won\|Lost, createdByUserId, createdAt, updatedAt, closedAt:null\|instant, closedByUserId:null\|uuid, finalAmount:null\|number, lossReason:null\|string, version |
| FollowUp | id, contactId, opportunityId:uuid\|null, subject, scheduledAt, ownerUserId, status:Pending\|Completed, createdAt, completedAt:null\|instant, completedByUserId:null\|uuid, version |
| Interaction | id, contactId, opportunityId:uuid\|null, type:Call\|Email\|Meeting, occurredAt, summary, authorUserId, createdAt |

`source` = Web/Referral/WhatsApp/Call/Other, catálogo propuesto P-03. Límites de texto/importes en
[modelo](data-model.md); los contratos OpenAPI deberán expresarlos. Las etiquetas españolas viven en Angular.

## Contacts: acceso, contactos y directorio

| Método y ruta | Entrada | Salida / regla | HU |
|---|---|---|---|
| POST C/auth/login | `{email,password}` | `{accessToken,tokenType:"Bearer",expiresIn:900,user:UserSummary}`; 401 genérico para error/inactivo | HU01 |
| GET C/auth/me | Sin body | UserSummary de la cuenta del token; verificar activa en Contacts | HU01 |
| GET C/contacts | `q?`, `type?`, `page?`, `pageSize?` | Page<Contact>, búsqueda nombre/email/teléfono normalizado, createdAt DESC,id | HU02 |
| GET C/contacts/{id} | UUID | Contact; 404 si no existe | HU02 |
| POST C/contacts | ContactWrite | Contact version=1, 201 | HU02 |
| PUT C/contacts/{id} | ContactWrite + `{version}` | Contact actualizado; conserva id/createdAt/relaciones | HU02 |
| GET C/references/contacts/{id} | UUID | ContactReference; 404 real, 401/403 según token | HU03/HU05/HU06 |
| POST C/references/contacts/lookup | `{ids:uuid[]}` de 1..100 únicos | `{items:ContactReference[],missingIds:uuid[]}`; consulta sin escritura | HU03/HU04 |
| GET C/references/users | `activeOnly:boolean=true` | `{items:UserSummary[]}`; no email ni credenciales | HU03/HU07 |
| GET C/references/users/{id} | UUID | UserSummary con isActive, 404 si no existe | HU03 |

`ContactWrite={name,type,email:string|null,phone:string|null,source,acceptPotentialDuplicate?:boolean=false}`.
Correo o teléfono debe ser válido y no vacío; el otro puede ser null. Para advertencia de duplicado:

1. Normalizar y buscar coincidencia por email **o** teléfono, excluyendo el mismo id al editar.
2. Si hay candidatos y acceptPotentialDuplicate=false, devolver 409 `potential_duplicate` con
   `duplicateContactIds:uuid[]`, sin guardar. Angular ofrece revisar y confirmar explícitamente.
3. Reenviar el mismo formulario con acceptPotentialDuplicate=true permite guardar; sigue validándose todo
   lo demás. La regla advierte, no impone unicidad. Un segundo submit automático de la UI no confirma.

La petición que lee referencias es autenticada. No convertir estos endpoints en endpoints anónimos
«internos» ni permitir seleccionar columnas arbitrarias.

## Sales: oportunidades, Kanban y dashboard

| Método y ruta | Entrada | Salida / regla | HU |
|---|---|---|---|
| GET S/opportunities | `stage?`, `openOnly?`, `ownerUserId?`, `priority?`, `contactId?`, `createdFrom?`, `createdTo?`, paginación | Page<Opportunity>, createdAt DESC,id | HU03/HU04 |
| GET S/opportunities/{id} | UUID | Opportunity o404 | HU03/HU04 |
| POST S/opportunities | OpportunityCreate | Opportunity New, 201 | HU03 |
| PUT S/opportunities/{id} | OpportunityUpdate | Solo abierta, 200 | HU03 |
| PATCH S/opportunities/{id}/owner | `{ownerUserId,version}` | Solo Admin + abierta + usuario activo; 200 | HU03 |
| GET S/opportunities/{id}/owner-changes | UUID y paginación | Page<{id,previousOwnerUserId,newOwnerUserId,changedByUserId,changedAt}>, changedAt DESC,id | HU03 |
| PATCH S/opportunities/{id}/stage | `{stage:New\|Contacted\|Proposal,version}` | Movimiento entre etapas activas; cerrada →409 | HU04 |
| POST S/opportunities/{id}/close | OpportunityClose | Cierra una vez, 200; segunda vez →409 `opportunity_closed` | HU04 |
| GET S/dashboard | `ownerUserId?`, `createdFrom?`, `createdTo?` | Dashboard, una misma cohorte para todos los cálculos | HU07 |

- `OpportunityCreate={contactId,title,estimatedAmount,expectedCloseDate,ownerUserId?:uuid,priority?:High|Medium|Low}`.
  Prioridad omitida→Medium; owner omitido→sub. Comercial con owner distinto de sub→403; Admin puede elegir.
- `OpportunityUpdate={title,estimatedAmount,expectedCloseDate,priority,version}`. Contacto inmutable según
  P-02; reasignación tiene endpoint propio. Ni stage, ni campos de cierre, ni actor se aceptan aquí.
- `OpportunityClose={result:Won|Lost,finalAmount:number|null,lossReason:string|null,version}`. Won exige
  monto no negativo y motivo null; Lost exige motivo no vacío y monto null. El servidor fija actor/fecha.
  Cambiar owner o stage al valor actual, en una oportunidad abierta, devuelve 200 sin nueva versión ni
  auditoría solo después de validar permiso, datos y versión. La interpretación de contacto «activo»
  pendiente en HU03-AC01 se resuelve con P-08 antes de implementar su validación.
- `Dashboard={openCount,wonCount,lostCount,openEstimatedAmount,currency:"COP",byStage:[{stage,count}],filters:{ownerUserId:null|uuid,createdFrom:null|date,createdTo:null|date},generatedAt}`.
  byStage contiene las cinco etapas en orden New,Contacted,Proposal,Won,Lost, incluso si count=0.

Filtros temporales siempre **fecha de creación**: convertir inicio local inclusivo y día siguiente al fin
exclusivo a UTC. Rango invertido→400. Open cuenta tres etapas activas; monto abierto suma estimatedAmount
solo de ellas. Ganadas/perdidas cuentan stage actual dentro de la misma cohorte. No filtrar esas tarjetas
por closedAt ni mezclar montos finales con estimados. Dashboard sin filas: ceros, cinco categorías en cero.
Ejecutar sus agregados en una sola sentencia o transacción de lectura repeatable-read para evitar desfases.

Para Kanban, Angular solicita cada etapa activa con paginación propia y «Cargar más»; muestra total y
cantidad cargada por columna. Mantener filtros de responsable/prioridad comunes. Nunca presentar las
primeras 100 filas como si fueran el conjunto completo. Cerradas se consultan por filtro/listado, fuera de columnas.

## Sales: agenda e interacciones

| Método y ruta | Entrada | Salida / regla | HU |
|---|---|---|---|
| POST S/follow-ups | `{contactId,opportunityId:uuid|null,subject,scheduledAt}` | FollowUp Pending, responsable sub, 201 | HU05 |
| GET S/follow-ups | `bucket:Overdue\|Today\|Upcoming\|Completed`, `scope:Mine\|All=Mine`, paginación | Page<FollowUp>; All solo Admin, Commercial→403 | HU05 |
| GET S/follow-ups/{id} | UUID | FollowUp solo propietario o Admin | HU05 |
| POST S/follow-ups/{id}/complete | `{version}` | Completa propio o Admin; 200 con FollowUp | HU05 |
| POST S/interactions | `{contactId,opportunityId:uuid|null,type,occurredAt,summary}` | Interaction, autor sub, 201 | HU06 |
| GET S/interactions | `contactId?`, `opportunityId?`, paginación | Exigir al menos un filtro; Page<Interaction>, occurredAt DESC,id | HU06 |

Un seguimiento con fecha pasada válida es permitido; debe poder aparecer vencido. Obtener `now` una
sola vez por consulta: Overdue `<now`, Today `[now,inicioMañanaBogota)`, Upcoming `>=inicioMañanaBogota`;
solo Pending. Completed usa completedAt DESC,id y no mezcla pendientes. Pendientes orden scheduledAt ASC,id.
La agenda muestra una pestaña de categoría a la vez; al cambiar se consulta de nuevo y no se concatenan
respuestas de categorías calculadas con distintos instantes. No construir una vista simultánea con tres
relojes independientes y anunciar que es una foto consistente.
Un Commercial no puede leer/completar un seguimiento ajeno, tampoco usando UUID conocido.
Completar uno ya completado es idempotente: 200 sin cambiar actor/fecha; para Pending, version desactualizada→409.

Contacto obligatorio y oportunidad opcional del mismo contacto. Validar existencia externa en comandos;
la coherencia de oportunidad/contacto también la refuerza una FK local compuesta. Historial por contacto
incluye actividades sin oportunidad y con cualquiera de sus oportunidades. Ambas vistas devuelven el mismo id.
Sin endpoints de edición/borrado de interacciones ni reprogramación/reasignación de seguimientos en el MVP.

## Dependencia HTTP Sales → Contacts

`ContactsDirectoryClient` usa URL fija configurada, nunca una URL del usuario. En AWS: misma URL HTTPS
pública del ALB + base Contacts. En Compose: `http://contacts:8080/api/contacts/v1` en red local.
Pasar el Bearer del solicitante y traceparent, sin redirects automáticos. Timeout total de la validación
remota por comando: 3 segundos con CancellationToken. Sin retry automático ni escrituras antes de validar.

404 de contacto/usuario→422 `invalid_reference`; usuario inactivo→422 `inactive_owner`;
401/403 remoto→mismo estado apropiado y sin escritura. Timeout/5xx/conexión fallida→503
`directory_unavailable`. No convertir dependencia caída en «contacto inexistente». Consultas de Sales
no dependen del directorio; nombres se resuelven en Angular por lote y soportan un fallback visible por id.

## Errores y salud

ProblemDetails: `{type:"urn:smartsales:problem:<code>",title,status,detail,instance,code,traceId,errors?:{campo:string[]}}`.
detail no contiene excepciones, SQL, credenciales o respuestas internas. Código estable, mensaje español.

| Estado | Uso |
|---|---|
| 400 | JSON/tipo/formato/enum/rango/paginación inválido, validaciones de formulario |
| 401 / 403 | Token ausente/inválido/expirado; permiso o alcance insuficiente |
| 404 | Recurso propio de la API no encontrado |
| 409 | potential_duplicate, concurrency_conflict, opportunity_closed, transición incompatible |
| 422 | Referencia externa inexistente/inactiva o asociación de oportunidad incorrecta |
| 429 | Límite de login; Retry-After cuando sea calculable |
| 500 / 503 | Fallo inesperado con traceId; dependencia/DB temporalmente no disponible |

Health por API: `C/health/live`, `C/health/ready`, `S/health/live`, `S/health/ready`. Live solo proceso;
ready comprueba su propia DB y configuración. 200 `{status:"ok"}` o503 genérico; sin detalles sensibles.
Sales ready no comprueba Contacts, para no convertir un fallo de directorio en caída de sus lecturas.
