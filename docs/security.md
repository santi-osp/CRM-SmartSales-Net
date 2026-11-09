# Autenticación y autorización — ARCH-001 v1

Diseño académico aprobado el 2026-11-08. HU01 exige credenciales, cuenta activa y dos
roles, no un servidor OAuth/OIDC completo. No presentar la solución propuesta como proveedor de identidad general.

## Sesión propuesta P-04

- Contacts valida email normalizado y contraseña con `IPasswordHasher<User>` de ASP.NET Core Identity.
  No implementar criptografía de contraseñas ni almacenar texto plano. Seed recibe contraseñas por secreto,
  mínimo propuesto 14 caracteres; la validación del login no cambia la contraseña ni revela si existe la cuenta.
- Emitir access token JWT **RS256, 15 minutos**, sin refresh token. Private key RSA >=3072 bits solo en Contacts;
  Sales y Contacts validan con public key. `kid` identifica la clave; issuer=`smartsales-contacts`,
  audience=`smartsales-api`, claims `sub` UUID, `name`, `role`, `jti`, `iat`, `exp`.
- Configurar `MapInboundClaims=false`, NameClaimType=`name`, RoleClaimType=`role`. Validar algoritmo,
  firma, issuer, audience y expiración, ClockSkew=30 segundos. No aceptar `none`, HS256 ni issuer libre.
- La clave pública se distribuye en configuración validada al arrancar; Sales no obtiene claves de una
  URL por petición. Rotación planificada: instalar claves públicas anterior+nueva, cambiar emisor y esperar
  vida máxima+skew antes de retirar la anterior. Cambio de esquema de claims requiere contrato/ADR.
- Angular conserva token **solo en memoria**, adjunta Authorization únicamente a sus dos APIs y a la misma
  origin configurada. Nunca localStorage/sessionStorage/URL. Recargar pestaña requiere login de nuevo;
  no hay persistencia ni renovación oculta. Expiración/401 limpia estado y abre login.
- Logout es local: borra token, estado sensible y rutas; el token emitido sigue siendo válido hasta expirar.
  No hay revocación inmediata. Cuenta inactiva no entra; desactivación manual posterior no invalida
  claims emitidos durante esos 15 minutos. Es un riesgo explícito aceptable solo si el usuario aprueba P-04.
- Las APIs validan el JWT sin consulta online por cada solicitud. `auth/me` sí confirma cuenta activa;
  Sales valida actividad del responsable al crear/reasignar. Así sus lecturas sobreviven a caída de Contacts.

## Matriz de permisos propuesta P-01

El backlog define CRM compartido y restricciones específicas, pero no dice que todas las ventas se
restrinjan por propietario. Se propone conservar colaboración comercial; aprobar esta matriz resuelve el hueco.

| Operación | Commercial | Administrator |
|---|---|---|
| Consultar/crear/editar contactos | Todos | Todos |
| Consultar oportunidades, historial y dashboard | Todos | Todos |
| Crear oportunidad | Responsable=sub; otro owner→403 | Puede elegir activo, default=sub |
| Editar datos básicos, mover/cerrar oportunidad abierta | Todas | Todas |
| Reasignar responsable | 403 | Activo, solo abierta |
| Registrar interacción en contacto/oportunidad válida | Sí, autor=sub | Sí, autor=sub |
| Crear seguimiento | Responsable=sub, sin elección | Responsable=sub, sin elección |
| Ver/completar seguimiento | Solo propio | Propio o todos |
| Listar referencias de contactos/usuarios | Sí, datos mínimos | Sí, datos mínimos |
| Gestionar usuarios, roles, integraciones o borrar registros | Endpoint inexistente | Endpoint inexistente |

No confiar en owner/author/createdBy/closedBy recibidos del cliente: campos de auditoría provienen de sub.
Validar permiso antes de escrituras y por cada ruta; un guard Angular no autoriza una API.
Manipular UUID/filtros no permite leer/completar seguimientos ajenos. Las consultas filtran en SQL por sub
cuando corresponde. All solicitado por Commercial produce403, no un cambio silencioso de alcance.

## Protección HTTP

- Origen único en despliegue: Angular y `/api/*` detrás del ALB; CORS deshabilitado allí.
  En desarrollo, proxy Angular al backend mantiene origin del navegador; si se habilita CORS para una
  herramienta local, lista explícita de origen, sin wildcard y sin cookies `AllowCredentials`.
- HTTPS obligatorio propuesto para login público; ver gate G-03. Desarrollo local HTTPS con certificado
  de desarrollo confiado; HTTP solo dentro de la red Docker local. No aceptar contraseñas públicas por HTTP.
- Sin cookies de autenticación, sin credenciales ambientadas: no hay flujo CSRF de cookies. No agregar
  un refresh cookie sin rediseñar CSRF, rotación y logout. XSS sigue siendo amenaza: template binding,
  texto escapado, sin `innerHTML` para datos comerciales ni `bypassSecurityTrust*`.
- Cabeceras frontend: `X-Content-Type-Options:nosniff`, `Referrer-Policy:strict-origin-when-cross-origin`,
  CSP `default-src 'self'; script-src 'self'; style-src 'self' 'unsafe-inline'; img-src 'self' data:;
  font-src 'self'; connect-src 'self'; object-src 'none'; frame-ancestors 'none'; base-uri 'self'; form-action 'self'`.
  Inline styles se permiten por Angular Material, nunca scripts inline. HSTS solo al tener HTTPS estable.
- Login rate limit: propuesta 5 intentos/minuto/IP por instancia con 429; advertir que no es control
  distribuido. Respuesta 401 genérica para cuenta inexistente/inactiva/clave incorrecta; evitar diferencias
  evidentes de trabajo de hash usando un hash ficticio al no existir la cuenta.
- Limitar cuerpos JSON a 64 KiB y q a120 caracteres; cancelar I/O y poner límite a paginación/lookup.
  Parametrizar EF, no interpolar SQL. Errores ProblemDetails sin trazas internas ni datos de terceros.
- `ForwardedHeaders`: confiar solo en red privada correspondiente al ALB y un salto; SG solo admite al ALB.
  No confiar en X-Forwarded-For de cualquier IP. Identificar IP de rate limit según proxy configurado.
- Login/me y respuestas comerciales: `Cache-Control:no-store`. No registrar Authorization, password,
  hashes, claves, cuerpo de login, correos o teléfonos en logs. Correlación con traceId y códigos técnicos.

## Credenciales, datos y operación

Local: user-secrets .NET/variables externas; archivos reales de entorno fuera de Git. Se permite un
`.env.example` futuro con nombres vacíos, nunca secretos funcionales. AWS: parámetros SSM SecureString
o mecanismo permitido que se apruebe en G-02, referenciados por tarea ECS y con roles existentes.
La arquitectura primaria usa SSM; no sustituir por Secrets Manager u otro servicio sin decisión si cambia permisos/costo.

RDS: TLS `VerifyFull`, endpoint DNS real y CA oficial RDS en la imagen; no desactivar validación de certificado.
La cuenta runtime no es dueña de bases ni migraciones. Password JWT private key y credenciales de cada API
separadas. Rotar secretos implica nuevas tareas; no asumir que la variable ECS se refresca sola.

Solo datos ficticios. URL pública y credenciales demo se entregan por canal autorizado al evaluador,
sin publicarlas en README. No usar cuentas personales del estudiante como datos de seed.
Despliegue público, recursos pagos y limpieza se autorizan en su fase; la planificación no los ejecuta.

Base técnica: [JWT bearer ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/configure-jwt-bearer-authentication?view=aspnetcore-10.0).
