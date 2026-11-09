# SmartSales — CRM universitario

**Fase: implementación incremental de ARCH-001 v1 aprobada.**

CRM compacto con ASP.NET Core, Angular y Entity Framework Core, destinado a demostrar
microservicios y CI/CD en AWS Academy. ARCH-001 v1 fue aprobada el **8 de noviembre de 2026**.
El alcance autorizado está en [approval](docs/approval.md); el avance verificable está en [progress](docs/progress.md).

## Alcance

Acceso con dos roles y cuentas de demostración, contactos, oportunidades, Kanban y cierre,
agenda, interacciones manuales y dashboard. WhatsApp y Lead Score están fuera del MVP.
Dos APIs .NET, un frontend Angular y PostgreSQL en RDS. Las funciones locales están en implementación y verificación incremental; el despliegue AWS sigue pendiente.

## Entrada a la documentación

| Necesidad | Documento |
|---|---|
| Resumen técnico y estructura | [Arquitectura](docs/architecture.md) |
| Requisitos exactos y trazabilidad | [Requisitos](docs/requirements.md) |
| Tablas, claves e integridad | [Modelo de datos](docs/data-model.md) |
| Endpoints, DTO y errores | [Contratos REST](docs/api-contracts.md) |
| Sesión, permisos y secretos | [Seguridad](docs/security.md) |
| Rutas, formularios y diseño | [Frontend](docs/frontend.md) |
| AWS y pipeline | [Entrega AWS](docs/aws-delivery.md) |
| Orden y resultados por HU | [Roadmap](docs/implementation-plan.md) |
| Gates y evidencias de calidad | [Pruebas](docs/testing.md) |
| Decisiones y riesgos | [Decisiones](docs/decisions.md) |
| Revisión crítica y riesgos | [Revisión de arquitectura](docs/architecture-review.md) |
| Estado real | [Progreso](docs/progress.md) |
| Reglas de futuros agentes | [AGENTS.md](AGENTS.md) |

[Proyecto Azure DevOps](https://dev.azure.com/mengrau/CRM%20SmartSales%20.Net).
Las épicas son [EP01 #120](https://dev.azure.com/mengrau/CRM%20SmartSales%20.Net/_workitems/edit/120)
y [EP02 #121](https://dev.azure.com/mengrau/CRM%20SmartSales%20.Net/_workitems/edit/121).

## Ejecución y herramientas

La solución .NET y Angular, las migraciones de Contacts/Sales y datos ficticios idempotentes ya existen.
Ver [progreso](docs/progress.md) para pruebas realizadas y pendientes, y [datos demo](docs/demo-data.md)
para la preparación. Los Dockerfiles y Compose están en HU10; pipelines y recursos AWS siguen pendientes.

Toolchain fijado en [toolchain-lock](docs/toolchain-lock.md). Node 24 portable vive en `.tools/` ignorado
por Git; Docker Desktop se inició para las pruebas locales.

El recorrido de navegador local usa `powershell -NoProfile -ExecutionPolicy Bypass -File tests/e2e/run.ps1`.
Para levantar los tres contenedores y PostgreSQL local: `powershell -NoProfile -ExecutionPolicy Bypass -File deploy/local/run.ps1`.
La URL local es `http://127.0.0.1:8088`; el script deja las contraseñas aleatorias en `.tools/local/secrets.json` (ignorado por Git).

La guía del curso fue proporcionada por el usuario. Se prioriza desplegar el CRM en microservicios;
no se requiere una aplicación monolítica previa ni capturas extensas. Permisos efectivos de AWS,
certificado HTTPS y credenciales del laboratorio aún requieren comprobación. No se han creado recursos AWS.
