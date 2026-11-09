# Angular y experiencia de usuario — ARCH-001 v1

Diseño aprobado para HU01–HU08. Angular standalone, Router, Reactive Forms, HttpClient y signals locales.
Material/CDK 22 provee formularios, diálogos, tablas y drag/drop; estilos propios dan coherencia visual.
Sin NgRx, Bootstrap, Tailwind simultáneo, kit adicional de controles ni biblioteca de gráficos.

## Carpetas y contratos

```text
src/app/
  core/{auth,http,layout}/
  shared/{ui,forms,formatters}/
  features/
    auth/{pages,auth-api.service}/
    contacts/{pages,components,contacts-api.service}/
    opportunities/{pages,components,opportunities-api.service}/
    agenda/{pages,components,follow-ups-api.service}/
    dashboard/{pages,components,dashboard-api.service}/
    interactions/{components,interactions-api.service}/
  api-types/{contacts,sales}.generated.ts
```

Generar tipos desde OpenAPI con `openapi-typescript` 7.x en build de desarrollo; no editar generated.
Los servicios HTTP se escriben por feature usando esos tipos. No generar un cliente que también gestione
sesión, UI y caché. Los formularios pueden tener modelos de vista específicos, con conversión explícita.

| Ruta | Pantalla / datos | Permiso y HU |
|---|---|---|
| `/login` | Email/contraseña, confirmación de carga y error genérico | Pública, HU01 |
| `/dashboard` | Cuatro métricas, barras por etapa, responsable y fecha de creación | Autenticado, HU07 |
| `/contacts` | Tabla/búsqueda, alta y edición | Autenticado, HU02 |
| `/contacts/:id` | Ficha, oportunidades relacionadas e historial manual paginado | HU02/HU06 |
| `/opportunities` | Kanban y selector de vista de cerradas; filtros comunes | HU03/HU04 |
| `/opportunities/:id` | Datos, responsable, acciones de etapa/cierre e interacciones | HU03/HU04/HU06 |
| `/agenda` | Vencidos/Hoy/Próximos/Completados, alta y completar; scope Admin | HU05 |
| `/**` | Estado 404 con vuelta al inicio | Sin error técnico visible |

Lazy-load por feature. Raíz autenticada redirige a dashboard; sin token a login.
`authGuard` y controles de rol para la UX; backend sigue siendo la autoridad.

## Estado, HTTP y errores

- `AuthSession` singleton con token/user/expiración en signals, solo memoria. Cada feature mantiene sus
  filtros, carga, resultado y error; URL query params conserva filtros de listas compartibles.
- Servicios devuelven Observable; componentes usan `toSignal`/async y cancelación por navegación.
  Búsqueda debounce250 ms + distinct + switchMap; sin suscripciones anidadas ni caché global arbitraria.
- Interceptor auth solo adjunta Bearer a rutas C/S de la origin acordada. Interceptor de error interpreta
  ProblemDetails, conserva traceId y diferencia401/403/409/422/503; no transforma errores en respuestas exitosas.
- No repetir automáticamente POST/PUT/PATCH. Deshabilitar Guardar mientras se procesa.409 de concurrencia
  ofrece recargar y comparar; no fuerza versión nueva para sobrescribir.
- Signals de vista: loading, data, error y empty, sin imponer una biblioteca de máquina de estados.
  Evitar pantalla en blanco durante una petición secundaria. Contacto permanece visible si falla actividad.
- Nombres de tarjetas: lookup por lote, cache solo durante vida de la vista; si falla, etiqueta explícita
  «Contacto no disponible» con identificador, sin inventar datos. Reintentar lectura por acción del usuario.

## Formularios y flujos

- Contacto: nombre, tipo, correo, teléfono y origen; validar contacto requerido entre correo/teléfono.
  El servidor vuelve a validar.409 potential_duplicate abre diálogo para revisar y confirmar; solo la
  confirmación envía acceptPotentialDuplicate=true. No convertir la advertencia en bloqueo definitivo.
- Oportunidad: selector de contacto con búsqueda remota, título, monto, fecha esperada, prioridad;
  Comercial responsable propio visible sin selector, Admin selector de activos. Detalle edita campos
  básicos; reasignación separada y con confirmación. Respetar propuesta de contacto inmutable.
- Kanban: tres columnas activas y filtros compartidos. Mantener versión/etapa previas hasta éxito o
  revertir exactamente si se usa movimiento optimista. Drag/drop CDK acompañado por menú «Mover a»
  usable con teclado; Ganada/Perdida abre diálogo, no se resuelve por arrastre sin datos de cierre.
  Un mapa por id/version evita tarjetas duplicadas entre respuestas; conservar la mayor versión y
  recargar columnas tras una mutación. No concatenar ciegamente páginas o respuestas desactualizadas.
- Agenda: datetime-local convertido explícitamente desde America/Bogota a ISO con offset/UTC; no tratar
  una fecha local sin zona como UTC. Filtros usan respuesta del servidor, no re-clasifican con otro reloj.
  Pestañas exclusivas: solo cargar/mostrar el bucket seleccionado, sin combinar cachés de otras categorías.
  Confirmar completar y conservar acceso al historial de completados.
  Centralizar conversión en `shared/formatters/business-date-time`: Luxon 3, `fromISO` con
  zone=`America/Bogota`, comprobar `isValid`, luego `toUTC().toISO()`. Formatear el retorno en la misma
  zona, independientemente de la zona configurada del navegador. No agregar otra librería de fechas.
- Interacción: tipo, fecha/hora pasada, resumen y oportunidad opcional del contacto. Publicar y releer
  historial real; no agregar un mensaje ficticio como si se hubiera guardado.
- Dashboard: etiqueta «Fecha de creación», filtros comunes y clear. SVG/CSS para barras sencillas con
  tabla accesible equivalente. Datos se agregan en API; cero no es error ni skeleton infinito.

## Sistema visual propuesto

| Token | Valor |
|---|---|
| Fondo / superficie | `#F5F7FB` / `#FFFFFF` |
| Texto / texto secundario | `#182230` / `#475467` |
| Primario / hover | `#1D4ED8` / `#1E40AF` |
| Borde | `#D0D5DD` |
| Éxito / aviso / error | `#067647` / `#92400E` / `#B42318` |
| Tipografía | system-ui, Segoe UI, sans-serif; base 16 px, línea1.5 |
| Escala | espacio4/8/12/16/24/32; radios8/12px; sombra suave en tarjetas |

Navegación superior compacta y contenido máximo1440 px; en <768 px enlaces reordenados y una columna.
Encabezado de página 32/24 px, títulos de sección 20 px; cifras con formato COP uniforme. No depender de
fuentes externas ni solo del color para estados/prioridad. Propuesta P-07, sin cambiar alcance comercial.

Verificar anchos1440 y390 px. Sin solapamientos ni scroll horizontal global; Kanban y tablas anchas
pueden desplazarse dentro de su contenedor, con indicación visible. Controles con área táctil mínima 44 px,
labels reales, mensajes asociados, foco visible y retorno de foco al cerrar diálogos. Anunciar carga/error
y éxito con live region moderada. Contraste objetivo WCAG2.2 AA, a medir; no afirmar cumplimiento sin revisión.

HU08 fija al menos ocho contactos y estados variados. Seeds reales en las dos bases, idempotentes y
habilitados para Demo; datos controlados de prueba se permiten solo en tests, no como backend sustituto.
