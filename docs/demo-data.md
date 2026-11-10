# Datos ficticios de demostración — Basic y Showcase

Los seeders actuales son **opt-in** (`--seed-users`, `--seed-demo`), idempotentes por ID y no se ejecutan al arrancar normalmente la API o las réplicas. `Demo:Enabled=true` y `Demo:Profile=Basic|Showcase` habilitan únicamente la carga explícita de datos de ejemplo. Nunca usar sobre bases reales o compartidas sin autorización. `--seed-demo` se rechaza en ambientes distintos de `Development` o `Demo`; `--seed-users` requiere credenciales externas.

## Perfiles

| Recurso | Basic predeterminado | Showcase en base nueva | IDs |
|---|---:|---:|---|
| Usuarios | 2 | 4 (1 Administrator + 3 Commercial) | `019a6010-...-0001` a `0004` |
| Contactos | 8 | 60 | `019a6020-...-0001` a `0060` |
| Oportunidades | 8 | 100 | `019a6030-...-0001` a `0100` |
| Seguimientos | 3 | 120 | `019a6040-...-0001` a `0120` |
| Interacciones | 3 | 150 | `019a6050-...-0001` a `0150` |

Se conservan las identidades, nombres y relaciones de las ocho oportunidades/contactos originales y de los tres seguimientos/interacciones Basic. Showcase añade filas con IDs deterministas; no introduce tablas ni referencias SQL entre servicios. Correos ficticios reservados bajo `.example`; cuentas originales configuradas externamente; adicionales `comercial2@demo.local`, `comercial3@demo.local` configurables. **Las contraseñas se almacenan fuera de Git** en `.tools/local/secrets.json` y pueden ser distintas mediante variables `Seed__Commercial2__Password` y `Seed__Commercial3__Password` (mínimo 14 caracteres). JWT privado solo en Contacts.

### Distribución esperada en una base nueva

| Etapa oportunidad | Cantidad |
|---|---:|
| New | 25 |
| Contacted | 25 |
| Proposal | 25 |
| Won | 15 |
| Lost | 10 |

Prioridades High/Medium/Low y cuatro responsables se alternan en los 92 registros nuevos; los 8 originales mantienen valores previos. Las 150 interacciones tienen 50 Call, 50 Email y 50 Meeting. Seguimientos previstos al instante de siembra: 30 vencidos, 30 hoy, 30 próximos, 30 completados (los tres Basic cubren las primeras tres categorías). Los intervalos se calculan una vez con `TimeProvider` y `America/Bogota` y se guardan UTC; al pasar días, la clasificación de la agenda cambia naturalmente, sin modificar filas.

## Ejecución local segura con Docker Compose

Desde la raíz, con Docker Desktop activo y las imágenes existentes:

```powershell
# Basic (comportamiento previo; volumen postgres-data persistente)
./deploy/local/run.ps1

# Showcase (añade faltantes a la misma base; no elimina ni reinicia el volumen)
./deploy/local/run.ps1 -DemoProfile Showcase
```

El script genera/reutiliza secretos locales ignorados por Git, construye los Dockerfiles actuales, inicia PostgreSQL, aplica bundles EF, ejecuta `docker compose run --rm --entrypoint dotnet contacts SmartSales.Contacts.Api.dll --seed-users` y luego `--seed-demo` en Contacts y Sales, levanta los tres servicios y consulta health/readiness a través de `http://127.0.0.1:8088`. No usar `docker compose down -v`. Si los datos actuales importan, realizar backup autorizado antes de sembrar; el script **no crea un volumen temporal por defecto**. Basic y Showcase preservan ediciones a filas existentes (no restauran cambios).

Para una base nueva exclusivamente desechable, crear un proyecto Compose separado con un nombre distinto antes de preparar; no borrar volúmenes ajenos para forzar conteos.

## Validación SQL solo lectura (local / bases separadas)

Tras ejecutar Showcase sobre una base nueva, verificar con un cliente autenticado de Postgres:

```sql
-- Ejecutar en contacts_db únicamente
SELECT count(*) FROM users;      -- esperado: 4
SELECT count(*) FROM contacts;   -- esperado: 60
SELECT type, source, count(*) FROM contacts GROUP BY type, source ORDER BY type, source;

-- Ejecutar en sales_db únicamente
SELECT stage, count(*) FROM opportunities GROUP BY stage ORDER BY stage;  -- 25/25/25/15/10
SELECT count(*) FROM follow_ups;    -- esperado: 120
SELECT status, count(*) FROM follow_ups GROUP BY status;
SELECT type, count(*) FROM interactions GROUP BY type ORDER BY type; -- 50/50/50
SELECT owner_user_id, stage, count(*) FROM opportunities GROUP BY owner_user_id, stage ORDER BY owner_user_id, stage;
```

Los conteos totales exactos corresponden a una base nueva. Tras altas manuales, comparar los IDs del rango reservado, no los totales de toda la tabla. Para demostrar idempotencia, ejecutar el mismo comando de preparación dos o tres veces y comprobar que no aparecen IDs duplicados; cualquier edición de oportunidad o seguimiento debe permanecer intacta. Si el seeding falla a mitad, volver a ejecutar el mismo comando sin limpiar datos: los IDs faltantes se insertarán.

## Preparación futura para ECS Fargate (sin ejecutar AWS en este GOAL)

Con autorización específica sobre cuenta/región/entorno **Demo**, permisos `ecs:RunTask`/`iam:PassRole` y roles/SSM validados: después de migrar `contacts_db` y `sales_db` con tareas migradoras separadas, iniciar tareas puntuales con las **mismas imágenes** Contacts y Sales. Para Contacts ejecutar primero `--seed-users` y después `--seed-demo`; para Sales ejecutar `--seed-demo` una vez que Contacts esté completo. Cada tarea recibe `Demo__Enabled=true`, `Demo__Profile=Showcase`, secretos por referencias seguras y configuración de red que alcance **solo su base**; usar overrides de comando `dotnet SmartSales.Contacts.Api.dll --seed-users` / `--seed-demo` y `dotnet SmartSales.Sales.Api.dll --seed-demo`. Esperar estado STOPPED y comprobar `containers[0].exitCode == 0` antes del siguiente paso. **No** incluir esta ejecución en ECS Services, schedulers ni canalizaciones automáticas; no usar RDS sin aprobación explícita. Validar referencias cruzadas vía APIs HTTP autenticadas, nunca SQL entre bases.

Notas: la política de Production debe revisarse antes de diseñar el laboratorio Demo AWS; no se presume que `LabRole`, dominio o cuotas cumplan G-02/G-03/G-04. Este GOAL no publicó ni ejecutó nada en AWS.
