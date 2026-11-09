# Instrucciones para agentes — SmartSales

## Puerta de aprobación

**Estado actual: arquitectura ARCH-001 v1 y roadmap aprobados el 2026-11-08; ADR-11 (Angular 22.2.x) aprobado posteriormente.**
El usuario autorizó ejecutar las HU activas según el roadmap. HU14/HU15 siguen excluidas.
G-01..G-04 se verifican antes de sus fases AWS; no se presumen satisfechos.
Consultar siempre [docs/approval.md](docs/approval.md).

Las decisiones de ARCH-001 son obligatorias. El mensaje de aprobación autoriza el conjunto del roadmap;
una nueva HU fuera de él o un cambio arquitectónico importante necesita decisión explícita.

## Lectura obligatoria y precedencia

1. Instrucciones actuales del usuario y este archivo.
2. `docs/approval.md`, `docs/progress.md` y `docs/implementation-plan.md`.
3. La sección de la HU en `docs/requirements.md`, incluidos **todos** sus criterios.
4. `docs/architecture.md` y `docs/decisions.md`.
5. Documentos específicos: `api-contracts.md`, `data-model.md`, `security.md`, `frontend.md`,
   `testing.md` y `aws-delivery.md`, todos bajo `docs/`.

Azure Boards es la fuente funcional viva; el snapshot local conserva lo leído al diseñar.
La arquitectura aprobada es la fuente técnica. Si difieren, registrar la diferencia y pedir una
decisión; no sobrescribir requisitos ni rediseñar por cuenta propia. No interpretar `Removed` como terminado.

## Flujo por HU

1. Confirmar aprobación y alcance autorizado; leer sus predecesoras y evidencias.
2. Revisar código/componentes existentes y reutilizarlos. Identificar los archivos y contratos afectados.
3. Implementar un incremento vertical con API, persistencia y Angular cuando corresponda.
4. Mantener contratos, permisos, reglas, migraciones y fallos parciales documentados.
5. Ejecutar build y pruebas pertinentes de `docs/testing.md`; verificar cada criterio `HUxx-ACnn`.
6. Registrar comandos, resultados, fecha, commit/archivos y limitaciones en `docs/progress.md`.
   Guardar evidencias bajo `docs/evidence/HUxx/` cuando existan, sin tokens, contraseñas ni datos reales.
7. Actualizar contratos/documentación si el cambio menor está permitido. Informar el resultado al usuario.
8. Actualizar Azure Boards solo dentro del alcance autorizado. No cerrar una HU por tener código o UI:
   frontend/API/datos, criterios, build y pruebas deben estar comprobados; si falta evidencia, dejar pendiente.

## Decisiones que no se pueden cambiar autónomamente

Límites o número de servicios, motor de datos, familia principal de versiones, protocolo de autenticación,
matriz de permisos, contratos incompatibles, propiedad de tablas, reglas comerciales, servicios AWS,
estrategia de despliegue o incorporación de una dependencia arquitectónica.
Crear una propuesta en `docs/decisions.md`, indicar HU afectadas, alternativas, riesgos y esperar decisión.
No aplicar la propuesta mientras se espera. Continuar solo trabajo independiente ya autorizado.

Sí se pueden resolver sin nueva aprobación: nombres internos, métodos privados, organización dentro de
las carpetas previstas, ajustes visuales dentro de los tokens y parches compatibles conforme a la política
de versiones. Registrar esas decisiones cuando afecten continuidad. No inventar reglas comerciales.

## Convenciones obligatorias tras la aprobación

- Dos APIs autónomas, un proyecto ejecutable por API; capas por carpetas. Sin referencias de proyecto
  entre Contacts y Sales, sin consultas SQL cruzadas ni biblioteca de entidades compartida.
- EF Core `DbContext` directo en casos de uso; mapeo DTO explícito. Nada de repositorio genérico,
  MediatR, bus de eventos, CQRS, Kubernetes, microfrontend o framework de estado global sin aprobación.
- Dominio independiente de ASP.NET/EF; endpoints delgados; validación y autorización en backend.
- Contratos camelCase, enums y rutas exactos de `docs/api-contracts.md`. Toda regla temporal usa
  `TimeProvider`; fechas persistidas UTC, presentación America/Bogota y moneda COP.
- C# y TypeScript en inglés; textos de usuario y documentación en español. Nullable y TypeScript strict.
  `async`/`await` y cancelación en I/O. No `.Result`, `async void`, SQL concatenado ni excepciones silenciadas.
- Cambios de esquema aditivos compatibles con blue/green; migraciones fuera del arranque de las APIs.
- Angular standalone, Reactive Forms, signals locales y servicios HTTP; guards complementan permisos.
- Prohibido usar mocks/datos simulados para afirmar que existe una funcionalidad integrada.
  Los seeds ficticios reales y las simulaciones aisladas de pruebas sí están previstos.
- Nunca registrar credenciales, tokens, connection strings o PII en repositorio/logs/evidencias.
- No desplegar, destruir recursos, alterar datos reales ni ampliar alcance sin autorización aplicable.
- No avanzar a otra HU fuera del conjunto autorizado. No hacer commit, push ni publicación por iniciativa propia.

## Handoff breve obligatorio

Al finalizar una sesión dejar: HU y criterios atendidos; cambios; pruebas realmente ejecutadas;
estado verificable; decisiones pendientes; siguiente paso dentro del roadmap. No delegar al siguiente
agente una decisión que ya esté resuelta en los documentos. Si falta una decisión, describirla con precisión.
