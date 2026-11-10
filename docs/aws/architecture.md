# Arquitectura AWS — SmartSales CRM (ARCH-001 v1)

Estado: **Preparación técnica local completada (GOAL 01)**. Despliegue en AWS pendiente de autorización.
Decisiones asociadas: [ADR-01 a ADR-12](../decisions.md) y [aprobación](../approval.md).

---

## 1. Visión general y topología académica

SmartSales CRM se compone de dos microservicios backend autónomos en ASP.NET Core 10 (.NET 10 LTS), un frontend SPA en Angular 22 servido por Nginx sin privilegios, y una base de datos relacional PostgreSQL 17 en Amazon RDS.

```
                                  [ Usuario / Navegador HTTPS ]
                                                |
                                          (Puerto 443)
                                                v
                    +-------------------------------------------------------+
                    |       Application Load Balancer (ALB Único)           |
                    |   - Redirección 80 -> 443                            |
                    |   - Certificado ACM TLS                              |
                    +-------------------------------------------------------+
                           |                     |                     |
             Regla: /api/contacts/*   Regla: /api/sales/*      Default (SPA)
                           |                     |                     |
                           v                     v                     v
                 +-----------------+   +-----------------+   +-----------------+
                 | TG-Contacts (IP)|   |  TG-Sales (IP)  |   |   TG-Web (IP)   |
                 +-----------------+   +-----------------+   +-----------------+
                           |                     |                     |
                           v                     v                     v
                 +-----------------+   +-----------------+   +-----------------+
                 | ECS Fargate     |   | ECS Fargate     |   | ECS Fargate     |
                 | contacts-api    |   | sales-api       |   | web-app (Nginx) |
                 | (0.25 vCPU/512M)|   | (0.25 vCPU/512M)|   | (0.25 vCPU/512M)|
                 +-----------------+   +-----------------+   +-----------------+
                           |            |        |
                           |            |        +--- LLamada HTTPS interna ---> [ALB]
                           |            |             (Validación de contactos/usuarios)
                           v            v
                    +-------------------------------------------------------+
                    |      Amazon RDS PostgreSQL 17 (Single-AZ Privado)    |
                    |   - Base lógica: contacts_db (usuario: contacts_app)  |
                    |   - Base lógica: sales_db    (usuario: sales_app)     |
                    |   - Conexión cifrada: Ssl Mode=VerifyFull            |
                    |   - Ingress SG: Solo puerto 5432 desde Fargate SG     |
                    +-------------------------------------------------------+
```

---

## 2. Red y Aislamiento de Seguridad (VPC)

- **VPC:** CIDR `10.0.0.0/16` con resolución DNS y hostnames habilitados.
- **Subredes Públicas (2 AZs):**
  - `PublicSubnet1` (`10.0.1.0/24`, AZ-a) y `PublicSubnet2` (`10.0.2.0/24`, AZ-b).
  - Alojan el ALB y las tareas ECS Fargate con `AssignPublicIp: ENABLED`.
  - **Decisión deliberada de costo de laboratorio (ADR-08):** Tareas Fargate en subred pública para salida directa a Internet/AWS (ECR, CloudWatch, SSM) sin costo de NAT Gateway (~$32 USD/mes por AZ descartado).
  - **Restricción de Ingress:** El Security Group de Fargate (`SecurityGroupFargate`) rechaza todo tráfico directo desde Internet; **solo acepta tráfico en el puerto 8080 proveniente del Security Group del ALB (`SecurityGroupALB`)**.
- **Subredes Privadas (2 AZs):**
  - `PrivateSubnet1` (`10.0.10.0/24`, AZ-a) y `PrivateSubnet2` (`10.0.11.0/24`, AZ-b).
  - Alojan el `DBSubnetGroup` de PostgreSQL RDS. Sin tabla de ruteo hacia el Internet Gateway ni acceso público.
  - **Security Group RDS:** Solo acepta conexiones TCP en puerto 5432 provenientes de `SecurityGroupFargate`.

---

## 3. Microservicios y Contratos de Comunicación

1. **Contacts API:**
   - Autenticación mediante JWT RS256 con expiración de 15 minutos en memoria (P-04).
   - CRUD de contactos, detección de duplicados por email/teléfono normalizado (FormC), búsqueda insensible a mayúsculas.
   - Endpoint de directorio para verificación de referencias: `/api/contacts/v1/directory/users/active` y `/api/contacts/v1/contacts/{id}`.
   - Base de datos propia: `contacts_db`.

2. **Sales API:**
   - Oportunidades comerciales, Kanban con etapas activas (`New`, `Contacted`, `Proposal`), transacciones de cierre (`Won`/`Lost`), reasignación exclusiva para Administrador con auditoría, agenda con reloj de Bogotá (UTC-5), interacciones compartidas y métricas agregadas de Dashboard.
   - **Comunicación HTTP hacia Contacts:** Para validar contacto existente y responsable activo, Sales llama a Contacts a través de la URL HTTPS del ALB. Timeout de 3 segundos; si Contacts no está disponible, Sales devuelve 503 sin modificar datos.
   - Base de datos propia: `sales_db`.

3. **Web Angular (Nginx):**
   - Single Page Application (SPA) construida en Angular 22 standalone con Material/CDK y signals.
   - Servida por Nginx no privilegiado (puerto 8080, usuario UID 101).
   - Health check en `/healthz` devolviendo 200 directo.
   - Fallback `try_files $uri $uri/ /index.html` para enrutamiento SPA; las rutas `/api/*` son gestionadas por las reglas del ALB.

---

## 4. Estrategia de Despliegue y Resolución de Bloqueo (ADR-12)

- **Problema de AWS CodeDeploy tradicional:**
  CodeDeploy para ECS gestiona tráfico a nivel de **Listener completo** (`TrafficRoute.ListenerArns`), no de reglas por ruta (`ListenerRule`). Dos Deployment Groups sobre el mismo listener HTTPS 443 colisionan y rompen el enrutamiento.
- **Resolución técnica preparada:**
  1. La plantilla `application.yaml` implementa un ALB único con reglas por path `/api/contacts/*` y `/api/sales/*` con pares de Target Groups A y B listos.
  2. Los servicios ECS incorporan `DeploymentCircuitBreaker: { Enable: true, Rollback: true }` para protección ante fallos.
  3. Se documentó formalmente **ADR-12** para que el propietario decida entre la Alternativa A (controlador ECS nativo con circuit breaker y un solo ALB) o la Alternativa B/C (CodeDeploy con listeners dedicados o balanceadores separados).

---

## 5. Almacenamiento y Cifrado de Datos

- **Instancia:** `db.t4g.micro`, gp3 de 20 GiB, cifrado en reposo con KMS AWS administrado.
- **TLS Obligatorio:** Grupo de parámetros `rds.force_ssl = 1`. Conexiones Npgsql configuradas con `Ssl Mode=VerifyFull` utilizando el certificado raíz oficial de Amazon RDS.
- **Aislamiento Lógico:** Dos bases lógicas (`contacts_db` y `sales_db`) con usuarios dedicados (`contacts_app` y `sales_app`) sin permisos cruzados ni privilegios DDL en tiempo de ejecución.
- **Migraciones EF Core:** Migraciones aditivas empaquetadas en ejecutables bundle standalone (`/app/migrate`), ejecutadas mediante tareas ECS efímeras previas al inicio de tráfico nuevo.
