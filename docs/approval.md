# Aprobación de ARCH-001 v1

- Fecha de propuesta: 2026-11-08, America/Bogota.
- Estado: **APROBADA — ARCH-001 v1**.
- Arquitectura aprobada: sí, incluidas ADR-01..ADR-10 y P-01..P-08.
- Roadmap aprobado: sí.
- Implementación autorizada: HU09-A, HU01..HU08, HU10, HU09-B y HU11..HU13, en la secuencia del roadmap. HU14/HU15 permanecen fuera.
- Evidencia de aprobación: mensaje del usuario del 2026-11-08: «Bueno entonces si apruebo la arquitectura, ve ejecutando segun el plan que creaste».
- G-01 se aclaró posteriormente con la guía pegada por el usuario y su instrucción de omitir monolito/evidencias extensas. G-02..G-04 siguen pendientes.

## Qué se propone aprobar

La arquitectura de `architecture.md` y sus documentos asociados, las decisiones ADR-01 a ADR-10 y
las aclaraciones propuestas P-01 a P-08 de `decisions.md`, junto con el orden del roadmap.
Las propuestas comerciales P-01/P-02/P-03/P-08 completan huecos del backlog; no se presentan como requisitos
ya confirmados. El usuario puede aprobar el conjunto o pedir cambios por identificador.

Los gates externos G-01 a G-04 deben verificarse antes de ejecutar infraestructura incluso si se aprueba
el diseño. Aprobar no demuestra permisos de la cuenta, disponibilidad de servicios, un dominio ni cumplimiento
de una rúbrica que no se ha podido leer.

## Después de una aprobación explícita

El agente registra aquí el mensaje/fecha y decisiones aceptadas, actualiza el estado de los ADR aceptados
y anota qué HU o conjunto autoriza el usuario. Si solo se aprueba la arquitectura, solicitar la HU inicial
antes de implementar. Si el mismo mensaje autoriza un incremento, continuar dentro de ese alcance.

Una decisión importante nueva se propone como ADR; no se modifica silenciosamente la referencia aprobada.
Si cambia un criterio funcional, actualizarlo en Azure Boards con autorización y reconciliar trazabilidad,
contratos, pruebas y roadmap. La documentación no puede reducir unilateralmente los criterios vigentes.
