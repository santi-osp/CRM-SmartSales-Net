# Contenedores locales — HU10

Tres imágenes Linux independientes: Contacts API, Sales API y Web estático. Cada API publica un
ejecutable `/app/migrate` de EF Core para migrar fuera del arranque normal; ningún contenedor de
aplicación crea tablas automáticamente. Los Dockerfiles fijan imágenes base por digest y usan
`dotnet restore --locked-mode` o `npm ci`.

## Ejecutar en Windows

Requisitos: Docker Desktop Linux, PowerShell y OpenSSL incluido con Git. Desde la raíz:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File deploy/local/run.ps1
```

El script genera claves RSA y contraseñas aleatorias en `.tools/local/` (ignorado por Git), compila,
inicia PostgreSQL 17, aplica migraciones de las dos bases, siembra datos ficticios idempotentes y
levanta los tres contenedores. Entrada: `http://127.0.0.1:8088`. Cuentas:
`admin@demo.local` y `comercial@demo.local`; contraseñas en `.tools/local/secrets.json`.
La URL solo escucha en loopback y usa HTTP de desarrollo. Para detener sin borrar datos:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File deploy/local/run.ps1 -Down
```

Compose usa un único PostgreSQL local y el rol de desarrollo `postgres`; **esto no verifica** el
aislamiento RDS de HU09. En AWS se requieren bases y roles de ejecución separados, TLS y RDS privado.
El frontend Nginx local conserva rutas `/api/contacts/*` y `/api/sales/*` al pasarlas a sus servicios.
La configuración Nginx de la imagen final espera que el ALB enrute esas rutas y devuelve 404 si una
petición de API llega por error al contenedor Web.

## Verificar

`/healthz`, `/api/contacts/health/ready` y `/api/sales/health/ready` deben responder 200 a través de
la URL local. El script comprueba las tres rutas. Para recorrido de navegador con bases desechables:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tests/e2e/run.ps1
```

HU10-AC02 sigue pendiente hasta publicar cada imagen en ECR con etiqueta de commit y registrar
su digest remoto. Un build local `:local` no demuestra publicación. El repositorio aún no tiene
commit del código actual y la sesión AWS Academy no está disponible; no inventar SHA ni digest ECR.
