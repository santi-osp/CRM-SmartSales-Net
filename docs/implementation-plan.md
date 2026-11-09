# Roadmap de implementación — ARCH-001 v1

**Aprobado y autorizado el 2026-11-08.** Leer [approval](approval.md).
Se priorizan incrementos verticales y resultados reales. Los gates externos siguen pendientes de verificación.

## Secuencia y gates

1. P0: aprobar arquitectura/roadmap y aclaraciones P-01..P-08.
2. HU09-A: fundamentos/documentación y preparación local autorizada; no exige cerrar toda HU09.
3. HU01 → HU02 → HU03 → HU04 → HU05 → HU06 → HU07 → HU08. Los estilos se aplican desde la primera pantalla; HU08 integra y verifica el conjunto.
4. HU10: contenedores de los tres componentes.
5. HU09-B: validar rúbrica, cuenta, datos reales RDS y aislamiento; resolver G-01/G-02.
6. HU11: despliegue tras HTTPS/certificado G-03.
7. HU12: pipeline con capacidad/credenciales G-04.
8. HU13: evidencias y entrega final. HU14/HU15 permanecen fuera.

La prioridad de Boards no sustituye dependencias. HU09-A/B son incrementos de una misma HU; no nuevas
historias creadas ni cambios de estado remotos en esta fase. Las dependencias HU09→HU01/02 de Boards
significan fundamento técnico disponible; HU09 solo puede cerrarse con su prueba RDS y rúbrica.

## Dependencias de verificación que no se deben ocultar

HU01-AC03 se comprueba completamente con la reasignación real de HU03; HU02-AC03 con oportunidades e
interacciones de HU03/HU06. Sus fundamentos pueden entregarse antes, pero la historia conserva estado
pendiente de verificación/Activa hasta ese cierre. No eliminar criterios ni crear endpoints falsos para
aparentar cobertura. Si se quiere transformar la granularidad de Boards, proponer el cambio al usuario.

La implementación de HU01 incluye soporte mínimo común (configuración, errores, auth y proyectos) que
requiere ese login real. No implementar endpoints comerciales de otra HU sin autorización. Los tests de
políticas iniciales no sustituyen la comprobación posterior en endpoint comercial real.

## Plan por HU

## HU09 — #154

**Objetivo:** HU09-A: cerrar el diseño aprobado y preparar fundamentos locales. HU09-B: verificar persistencia y permisos RDS antes de HU11.

**Predecesoras en Boards:** ninguna. Orden/gates adicionales: secuencia anterior y decisiones P aplicables.

**Backend:** A: límites, contratos y configuración; bootstrap de DbContext/ownership al autorizarlo. B: conexiones TLS y migraciones separadas, sin cambiar límites.

**Angular:** A: contrato de integración y convenciones; no construir pantallas desconectadas.

**Datos:** A: modelo y migraciones locales según HU funcionales; B: bases/usuarios separados en RDS y prueba de aislamiento.

**Integración:** A: herramientas locales y diseño aprobado; B: cuenta AWS autorizada y gates G-01/G-02.

**Aceptación:** HU09-AC01, HU09-AC02, HU09-AC03; texto íntegro en [requirements](requirements.md). No modificarlo para ajustar implementación.

**Pruebas:** Límites, revisiones de contrato, migraciones limpias, persistencia tras reinicio y negativa de acceso cruzado. Rúbrica validada con evidencia. Caso Azure: #182.

**Resultado verificable:** A habilita empezar HU01/HU02; HU09 completa solo tras AC01–AC03, incluida RDS real. No declarar todo terminado al aprobar documentación.

