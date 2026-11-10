# Guion de Demostración Comercial — SmartSales CRM (Showcase)

**Duración estimada:** 7 a 10 minutos.  
**Perfil:** `Demo:Profile=Showcase` (4 usuarios, 60 contactos, 100 oportunidades, 120 seguimientos, 150 interacciones).  
**Entorno:** Local Docker Compose (`http://127.0.0.1:8088`) o ambiente de laboratorio autorizado.

---

## 1. Introducción y Arquitectura (1 minuto)

- **Objetivo:** Mostrar la arquitectura de microservicios y la separación de responsabilidades.
- **Puntos a mencionar:**
  - Dos microservicios autónomos en .NET 10 LTS (`Contacts API` y `Sales API`), un frontend SPA en Angular 22 (Material/CDK, signals) servido por Nginx unprivileged, y PostgreSQL 17 en bases lógicas aisladas (`contacts_db` y `sales_db`).
  - No hay tablas compartidas, ensamblados comunes ni consultas SQL cruzadas entre servicios; la comunicación es vía contratos HTTP REST.
  - La arquitectura está preparada para AWS (ECS Fargate, ALB con enrutamiento por path, RDS y CI/CD con Azure Pipelines).

---

## 2. Acceso y Dashboard Comercial (2 minutos)

1. **Iniciar sesión como Administradora:**
   - URL: `http://127.0.0.1:8088/login`
   - Correo: `admin@demo.local` (contraseña obtenida de `.tools/local/secrets.json`).
   - Resaltar: Autenticación JWT RS256 con expiración de 15 minutos en memoria, sin credenciales expuestas en frontend.
2. **Explicación del Dashboard:**
   - La pantalla principal muestra cuatro indicadores consolidados de la misma cohorte:
     * Oportunidades Abiertas (75 abiertas en estado inicial: 25 New, 25 Contacted, 25 Proposal).
     * Ganadas (15) y Perdidas (10).
     * Monto estimado abierto consolidado en Pesos Colombianos (`COP`).
   - Tabla de distribución por etapa con gráficos de barra porcentuales y cantidades exactas.
3. **Filtros en tiempo real:**
   - Seleccionar un responsable específico (ej. `Comercial Demo`) y hacer clic en **«Aplicar filtros»**.
   - Observar cómo las cifras y la tabla se recalculan sobre la misma cohorte sin recargar la página.
   - Resaltar la sincronización de filtros con los query parameters de la URL (`?owner=...`), permitiendo compartir o guardar búsquedas.
   - Hacer clic en **«Limpiar»** para restaurar la vista global.

---

## 3. Directorio de Contactos e Historial Compartido (2 minutos)

1. **Navegar a Contactos (`/contacts`):**
   - La lista presenta 60 empresas y contactos comerciales colombianos ficticios, con paginación limpia de 20 por página.
   - En el campo de búsqueda, escribir un término parcial (ej. `Valle` o `Café`) y observar el filtrado insensible a mayúsculas con debounce de 250ms.
2. **Abrir ficha de contacto:**
   - Seleccionar `Café Aurora` (o `Agroindustrias del Valle`).
   - La ficha detallada carga datos de contacto y el componente de **Actividad** (interacciones y llamadas registradas).
3. **Edición con conservación de relaciones (HU02-AC03):**
   - Hacer clic en **«Editar ficha»**.
   - Cambiar el teléfono o correo y guardar.
   - Demostrar que los cambios persisten de inmediato y que las oportunidades e interacciones asociadas en Sales API se conservan intactas (relaciones preservadas mediante UUID inmutable).

---

## 4. Tablero Kanban y Ciclo de Venta (2 minutos)

1. **Navegar a Tablero (`/pipeline`):**
   - El tablero Kanban agrupa las oportunidades activas en 3 columnas: **Nuevo (25)**, **En contacto (25)** y **Propuesta (25)**.
   - Explicar la distribución de montos COP tabulares y etiquetas de prioridad (Alta, Media, Baja).
2. **Gesto de arrastre (Drag-and-Drop) y menú accesible:**
   - Arrastrar una tarjeta desde «Nuevo» hacia «En contacto».
   - Observar cómo se actualiza la versión optimista y se recarga el estado confirmado por el servidor.
   - Mostrar el menú contextual de tres puntos en la tarjeta: permite mover la etapa usando exclusivamente el teclado (accesibilidad WCAG).
3. **Cierre de oportunidad (Ganada / Perdida):**
   - En una tarjeta de «Propuesta», abrir el menú y seleccionar **«Cerrar oportunidad»**.
   - Se abre el diálogo modal:
     * Marcar como **Ganada**: exige ingresar el monto final en COP.
     * Confirmar el cierre: la tarjeta sale del tablero Kanban activo.
   - Volver al Dashboard (`/`): demostrar que el contador de «Ganadas» se incrementó inmediatamente.

---

## 5. Agenda de Seguimientos en Bogotá (1.5 minutos)

1. **Navegar a Agenda (`/agenda`):**
   - Mostrar las cuatro categorías con 30 registros cada una al sembrar una base limpia:
     * **Vencidos:** compromisos pasados pendientes de gestión.
     * **Hoy:** tareas programadas para el transcurso del día en zona horaria de Bogotá (UTC-5).
     * **Próximos:** reuniones y llamadas a partir de mañana.
     * **Completados:** historial de seguimientos ya cerrados.
2. **Completar un seguimiento:**
   - En la pestaña «Hoy», hacer clic en **«Completar»** sobre un seguimiento.
   - La tarea desaparece de pendientes y pasa inmediatamente a la pestaña «Completados» con fecha y autor de finalización.

---

## 6. Comparación de Roles y Permisos (1.5 minutos)

1. **Cerrar sesión** desde el menú superior derecho.
2. **Iniciar sesión como Comercial:**
   - Correo: `comercial@demo.local` (contraseña de `.tools/local/secrets.json`).
   - Observar en la cabecera el badge `Comercial` y el nombre del usuario.
3. **Verificación de permisos comerciales (HU01-AC03):**
   - Navegar a Oportunidades (`/opportunities`).
   - Abrir una oportunidad existente: el Comercial puede ver los datos y actualizar notas.
   - El botón o selector de «Reasignar responsable» está oculto en la interfaz para el rol Comercial.
   - Si se intenta una petición directa `PATCH /api/sales/v1/opportunities/{id}/owner`, la API responde con **HTTP 403 Forbidden** protegiendo los datos.

---

## 7. Cierre Técnico: Idempotencia y PostgreSQL (1 minuto)

1. **Verificación de datos en base de datos:**
   - Explicar que todos los registros se almacenan en tablas reales de PostgreSQL 17 (`contacts_db` y `sales_db`), no en memoria ni en archivos JSON del cliente.
2. **Demostración de idempotencia:**
   - Ejecutar nuevamente en terminal:
     ```powershell
     ./deploy/local/run.ps1 -DemoProfile Showcase
     ```
   - Al terminar, comprobar que los conteos no se duplicaron (siguen siendo exactamente 60 contactos y 100 oportunidades) y que la oportunidad que cerramos y el contacto que editamos **conservan su estado modificado por el usuario**.
