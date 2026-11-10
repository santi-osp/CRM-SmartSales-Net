# Matriz de Equivalencia de las 9 Fases del Curso AWS Academy

Curso: **«Creación de microservicios y una canalización de CI/CD con AWS» (Guía 181128)**  
Proyecto: **SmartSales CRM** (Arquitectura ARCH-001 v1)  
Fecha de corte: **2026-11-09**

---

## 1. Matriz de correspondencia y estado de implementación

| Fase del Curso | Práctica original de la guía | Adaptación académica aprobada en SmartSales CRM | Componente técnico y entregables | Estado local |
|---|---|---|---|---|
| **Fase 1: Análisis del monolito** | Análisis de código legacy Node.js/Express y MySQL de la cafetería | **No se crea monolito artificial**. El CRM nace directamente desacoplado en dos microservicios (.NET 10) y SPA (Angular 22). | `docs/architecture.md`, `docs/decisions.md` (ADR-01/02), `docs/api-contracts.md` | **VERIFIED** (Código y diseño desacoplados desde la base) |
| **Fase 2: Entorno y control de versiones** | Creación de entorno AWS Cloud9 y repositorios AWS CodeCommit | **Conservar Azure Repos como fuente Git canónica** (instrucción expresa del usuario). Entorno de desarrollo local Windows 11 con OpenCode Auto Dev. | `SmartSales.slnx`, `AGENTS.md`, `docs/toolchain-lock.md`, Git `origin/master` en Azure Repos | **VERIFIED** (Repositorio y toolchains portables validados) |
| **Fase 3: Desacoplamiento de base de datos** | Separación de tablas en MySQL | **PostgreSQL 17 en Amazon RDS**: dos bases lógicas independientes (`contacts_db` y `sales_db`), usuarios y roles aislados, sin consultas SQL cruzadas. | `infra/cloudformation/data.yaml`, `deploy/local/init-dbs.sql`, migraciones EF independientes | **IMPLEMENTED** (Plantillas y scripts listos; despliegue RDS pendiente autorización) |
| **Fase 4: Contenedorización** | Dockerfiles básicos para la aplicación de cafetería | **Tres imágenes multi-stage reproducibles**: Contacts API, Sales API y Web Nginx con usuario no root, digest pinning e imágenes base oficiales de .NET 10 y Alpine. | `src/Contacts/.../Dockerfile`, `src/Sales/.../Dockerfile`, `src/Web/Dockerfile`, `compose.yaml` | **IMPLEMENTED** (Dockerfiles listos y compilación .NET/Web verificada) |
| **Fase 5: Repositorios ECR y Clúster ECS** | Repositorios ECR manuales y clúster ECS | **Infraestructura como código**: 3 repositorios ECR con tag immutability y escaneo (`smartsales-contacts`, `sales`, `web`), clúster ECS Fargate, Task Definitions parametrizables. | `infra/cloudformation/delivery.yaml`, `deploy/{contacts,sales,web}/taskdef.json`, `pipelines/release/` | **IMPLEMENTED** (Plantillas CloudFormation y empaquetador de releases verificado) |
| **Fase 6: ALB y Target Groups** | Balanceador de carga básico para servicios | **Un solo ALB compartido con enrutamiento por path**: reglas para `/api/contacts/*` y `/api/sales/*`, fallback a Web; pares de Target Groups IP (A/B) para Blue/Green; health checks reales. | `infra/cloudformation/application.yaml`, análisis **ADR-12** sobre limitaciones de listeners | **IMPLEMENTED / BLOCKED (G-03 / ADR-12)** |
| **Fase 7: Despliegue de servicios ECS** | Despliegue manual de tareas y servicios | **ECS Fargate gestionado por CloudFormation**: servicios Contacts, Sales y Web con `AssignPublicIp: ENABLED` en subredes públicas con SG restrictivo y Circuit Breaker con rollback. | `infra/cloudformation/application.yaml`, scripts seguros `deploy/aws/bootstrap.ps1`, `verify.ps1` | **IMPLEMENTED** (Listo para despliegue cuando se autorice AWS) |
| **Fase 8: Automatización CI/CD** | Canalización CodePipeline con CodeCommit | **Canalización híbrida**: Azure Pipelines compila y testea -> publica imagen en ECR y ZIP versionado en S3 -> dispara CodePipeline -> CodeBuild ejecuta migración EF one-off. | `pipelines/azure-pipelines.yml`, `pipelines/buildspec-migrations.yml`, `deploy/aws/permissions.md` | **IMPLEMENTED / BLOCKED (G-04)** |
| **Fase 9: Actualización y Rollback** | Prueba manual de cambio de código y actualización | **Procedimiento automatizado de actualización sin downtime**: escalado independiente 1->2->1, rollback binario manteniendo esquema aditivo, scripts de verificación. | `deploy/aws/rollback.ps1`, `scale.ps1`, `teardown.ps1`, `docs/aws/rollback-guide.md` | **IMPLEMENTED / VERIFIED LOCALMENTE** |

---

## 2. Correspondencia con Requisitos R1–R6 y HU01–HU13

- **R1 (Diagrama de arquitectura AWS):** Satisfecho en `docs/aws/architecture.md`.
- **R2 (Estimación de costos):** Satisfecho en `docs/aws/cost-estimate.md` (desglose detallado de $0.39 USD/sesión y $75.06 USD/mes).
- **R3 (Validación de arquitectura existente):** Satisfecho mediante la separación física de proyectos .NET, pruebas de integración y contratos HTTP en `docs/aws/course-phases-matrix.md`.
- **R4 (Despliegue y automatización):** Satisfecho con plantillas CloudFormation en `infra/cloudformation/` y canalización en `pipelines/`.
- **R5 (Monitoreo y observabilidad):** Satisfecho con CloudWatch LogGroups en `application.yaml`, health checks en Kestrel/Nginx y alarmas documentadas.
- **R6 (Evidencias y lecciones aprendidas):** Satisfecho en `docs/evidence/GOAL-01/`.
- **HU01–HU08:** Refinamiento visual y de accesibilidad completado en Angular 22, validado con Vitest 5/5 y build limpio.
- **HU09–HU10:** Modelado de PostgreSQL RDS, bundle EF migrations y Dockerfiles listos con usuario no root y digests fijados.
- **HU11–HU13:** Plantillas CloudFormation, scripts de preflight/bootstrap/rollback y guías operativas completadas. Permanecen sin desplegar remotamente en estricto cumplimiento de las restricciones vinculantes.
