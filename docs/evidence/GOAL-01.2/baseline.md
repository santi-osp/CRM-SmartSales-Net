# GOAL-01.2 — Baseline

Fecha: 2026-11-09 (America/Bogota). Repositorio original SmartSales CRM, rama `master`, HEAD `011c57b`; `origin` Azure Repos. No commit/push. Cambios anteriores en `docs/`, `src/Web/`, `infra/`, `pipelines/`, `.opencode/`, `.serena/` y ZIP de evaluación preservados.

Backend verificado: `UserSeeder`, `DemoContactsSeeder`, `DemoSalesSeeder`; comandos `--seed-users`, `--seed-demo`; flag `Demo:Enabled`. Basic histórico: 2 usuarios, 8 contactos, 8 oportunidades, 3 seguimientos, 3 interacciones. Docker daemon no disponible; SDK host 10.0.204 frente a 10.0.300 de `global.json`. Por tanto no se pueden afirmar pruebas PostgreSQL, Docker Compose ni Playwright actuales.

Restricciones de esquema: `contacts_db` y `sales_db` separados; Sales guarda UUID externos sin FK a Contacts; FK compuesta `(opportunity_id,contact_id)` dentro de Sales; check constraints para cierre, agenda y tipos. Los ocho IDs originales por entidad y los tres de actividad se conservan. Las fechas relativas de agenda se fijan al insertar. El usuario solicita Basic intacto y Showcase 4/60/100/120/150 en base fresca.
