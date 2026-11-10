# Estimación de Costos — Laboratorio AWS Academy (ARCH-001 v1)

Región candidata: **us-east-1 (N. Virginia)**.
Precios oficiales vigentes consultados a fecha noviembre 2026.
Moneda: **Dólares estadounidenses (USD)**.

---

## 1. Supuestos del entorno académico

1. **Modalidad de laboratorio:** La cuenta AWS Academy dispone de un crédito limitado para la sesión educativa. No se debe asumir costo cero; el presupuesto debe calcularse tanto en base mensual continua como por sesión activa de trabajo.
2. **Duración típica de una sesión de laboratorio:** Sesiones de 3 a 4 horas de uso activo, tras las cuales los servicios no esenciales pueden detenerse o destruirse mediante scripts de teardown.
3. **Decisiones de optimización de costos aprobadas en ARCH-001:**
   - **Sin NAT Gateways:** Se evita desplegar NAT Gateway en ambas AZs, ahorrando ~$64.80 USD/mes ($0.045/hora por gateway + cargos de datos).
   - **ALB único compartido:** Un solo Application Load Balancer para Web, Contacts y Sales, ahorrando ~$45.00 USD/mes respecto a balanceadores independientes.
   - **RDS Single-AZ:** Instancia `db.t4g.micro` en una sola AZ con almacenamiento mínimo de 20 GiB gp3.
   - **Dimensionamiento Fargate mínimo:** 0.25 vCPU y 512 MiB de RAM por tarea.

---

## 2. Desglose detallado de costos por componente

| Servicio AWS | Configuración / Dimensión | Tarifa Unitaria (us-east-1) | Costo por Hora (USD) | Costo Sesión (4 hrs) | Costo Mensual Continuo (730 hrs) |
|---|---|---|---|---|---|
| **Application Load Balancer** | 1 ALB público, ~1 LCU consumida | $0.0225 / hora + $0.008 / LCU-hora | ~$0.0305 | $0.122 | ~$22.27 |
| **ECS Fargate (Contacts API)** | 1 tarea (0.25 vCPU, 0.5 GB RAM) | $0.04048/vCPU-hr + $0.004445/GB-hr | $0.0123 | $0.049 | ~$8.98 |
| **ECS Fargate (Sales API)** | 1 tarea (0.25 vCPU, 0.5 GB RAM) | $0.04048/vCPU-hr + $0.004445/GB-hr | $0.0123 | $0.049 | ~$8.98 |
| **ECS Fargate (Web Nginx)** | 1 tarea (0.25 vCPU, 0.5 GB RAM) | $0.04048/vCPU-hr + $0.004445/GB-hr | $0.0123 | $0.049 | ~$8.98 |
| **Direcciones IPv4 Públicas** | 3 tareas Fargate con public IP | $0.005 / IP / hora | $0.0150 | $0.060 | ~$10.95 |
| **Amazon RDS PostgreSQL 17** | `db.t4g.micro` Single-AZ | $0.0160 / hora | $0.0160 | $0.064 | ~$11.68 |
| **Almacenamiento RDS gp3** | 20 GiB gp3 (3,000 IOPS, 125 MB/s) | $0.115 / GiB / mes | — | Negligible | $2.30 |
| **Amazon ECR** | 3 repositorios (~1.5 GB total) | $0.10 / GB / mes | — | Negligible | $0.15 |
| **Amazon S3 (Releases)** | Bucket con versionado (< 500 MB) | $0.023 / GB / mes | — | Negligible | $0.02 |
| **CloudWatch Logs** | Ingesta < 1 GB/mes, retención 7 días | $0.50 / GB ingerido | — | Negligible | $0.25 |
| **Capacidad Blue/Green (Surge)**| Tareas temporales durante despliegue | ~30 min por despliegue | — | ~$0.025 | ~$0.50 |
| **TOTAL ESTIMADO** | **Pila completa SmartSales CRM** | — | **~$0.0984 / hora** | **~$0.39 USD** | **~$75.06 USD / mes** |

---

## 3. Recomendaciones operativas para el laboratorio

1. **Durante el desarrollo y pruebas:**
   Mantener los servicios activos durante la franja de pruebas (~4 horas). El costo incurrido será inferior a **$0.40 USD por sesión**.
2. **Al finalizar la sesión:**
   Si la cuenta de laboratorio no tiene crédito ilimitado, ejecutar `deploy/aws/teardown.ps1` o reducir el `desired-count` de los servicios ECS a `0` para detener las tareas Fargate y liberar las IPs públicas, manteniendo la base de datos RDS y la red intactas.
3. **Control de cargos por IPv4 pública:**
   Las 3 tareas Fargate con IP pública generan un costo de $10.95 USD al mes. Si se requiere reducir aún más, se puede consolidar en subredes privadas con VPC Endpoints gratuitos para ECR y CloudWatch si el laboratorio lo permite.
