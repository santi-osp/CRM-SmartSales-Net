# Datos ficticios de demostración

Los dos servicios tienen seeds opt-in, reales e idempotentes para una base de demostración. No se usan como sustitutos de la API en producción ni se guardan credenciales en Git.

## Orden de preparación

1. Aplicar migraciones de Contacts y Sales con sus usuarios migradores. Las APIs **no** migran al iniciar.
2. Ejecutar Contacts con `--seed-users` y los valores externos `Seed:Administrator:{Email,DisplayName,Password}` y `Seed:Commercial:{Email,DisplayName,Password}`. Ambas contraseñas deben tener al menos 14 caracteres.
3. Con `Demo:Enabled=true`, ejecutar Contacts con `--seed-demo`.
4. Con `Demo:Enabled=true`, ejecutar Sales con `--seed-demo`.
5. Validar por HTTP que los ocho contactos y sus ocho oportunidades correspondan a los mismos identificadores. Sales no lee la base de Contacts durante el seed.

Cada comando usa la configuración normal de su API: conexión propia, clave pública JWT y clave privada solo Contacts; Sales requiere `ContactsBaseUrl`. Configurar secretos en variables de entorno o un almacén permitido. No ponerlos en argumentos de terminal, scripts versionados ni capturas.

## Conjunto fijo

| Recurso | Cantidad | Identificadores / estados |
|---|---:|---|
| Usuarios | 2 | IDs fijos `019a6010-...-0001/0002`, roles Administrador y Comercial; correo/contraseña externos |
| Contactos | 8 | IDs `019a6020-...-0001` a `0008`, nombres y correos `demo.example` ficticios |
| Oportunidades | 8 | IDs `019a6030-...-0001` a `0008`, etapas New/Contacted/Proposal/Won/Lost |
| Seguimientos | 3 | IDs `019a6040-...-0001` a `0003`, vencido/hoy/próximo al crear una base limpia |
| Interacciones | 3 | IDs `019a6050-...-0001` a `0003`, Call/Email/Meeting |

La segunda ejecución no agrega filas ni reabre oportunidades/seguimientos que el usuario haya cambiado. Las fechas relativas de la agenda se fijan en la primera ejecución. Para un demo posterior, preparar una base limpia antes de sembrar de nuevo; no borrar datos del usuario para «refrescar» la agenda.

Solo activar `Demo:Enabled` en un ambiente de demostración controlado. El seed rechaza IDs preexistentes con referencias incompatibles y no modifica registros ajenos.
