# Fuentes y vigencia

Consulta de planificación: **8 de noviembre de 2026**. Las versiones exactas y permisos se vuelven a
validar al bootstrap/despliegue; no se adopta automáticamente otra familia principal.

## Fuentes del usuario y del proyecto

- [Prompt original y solicitud arquitectónica](source/user-brief.md).
- [Snapshot de Azure Boards](source/backlog-snapshot.json):67 elementos, épicas#120/#121 y HU#129..#173.
- [Proyecto Azure DevOps](https://dev.azure.com/mengrau/CRM%20SmartSales%20.Net).

Se consultó por CLI autenticado; se guardaron solo campos necesarios y relaciones. La consulta fue
de lectura. La guía privada AWS Academy 181128 no está en estas fuentes y no se ha validado.

## Fuentes técnicas primarias

| Fuente | Qué sustenta |
|---|---|
| [.NET Support Policy](https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core) | Elegir .NET 10 LTS y evitar líneas que terminan soporte próximamente |
| [Angular compatibility](https://angular.dev/reference/versions) | Angular 22.0, Node 24>=24.15 y TypeScript 6.0; rangos RxJS |
| [Angular releases](https://angular.dev/reference/releases) | Línea estable y soporte; no basar stack en un prerelease |
| [Npgsql EF 10](https://www.npgsql.org/efcore/release-notes/10.0.html) | Proveedor EF 10 y necesidad de usar capacidades del PostgreSQL objetivo |
| [RDS PostgreSQL releases](https://docs.aws.amazon.com/AmazonRDS/latest/PostgreSQLReleaseNotes/postgresql-versions.html) | Motor disponible en RDS; minor/región aún se verifican en la cuenta |
| [OpenAPI ASP.NET](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/openapi/aspnetcore-openapi?view=aspnetcore-10.0) | Emisión nativa OpenAPI 3.1 en .NET 10 |
| [JWT bearer ASP.NET](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/configure-jwt-bearer-authentication?view=aspnetcore-10.0) | Validación de tokens y políticas backend; protocolo concreto es propuesta propia |
| [Angular testing](https://angular.dev/guide/testing) | TestBed y Vitest de tooling Angular |
| [dotnet test](https://learn.microsoft.com/en-us/dotnet/core/testing/unit-testing-with-dotnet-test) | Diferencia VSTest/MTP; se propone VSTest con xUnit 2 |
| [OpenAPI TypeScript](https://openapi-ts.dev/introduction) | Tipos desde OpenAPI 3.0/3.1 |
| [Luxon zonas](https://github.com/moment/luxon/blob/master/docs/zones.md) | Interpretar entrada en una zona explícita y convertir instantes |
| [Luxon API](https://moment.github.io/luxon/api-docs/index.html) | fromISO, setZone y toUTC |
| [S3 source CodePipeline](https://docs.aws.amazon.com/codepipeline/latest/userguide/action-reference-S3.html) | ZIP de source en bucket versionado |
| [StartPipelineExecution](https://docs.aws.amazon.com/cli/latest/reference/codepipeline/start-pipeline-execution.html) | Seleccionar revisión concreta con S3_OBJECT_VERSION_ID |
| [CodeDeployToECS](https://docs.aws.amazon.com/codepipeline/latest/userguide/action-reference-ECSbluegreen.html) | Task definition/AppSpec, imágenes y despliegue blue/green |
| [CodeDeploy ECS deployment](https://docs.aws.amazon.com/codedeploy/latest/userguide/deployment-steps-ecs.html) | Pares de target groups y tráfico hacia task sets |
| [ECS migration controllers](https://docs.aws.amazon.com/AmazonECS/latest/developerguide/migrate-codedeploy-to-ecs-bluegreen.html) | Diferenciar CODE_DEPLOY de blue/green nativo ECS |
| [Service Connect restrictions](https://docs.aws.amazon.com/AmazonECS/latest/developerguide/service-connect-concepts-deploy.html) | Incompatibilidad con blue/green controlado por CodeDeploy |

La topología, separación de capas, endpoints, permisos propuestos, catálogos, campos, sesión y roadmap
son diseño de este proyecto a partir de sus HU; no son recetas copiadas ni garantías de esas fuentes.
No se han probado instalaciones, AWS ni aplicaciones al consultar documentación.
