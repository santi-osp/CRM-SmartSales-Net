# Revisión crítica del diseño — ARCH-001 v1

Revisión del 2026-11-08 por el agente autor sobre requisitos, contratos y fuentes técnicas.
Se revisa una propuesta; los resultados de aplicación, compatibilidad instalada y AWS se comprobarán
cuando se autorice implementarlos. Esta revisión no es una certificación de seguridad ni una prueba de despliegue.

## Preguntas de validación

| Pregunta | Conclusión | Evidencia / condición |
|---|---|---|
| ¿Cumple los requisitos? | Cobertura documental de15 HU,13 activas y40 escenarios; pendiente aprobación y ejecución | requirements.md conserva alcance/AC y vínculos a tareas/casos; HU14/HU15 se excluyen explícitamente |
| ¿Es excesivamente complejo? | Dos APIs es el mínimo útil para demostrar servicios independientes; capas por carpetas | Sin bus, repositorio genérico, CQRS, store global, identity service ni analítica independiente |
| ¿Tiene dependencias innecesarias? | Cada dependencia propuesta tiene un uso concreto | EF/Npgsql, Material/CDK, Luxon por zona, generación de tipos; CodeBuild migra, no duplica builds |
| ¿Permite desarrollo incremental? | Sí, distinguiendo fundamento de aceptación final | HU09-A/B y criterios diferidos HU01/HU02 evitan ciclos; no cierre anticipado |
| ¿Es mantenible? | Ownership y contratos explícitos, servicios pequeños y cambios locales | Un DbContext/base por API, DTO, errores comunes y pruebas por HU; sin entidades compartidas |
| ¿Está listo para Angular? | Rutas, DTO, nullability, errores, sesión, paginación y formularios definidos | api-contracts.md y frontend.md; OpenAPI 3.1 y tipos generados, sin dependencia de un backend simulado |
| ¿Otro agente puede implementarlo sin rediseñar? | Puede seguir el diseño una vez aprobadas P-01..P-08 | AGENTS.md, contratos exactos, campos/constraints, permisos, roadmap y handoff; gates externos bloquean su fase |

## Correcciones incorporadas durante la revisión

1. **Ciclo de verificación:** HU01 exige una reasignación de HU03 y HU02 relaciones de otras HU. Se
   documentó cierre diferido en lugar de quitar criterios o declarar mocks suficientes.
2. **Toolchain:** .NET 8 local no es la base elegida; .NET 10 LTS evita arrancar con soporte por terminar.
   Node 22.20 instalado no cumple Angular 22.0; bootstrap debe preparar Node 24 compatible y lock estable.
3. **OpenAPI:** se usa3.1 nativo de .NET 10 y compatible con openapi-typescript 7, sin asumir un emisor3.0.3.
4. **Integridad entre servicios:** sin FK remota ni lecturas cruzadas; no se permite borrado externo.
   FK compuesta local evita asociar una actividad a oportunidad de otro contacto.
5. **Autenticación:** Sales valida clave pública local; no consulta Contacts en cada lectura ni usa su
   disponibilidad para readiness. Logout local y falta de revocación se declaran, no se ocultan.
6. **Agenda:** categorías disjuntas y conversión de zona explícita. No depende del reloj/zona del navegador.
7. **Concurrencia:** version evita perder cambios de Kanban/cierre/asignación;409 no dispara overwrite.
8. **AWS:** se descartó Service Connect con CODE_DEPLOY. El ALB único mantiene routing de blue/green.
   El puente de repositorio usa S3 VersionId y digest, no fuentes ECR/S3 independientes con carreras.
9. **Migraciones:** CodeBuild arranca un bundle EF en tarea one-off antes del deploy. No migración por
   réplica ni reversión destructiva de esquema al hacer rollback de binario.
10. **Presupuesto:** salida por public IP con SG restrictivo evita NAT/ALB extra; impacto de disponibilidad,
    costo y aislamiento documentado. No se inventa una cifra de costo ni se promete costo cero.
11. **Estado de contactos:** HU03-AC01 menciona contacto activo, pero HU02 no tiene ese estado. Se registra
    D-08/P-08 para decisión explícita; el criterio original sigue íntegro, sin campo inventado en el modelo.

## Aclaraciones y riesgos que siguen abiertos

Antes de implementar reglas, el usuario debe aprobar permisos compartidos, estados/inmutabilidad,
catálogos/límites, sesión, exposición HTTPS, roadmap y presentación (P-01..P-08). Son elecciones propuestas,
no requisitos confirmados retroactivamente.

G-01: falta la guía privada 181128. G-02: faltan capacidades/roles/cuotas de AWS. G-03: dominio/certificado
no confirmado. G-04: agente CI y credenciales/federación no confirmados. Estos gates no requieren rediseñar
la app para producir esta documentación; sí impiden anunciar despliegue o pipeline terminado.

RDS/ALB compartidos, cuenta LabRole potencialmente amplia, llamadas Sales→Contacts síncronas, JWT sin
revocación, salida mediante public IP y toolchain aún sin restore/build son riesgos reales. Sus mitigaciones
y alternativas están en [decisions](decisions.md). Si un gate hace inviable la topología, se solicita ADR
y decisión del usuario; un agente de implementación no sustituye la plataforma por su cuenta.

## Resultado

**Propuesta lista para revisión del usuario, con aprobación pendiente y gates externos explícitos.**
No existe evidencia de app compilada, pruebas de negocio exitosas ni infraestructura desplegada.
La verificación de esta entrega consiste en trazabilidad, referencias y consistencia documental.
El [registro de validación](planning-validation.json) comprueba preservación de alcance/criterios,
referencias del roadmap, enlaces locales, codificación y estado pendiente de aprobación.