**Tareas existentes:** [#155](https://dev.azure.com/mengrau/CRM%20SmartSales%20.Net/_workitems/edit/155), [#156](https://dev.azure.com/mengrau/CRM%20SmartSales%20.Net/_workitems/edit/156), [#157](https://dev.azure.com/mengrau/CRM%20SmartSales%20.Net/_workitems/edit/157).

## HU01 — #129

**Objetivo:** Login real, sesión y políticas base. Bootstrap solo de los proyectos necesarios; crear lock de herramientas y health/configuración mínima de ambas APIs.

**Predecesoras en Boards:** HU09. Orden/gates adicionales: secuencia anterior y decisiones P aplicables.

**Backend:** Auth/hasher/JWT en Contacts, validación JWT/políticas en ambas y ProblemDetails. Endpoints login/me/directorio de usuarios.

**Angular:** Login, memoria de sesión, guard, interceptor y layout mínimo coherente con tokens.

**Datos:** Contacts users y seed de cuentas por secreto; migración propia.

**Integración:** Angular→Contacts; claves públicas consistentes para Sales.

**Aceptación:** HU01-AC01, HU01-AC02, HU01-AC03; texto íntegro en [requirements](requirements.md). No modificarlo para ajustar implementación.

**Pruebas:** Cuenta activa/inactiva, clave incorrecta, JWT inválido/expirado, autorización base y logout. HU01-AC03 se revalida con endpoint real de HU03. Caso Azure: #174.

**Resultado verificable:** Ingresar y salir con cuentas reales. Si aún falta HU03, mantener HU01 pendiente de verificación de AC03; no cerrar completa.

**Tareas existentes:** [#130](https://dev.azure.com/mengrau/CRM%20SmartSales%20.Net/_workitems/edit/130), [#131](https://dev.azure.com/mengrau/CRM%20SmartSales%20.Net/_workitems/edit/131).

## HU02 — #132

**Objetivo:** Primer CRUD vertical de contactos con búsqueda y advertencia de duplicado.

**Predecesoras en Boards:** HU09. Orden/gates adicionales: secuencia anterior y decisiones P aplicables.

**Backend:** Contacts CRUD y referencias, normalización, búsqueda parametrizada y concurrencia.

**Angular:** Lista/formulario/ficha; confirmar duplicado; errores de campo y loading/empty/error.

**Datos:** contacts, constraints e índices; migración Contacts.

**Integración:** UI→Contacts, DTO/tipos OpenAPI.

**Aceptación:** HU02-AC01, HU02-AC02, HU02-AC03; texto íntegro en [requirements](requirements.md). No modificarlo para ajustar implementación.

**Pruebas:** Correo/teléfono, duplicados confirmados, búsquedas y conflictos de versión. Conservación de relaciones se comprueba después con HU03/HU06. Caso Azure: #175.

**Resultado verificable:** Crear/editar y encontrar un contacto persistente desde Angular; dejar AC03 pendiente hasta contar con relaciones reales.

**Tareas existentes:** [#133](https://dev.azure.com/mengrau/CRM%20SmartSales%20.Net/_workitems/edit/133), [#134](https://dev.azure.com/mengrau/CRM%20SmartSales%20.Net/_workitems/edit/134).

## HU03 — #135

**Objetivo:** Oportunidades reales con responsable y prioridad manual.

**Predecesoras en Boards:** HU01, HU02. Orden/gates adicionales: secuencia anterior y decisiones P aplicables.

**Backend:** Sales endpoints, validación HTTP de referencias, permisos de asignación, auditoría y versiones.

**Angular:** Selector de contacto, formulario, detalle, filtros y selector Admin; nombres por lookup de lote.

**Datos:** opportunities y opportunity_owner_changes; migración Sales.

**Integración:** SalesíContacts con timeout/token; Angular→ambas.

**Aceptación:** HU03-AC01, HU03-AC02, HU03-AC03; texto íntegro en [requirements](requirements.md). No modificarlo para ajustar implementación.

**Pruebas:** Activo/inactivo/inexistente, monto/fechas,403 Comercial, filtros, concurrencia y503 sin escritura. Ejecutar HU01-AC03 real y primera parte HU02-AC03. Caso Azure: #176.

**Resultado verificable:** Registrar y reasignar desde UI, observar persistencia y rechazo real de Comercial al reasignar.

**Tareas existentes:** [#136](https://dev.azure.com/mengrau/CRM%20SmartSales%20.Net/_workitems/edit/136), [#137](https://dev.azure.com/mengrau/CRM%20SmartSales%20.Net/_workitems/edit/137).

## HU04 — #138

**Objetivo:** Tablero y cierre completo con datos reales.

**Predecesoras en Boards:** HU03. Orden/gates adicionales: secuencia anterior y decisiones P aplicables.

**Backend:** Comandos de etapa/cierre, transacción y validaciones de estado/monto/motivo.

**Angular:** Kanban por columnas, carga adicional, alternativa teclado, rollback de movimiento y modal de cierre.

**Datos:** Extensión aditiva si HU03 no incluyó aún campos/checks de cierre; nunca eliminar datos.

**Integración:** Angular→Sales; lookup contactosíContacts.

**Aceptación:** HU04-AC01, HU04-AC02, HU04-AC03, HU04-AC04; texto íntegro en [requirements](requirements.md). No modificarlo para ajustar implementación.

**Pruebas:** Todos los movimientos activos, persistencia tras recarga, fallo de API, conflicto, ambos resultados y rechazo de segundo cierre. Caso Azure: #177.

**Resultado verificable:** Mover, recargar, ganar/perder y ver que cerradas salen del tablero; los errores no falsifican el movimiento.

**Tareas existentes:** [#139](https://dev.azure.com/mengrau/CRM%20SmartSales%20.Net/_workitems/edit/139), [#140](https://dev.azure.com/mengrau/CRM%20SmartSales%20.Net/_workitems/edit/140), [#141](https://dev.azure.com/mengrau/CRM%20SmartSales%20.Net/_workitems/edit/141).

## HU05 — #142

**Objetivo:** Agenda con horarios de negocio y finalización.

**Predecesoras en Boards:** HU02, HU01. Orden/gates adicionales: secuencia anterior y decisiones P aplicables.

**Backend:** Seguimientos, clasificación con TimeProvider y alcance SQL propietario/Admin.

**Angular:** Agenda, alta, completados y conversión Luxon de America/Bogota.

**Datos:** follow_ups, FK compuesta y checks/índices; migración Sales.

**Integración:** Validar contactos y oportunidad; cliente Sales y directorio.

**Aceptación:** HU05-AC01, HU05-AC02, HU05-AC03, HU05-AC04; texto íntegro en [requirements](requirements.md). No modificarlo para ajustar implementación.

**Pruebas:** Límites now/medianoche, reloj fijo, navegador en otra zona, UUID ajeno, Admin All, completar repetido, relación inválida y503 sin escritura. Caso Azure: #178.

**Resultado verificable:** Crear un seguimiento y verlo en la categoría correcta; completar desde UI y conservar su historial.

**Tareas existentes:** [#143](https://dev.azure.com/mengrau/CRM%20SmartSales%20.Net/_workitems/edit/143), [#144](https://dev.azure.com/mengrau/CRM%20SmartSales%20.Net/_workitems/edit/144).

## HU06 — #145

**Objetivo:** Historial manual integrado a fichas.

**Predecesoras en Boards:** HU01, HU02. Orden/gates adicionales: secuencia anterior y decisiones P aplicables.

**Backend:** Append/read de interacciones y consultas paginadas por contacto/oportunidad.

**Angular:** Formulario y timeline compartidos con recuperación de error parcial.

**Datos:** interactions y FK compuesta; migración Sales.

**Integración:** Angular compone ficha Contacts + actividad Sales; no endpoint agregador nuevo.

**Aceptación:** HU06-AC01, HU06-AC02, HU06-AC03; texto íntegro en [requirements](requirements.md). No modificarlo para ajustar implementación.

**Pruebas:** Fecha futura, resumen, asociación incorrecta, autor real, mismo id en ambas fichas y ficha utilizable si falla actividad. Completar HU02-AC03. Caso Azure: #179.

**Resultado verificable:** Registrar una interacción desde una ficha y verla en ambas sin duplicar datos.

**Tareas existentes:** [#146](https://dev.azure.com/mengrau/CRM%20SmartSales%20.Net/_workitems/edit/146), [#147](https://dev.azure.com/mengrau/CRM%20SmartSales%20.Net/_workitems/edit/147).

## HU07 — #148

**Objetivo:** Indicadores coherentes con filtros visibles.

**Predecesoras en Boards:** HU03. Orden/gates adicionales: secuencia anterior y decisiones P aplicables.

**Backend:** Agregados SQL coherentes por cohorte de creación, zona horaria y responsable.

**Angular:** Tarjetas, gráfico sencillo accesible y filtros/limpiar.

**Datos:** Sin tabla de dashboard; usar oportunidades/índices existentes.

**Integración:** Angular→Sales y listado mínimo de responsablesíContacts.

**Aceptación:** HU07-AC01, HU07-AC02, HU07-AC03; texto íntegro en [requirements](requirements.md). No modificarlo para ajustar implementación.

**Pruebas:** Dataset conocido, suma/conteo por etapa, filtros combinados, ceros, rango inválido y límites temporales. Caso Azure: #180.

**Resultado verificable:** Cambiar/cerrar una oportunidad y comprobar los nuevos indicadores al refrescar.

**Tareas existentes:** [#149](https://dev.azure.com/mengrau/CRM%20SmartSales%20.Net/_workitems/edit/149), [#150](https://dev.azure.com/mengrau/CRM%20SmartSales%20.Net/_workitems/edit/150).

## HU08 — #151

**Objetivo:** Pulir el sistema visual y preparar demo reproducible sin sustituir APIs.

**Predecesoras en Boards:** HU01. Orden/gates adicionales: secuencia anterior y decisiones P aplicables.

**Backend:** Completar solo seeds idempotentes y campos/errores compartidos ya previstos; sin backend falso.

**Angular:** Aplicar/validar navegación, tokens, responsive, teclado, cargas/errores/vacíos en todas las secciones.

**Datos:** Seeds ficticios en ambas bases, UUID fijos y modo Demo explícito; no nuevos dominios.

**Integración:** Frontend real conectado a las dos APIs y sus datos.

**Aceptación:** HU08-AC01, HU08-AC02, HU08-AC03; texto íntegro en [requirements](requirements.md). No modificarlo para ajustar implementación.

**Pruebas:** 1440/390, teclado y foco, formularios, contraste medido, seeds repetidos y recorrido funcional real. Caso Azure: #181.

**Resultado verificable:** Al menos ocho contactos y estados requeridos; interfaz coherente y demo repetible.

**Tareas existentes:** [#152](https://dev.azure.com/mengrau/CRM%20SmartSales%20.Net/_workitems/edit/152), [#153](https://dev.azure.com/mengrau/CRM%20SmartSales%20.Net/_workitems/edit/153).

## HU10 — #158

**Objetivo:** Empaquetar los componentes ya funcionales y publicar imágenes trazables.

**Predecesoras en Boards:** HU09, HU03, HU02. Orden/gates adicionales: secuencia anterior y decisiones P aplicables.

**Backend:** Dockerfiles Linux por API y bundle de migración en imagen; health y cancelación.

**Angular:** Build producción y Nginx no root, rutas SPA y config de ambiente sin secretos.

**Datos:** PostgreSQL 17 de desarrollo en Compose; no reemplazar RDS por DB en contenedor en AWS.

**Integración:** Compose y ECR; debe estar autorizado publicar imágenes.

**Aceptación:** HU10-AC01, HU10-AC02; texto íntegro en [requirements](requirements.md). No modificarlo para ajustar implementación.

**Pruebas:** Build limpio, health, flujo integrado, arranque sin secrets reales, imagen por digest y descarga ECR. Caso Azure: #183.

**Resultado verificable:** Tres imágenes separadas y un entorno integrado reproducible; no servicios fusionados.

**Tareas existentes:** [#159](https://dev.azure.com/mengrau/CRM%20SmartSales%20.Net/_workitems/edit/159), [#160](https://dev.azure.com/mengrau/CRM%20SmartSales%20.Net/_workitems/edit/160).

## HU11 — #161

**Objetivo:** Hacer operativo el despliegue académico con persistencia y logs.

**Predecesoras en Boards:** HU10, HU09. Orden/gates adicionales: secuencia anterior y decisiones P aplicables.

**Backend:** Configuración ECS/health y logs, sin nuevo negocio.

**Angular:** Web servido por ALB, rutas y endpoint real de APIs; HTTPS.

**Datos:** RDS comprobado en HU09-B; cuentas runtime/migrador, TLS y backups configurados según permisos.

**Integración:** CloudFormation/Fargate/ALB/ECR/RDS/SSM/CloudWatch con gates G-01/G-02/G-03 resueltos.

**Aceptación:** HU11-AC01, HU11-AC02, HU11-AC03; texto íntegro en [requirements](requirements.md). No modificarlo para ajustar implementación.

**Pruebas:** Recorrido público seguro, persistencia tras reemplazo de tarea, RDS privado, logs y despliegue/escala independiente. Caso Azure: #184.

**Resultado verificable:** URL funcional, diagrama real y evidencia de una API actualizada/escalada sin redesplegar la otra.

**Tareas existentes:** [#162](https://dev.azure.com/mengrau/CRM%20SmartSales%20.Net/_workitems/edit/162), [#163](https://dev.azure.com/mengrau/CRM%20SmartSales%20.Net/_workitems/edit/163), [#164](https://dev.azure.com/mengrau/CRM%20SmartSales%20.Net/_workitems/edit/164).

## HU12 — #165

**Objetivo:** Automatizar release por componente y demostrar recuperación.

**Predecesoras en Boards:** HU11. Orden/gates adicionales: secuencia anterior y decisiones P aplicables.

**Backend:** CI y bundle de migración compatible; pipeline API blue/green independiente.

**Angular:** CI Angular y despliegue Web estándar; no forzar redespliegue por cambios de Sales.

**Datos:** Migración one-off antes de tráfico, expand/contract y rol separado.

**Integración:** Azure PipelinesíECR/S3VersionId→CodePipeline→CodeBuild→CodeDeploy; gate G-04.

**Aceptación:** HU12-AC01, HU12-AC02, HU12-AC03; texto íntegro en [requirements](requirements.md). No modificarlo para ajustar implementación.

**Pruebas:** Fallo de build bloquea, release exacta por digest/VersionId, fallo de migración bloquea, green no saludable preserva anterior y rollback, otra API no cambia. Caso Azure: #185.

**Resultado verificable:** Ejecuciones reales, fallida y exitosa, con trazabilidad completa; no bastan archivos YAML.

**Tareas existentes:** [#166](https://dev.azure.com/mengrau/CRM%20SmartSales%20.Net/_workitems/edit/166), [#167](https://dev.azure.com/mengrau/CRM%20SmartSales%20.Net/_workitems/edit/167), [#168](https://dev.azure.com/mengrau/CRM%20SmartSales%20.Net/_workitems/edit/168).

## HU13 — #169

**Objetivo:** Cerrar el entregable con evidencias verificables.

**Predecesoras en Boards:** HU04, HU07, HU05, HU08, HU12, HU06. Orden/gates adicionales: secuencia anterior y decisiones P aplicables.

**Backend:** Identificar versiones y comprobar flujos operativos; no agregar features.

**Angular:** Recorrido corto con datos ficticios, acceso y visualización correctos.

**Datos:** Datos demostrables y persistencia, sin alteración/destrucción no autorizada.

**Integración:** Matriz del curso, diagramas reales, costos y guía de operación/limpieza.

**Aceptación:** HU13-AC01, HU13-AC02, HU13-AC03; texto íntegro en [requirements](requirements.md). No modificarlo para ajustar implementación.

**Pruebas:** Recorrido E2E, enlaces, versión presentada, pipeline/rollback/escala, criterios pendientes y revisión final del paquete. Caso Azure: #186.

**Resultado verificable:** Entrega reproducible que distingue resultados ejecutados de hipótesis y limitaciones.

**Tareas existentes:** [#170](https://dev.azure.com/mengrau/CRM%20SmartSales%20.Net/_workitems/edit/170), [#171](https://dev.azure.com/mengrau/CRM%20SmartSales%20.Net/_workitems/edit/171).

## Plantilla de handoff

- HU y subincremento autorizado; criterios atendidos y diferidos.
- Archivos y contratos modificados; migraciones; decisión de arquitectura usada.
- Comandos realmente ejecutados, resultado, ambiente y evidencia.
- Estado: pendiente/en curso/implementada pendiente de verificar/verificada.
- Dependencias, bloqueos y próximo paso dentro del conjunto autorizado.

No registrar fechas de finalización o porcentajes de implementación por adelantado. No hay estimaciones
en puntos/horas aprobadas; no inventarlas. Los 13 casos Azure son diseños, no ejecuciones exitosas.
