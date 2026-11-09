# Estrategia de pruebas y verificación — ARCH-001 v1

Plan aprobado. Los resultados reales de pruebas se registran en [progress](progress.md);
la verificación documental previa se conserva aparte en progress/review.

## Capas de verificación

| Nivel | Herramienta propuesta | Qué demuestra |
|---|---|---|
| Reglas backend | xUnit 2.9.x, sin DB/red | Etapas, cierres, montos, normalización, clasificación y permisos puros |
| API/persistencia | ASP.NET WebApplicationFactory 10 + PostgreSQL 17 Testcontainers 4.x | HTTP, validaciones, autorización, migraciones, FK, concurrencia y persistencia real |
| Cliente Contacts de Sales | HttpMessageHandler controlado y pruebas de contrato | Traducción404/401/403/5xx/timeout, cancelación y ausencia de escritura ante fallo |
| Angular | Vitest/TestBed y HttpTestingController | Formularios, servicios, componentes críticos, rutas y ProblemDetails |
| E2E real | Playwright 1.x, dos APIs y PostgreSQL reales | Flujos integrados y reglas observables desde UI |
| AWS | Ejecuciones reales y evidencia | RDS, salud, logs, aislamiento, actualización, escala y rollback |

No usar EF InMemory/SQLite para afirmar integridad PostgreSQL o migraciones RDS. Stubs son admisibles en
pruebas aisladas de error/contrato; no sustituyen los E2E de integración ni los resultados AWS.

En cada proyecto backend fijar xUnit/runner visualstudio y Microsoft.NET.Test.Sdk compatibles con .NET 10;
usar **VSTest** (global.json sin activación de MTP). No mezclar la sintaxis/flags de ambos runners.
Versiones exactas bloqueadas en bootstrap según architecture.md. Pruebas independientes, sin orden implícito.

## Datos y entorno

- Contenedores PostgreSQL 17 desechables. Bases/usuarios por servicio y por suite paralela; aplicar
  migraciones reales. No conectar una prueba automática a RDS o a una base del usuario por defecto.
- Fixture con instantes congelados mediante TimeProvider, ids estables por caso y claves JWT de prueba.
  Tokens vencidos, issuer/audience/algoritmo incorrecto y usuario inactivo cubiertos explícitamente.
- E2E ejecuta ambas APIs como procesos locales antes de HU10; después también contra Compose para
  verificar empaquetado. Sales llama al Contacts real en esos recorridos.
- HTTP solo en test in-memory/loopback y red Docker aislada con credenciales ficticias; no es excepción
  para login público. HTTPS local usa certificados de desarrollo; bypass de certificado solo en la
  configuración de navegador/proxy de pruebas, nunca en HttpClient de producción ni RDS.
- No escribir snapshots con tokens/contraseñas o datos personales. Evidence debe identificar ambiente,
  commit y criterios; una captura sola no demuestra una transacción guardada.

## Casos mínimos por funcionalidad

| HU | Pruebas imprescindibles adicionales al texto de sus AC |
|---|---|
| HU01 | Activa/inactiva/inexistente, error genérico, expiración, RS256/issuer/audience, rutas protegidas, memoria/logout y403 real al reasignar tras HU03 |
| HU02 | Email o teléfono, ambos vacíos, normalización, duplicado advertido/confirmado, búsqueda parcial, version incorrecta y conservación de ids/relaciones después de HU03/HU06 |
| HU03 | Validación de directorio real, dueño activo, Comercial no elige otro, Admin reasigna atómicamente con auditoría, filtros, monto y503 sin filas nuevas |
| HU04 | Cada transición entre tres etapas, error de movimiento, versión concurrente, cierres Won/Lost, motivo/monto, recarga y segundo cierre rechazado |
| HU05 | now exacto, fin de día/inicio siguiente en Bogotá, no solapamiento, browser UTC/distinta zona, fechas válidas pasadas, acceso a UUID ajeno, scope All403 y completar idempotente |
| HU06 | Tres tipos, futuro, resumen vacío, oportunidad de otro contacto, id único en dos fichas y fallo de Sales sin perder ficha Contacts |
| HU07 | Dataset con cinco etapas, filtros por creación y responsable, mismos límites para todos, abierto estimado, ceros y rango invertido |
| HU08 | 1440/390, teclado sin drag obligatorio, foco en diálogo, contraste comprobado, cargas/vacíos/errores, seeds repetidos y recorrido completo real |
| HU09 | Bases y usuarios aislados, migración limpia, persistencia al reiniciar, TLS RDS y rúbrica contrastada; no confundir prueba local con RDS |
| HU10 | Build limpio, contenedores separados, configuración externa, health y digest/commit en ECR |
| HU11 | URL HTTPS, ALB por ruta, no acceso directo a tarea/RDS, reemplazo de tarea sin perder datos, logs y cambio de una API sin cambiar otra |
| HU12 | CI fallida bloquea; VersionId/digest correctos; fallo de migración bloquea; green no saludable preserva anterior; rollback y esquema compatible |
| HU13 | Guion funcional y técnico, enlaces/versión/evidencias reales, costos/limpieza documentados, criterios pendientes explícitos |

