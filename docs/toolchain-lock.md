# Toolchain fijado — ARCH-001 v1

Resuelto el 2026-11-08. `.NET` y npm usan lockfiles en cada proyecto; no actualizar líneas mayores sin ADR.

| Elemento | Versión | Registro |
|---|---|---|
| .NET SDK | 10.0.300 | `global.json`, disponible localmente |
| EF Core, JwtBearer, OpenAPI, MVC Testing | 10.0.14 | `Directory.Packages.props` y `packages.lock.json` |
| Npgsql EF provider | 10.0.3 | Igual |
| dotnet-ef | 10.0.14 | `dotnet-tools.json` |
| PostgreSQL | 17 | Migración y pruebas Testcontainers |
| Node LTS | 24.22.1 | ZIP oficial portable en `.tools/`, SHA256 `d2046d28c2c3dcd211ac5145c0bf30e2047ef9dfb306693c8c6e2eccd8b95fa5` verificado con SHASUMS256.txt oficial |
| npm | 11.19.1 | Distribución Node portable y `src/Web/package.json` |
| Angular core/common/forms/router/compiler | 22.2.5 | `src/Web/package.json` y `package-lock.json`; ADR-11 |
| Angular CLI/build | 22.2.6 | Igual |
| Angular Material/CDK | 22.2.6 | Igual |
| TypeScript | 6.0.2 | Igual |
| RxJS | 7.8.2 | Igual |
| Playwright Test | 1.65.0 | `src/Web/package.json` y lockfile; Edge local para E2E |
| Vitest | 4.1.12 | Corregido tras audit; solo pruebas |
| Base SDK .NET | 10.0.300-noble, digest `c0790639332692a0d56cdd81ed581cfd24d040d9839764c138994866df89a3b6` | Ambos Dockerfiles |
| Base ASP.NET | 10.0.8-noble, digest `8c0b6857eab7b2aa57884c839bf4678414606bd7d17370f18a842ac5cf414711` | Ambos Dockerfiles |
| Base Node | 24.22.1-bookworm-slim, digest `be8204d4bd327363357f532187b752ff62759c31bedf98e9e18fc08c9cbd605c` | Dockerfile Web |
| Base Nginx sin root | 1.29.4-alpine-slim, digest `93a0458087e8dd06e7e59ceaeb3c54c9e73f64730282ab9387c3fe47a42bfc99` | Dockerfile Web |
| Base PostgreSQL local | 17, digest `ff87138949ad9a46f32ff23bd6852b52ce024a13dffe9b4087be98292cdd3b4f` | Compose |

La versión del SDK satisface la línea10; las bibliotecas ASP.NET10.0.14 corren sobre runtime10.0.8
mediante el framework instalado, con pruebas locales. Actualizar runtime/SDK en imágenes de despliegue
se fijó por digest en HU10. Las imágenes finales ECR siguen pendientes.

E2E local: `powershell -NoProfile -ExecutionPolicy Bypass -File tests/e2e/run.ps1`. El script crea PostgreSQL desechable, migra y siembra ambas bases, inicia las dos APIs y Angular en loopback, ejecuta Playwright en Edge y limpia procesos/contenedor al salir. Requiere Docker Desktop, Edge, Node portable y OpenSSL de Git. Usa claves y contraseñas aleatorias solo durante la ejecución.

`npm audit` sobre el lock actualizado informó cero vulnerabilidades el 2026-11-08. Es un resultado
puntual del registro npm; repetirlo antes de publicar nuevas imágenes.

Para invocar npm con la versión portable desde la raíz en PowerShell:

```powershell
$env:PATH=(Resolve-Path '.tools/node-v24.22.1-win-x64').Path + ';' + $env:PATH
& '.tools/node-v24.22.1-win-x64/npm.cmd' --prefix src/Web run build
```