Cada `HUxx-ACnn` tiene resultado individual en el registro de progreso. Los casos Azure #174..#186
son contenedores de escenarios diseñados, estado Design; no representan tests ya ejecutados.

## Contratos entre aplicaciones

El futuro directorio `contracts/` contiene OpenAPI exportado por servicio, producido por su build, sin secretos.
Exportar desde un host CI con configuración de prueba y claves efímeras, o generación build-time con esa
configuración; nunca añadir un bypass de autenticación/configuración para exportar documentación.
Generar `api-types/*.generated.ts` desde esos documentos con openapi-typescript 7; validar generación sin
diff y compilar Angular. Un esquema/export diferente obliga a revisar DTO, consumers y tests, no a editar
generated a mano. Los cambios incompatibles requieren decisión de arquitectura y versión de contrato.

Ejemplos de regresión: nullability de contacto/email, enums PascalCase de wire, decimal/date/instant,
version obligatoria en comandos y ProblemDetails.code. Pruebas de serialización .NET + clientes HTTP
Angular y E2E protegen ambos lados. Validación en UI no sustituye peticiones inválidas directas a API.

## Comandos futuros mínimos

Estos comandos son un **contrato de tooling a crear tras aprobación**, no comandos ejecutados aquí.
Los scripts npm `build`, `test`, `contracts:check` y `e2e` deberán existir antes de anunciar este gate operativo.

```text
dotnet restore SmartSales.slnx --locked-mode
dotnet build SmartSales.slnx --configuration Release --no-restore
dotnet test SmartSales.slnx --configuration Release --no-build --logger trx --results-directory artifacts/tests
npm --prefix src/Web ci
npm --prefix src/Web run build
npm --prefix src/Web test -- --watch=false
npm --prefix src/Web run contracts:check
npm --prefix src/Web run e2e
```

En Windows el E2E integrado se prepara y ejecuta con `tests/e2e/run.ps1` (ver [toolchain-lock](toolchain-lock.md)); `npm run e2e` por sí solo requiere que las dos APIs, bases y Angular ya estén iniciadas y que las variables `E2E_*` estén presentes. Playwright Test 1.65.0 se fijó dentro de la línea 1.x aprobada.

Si solo cambia una HU, ejecutar sus pruebas y regresiones que compartan contratos/reglas; build de los
componentes afectados. Antes del release, gates completos correspondientes. No repetir una batería más
amplia sin cambio, fallo o riesgo concreto pendiente. Docker inaccesible bloquea integración, no equivale a pass.

## Gate para declarar una HU terminada

Funcionalidad autorizada completa; datos reales cuando corresponda; UI y APIs integradas; cada AC
verificado; build y pruebas pertinentes aprobados; sin defecto crítico conocido; documentación actualizada;
evidencia identificable. Falta de entorno/evidencia → pendiente de verificación, nunca «terminada con mocks».
Cambios de seguridad/datos requieren sus pruebas aunque la pantalla parezca idéntica.

Fuente de tooling: [testing Angular](https://angular.dev/guide/testing),
[dotnet test y runners](https://learn.microsoft.com/en-us/dotnet/core/testing/unit-testing-with-dotnet-test),
[generación TypeScript OpenAPI](https://openapi-ts.dev/introduction).
