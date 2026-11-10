# Inventario de Recursos AWS — SmartSales CRM (ARCH-001 v1)

Este inventario documenta todos los recursos lógicos y físicos definidos en las plantillas de Infraestructura como Código (`infra/cloudformation/`) y artefactos de despliegue (`deploy/`).

---

## 1. Stack de Red (`network.yaml`)

| Identificador Lógico | Tipo de Recurso AWS | Propósito / Configuración | Export / Referencia |
|---|---|---|---|
| `VPC` | `AWS::EC2::VPC` | Red virtual `10.0.0.0/16` con DNS habilitado | `${EnvironmentName}-VpcId` |
| `InternetGateway` | `AWS::EC2::InternetGateway` | Salida a Internet para ALB y tareas Fargate | — |
| `VPCGatewayAttachment` | `AWS::EC2::VPCGatewayAttachment` | Asociación del IGW con la VPC | — |
| `PublicSubnet1` | `AWS::EC2::Subnet` | Subred pública en AZ-a (`10.0.1.0/24`) | `${EnvironmentName}-PublicSubnet1` |
| `PublicSubnet2` | `AWS::EC2::Subnet` | Subred pública en AZ-b (`10.0.2.0/24`) | `${EnvironmentName}-PublicSubnet2` |
| `PrivateSubnet1` | `AWS::EC2::Subnet` | Subred privada en AZ-a (`10.0.10.0/24`) para RDS | `${EnvironmentName}-PrivateSubnet1` |
| `PrivateSubnet2` | `AWS::EC2::Subnet` | Subred privada en AZ-b (`10.0.11.0/24`) para RDS | `${EnvironmentName}-PrivateSubnet2` |
| `PublicRouteTable` | `AWS::EC2::RouteTable` | Tabla de enrutamiento con ruta `0.0.0.0/0` hacia IGW | — |
| `PrivateRouteTable` | `AWS::EC2::RouteTable` | Tabla de enrutamiento aislada para RDS (sin ruta externa) | — |
| `SecurityGroupALB` | `AWS::EC2::SecurityGroup` | Ingress 80 y 443 desde `0.0.0.0/0`; Egress 8080 hacia Fargate | `${EnvironmentName}-SecurityGroupALB` |
| `SecurityGroupFargate`| `AWS::EC2::SecurityGroup` | Ingress 8080 solo desde ALB; Egress total a Internet/AWS | `${EnvironmentName}-SecurityGroupFargate` |
| `SecurityGroupRDS` | `AWS::EC2::SecurityGroup` | Ingress 5432 solo desde `SecurityGroupFargate`; Egress 127.0.0.1 | `${EnvironmentName}-SecurityGroupRDS` |

---

## 2. Stack de Datos (`data.yaml`)

| Identificador Lógico | Tipo de Recurso AWS | Propósito / Configuración | Export / Referencia |
|---|---|---|---|
| `DBSubnetGroup` | `AWS::RDS::DBSubnetGroup` | Agrupación de subredes privadas para PostgreSQL | — |
| `DBParameterGroup` | `AWS::RDS::DBParameterGroup` | Familia `postgres17` con `rds.force_ssl = 1` | — |
| `RDSInstance` | `AWS::RDS::DBInstance` | PostgreSQL 17.2, `db.t4g.micro`, 20 GiB gp3, cifrado KMS | `${EnvironmentName}-DBEndpointAddress` |

---

## 3. Stack de Aplicación (`application.yaml`)

| Identificador Lógico | Tipo de Recurso AWS | Propósito / Configuración | Export / Referencia |
|---|---|---|---|
| `ECSCluster` | `AWS::ECS::Cluster` | Clúster Fargate `smartsales-cluster` | `${EnvironmentName}-ClusterName` |
| `ApplicationLoadBalancer` | `AWS::ElasticLoadBalancingV2::LoadBalancer` | ALB público en subredes públicas | `${EnvironmentName}-AlbDnsName` |
| `TGContactsA` / `B` | `AWS::ElasticLoadBalancingV2::TargetGroup` | Par de Target Groups IP en puerto 8080 (`/api/contacts/health/ready`) | — |
| `TGSalesA` / `B` | `AWS::ElasticLoadBalancingV2::TargetGroup` | Par de Target Groups IP en puerto 8080 (`/api/sales/health/ready`) | — |
| `TGWeb` | `AWS::ElasticLoadBalancingV2::TargetGroup` | Target Group IP en puerto 8080 para Nginx (`/healthz`) | — |
| `ALBListenerHTTP` | `AWS::ElasticLoadBalancingV2::Listener` | Puerto 80: Redirección 301 a HTTPS 443 | — |
| `ALBListenerHTTPS` | `AWS::ElasticLoadBalancingV2::Listener` | Puerto 443: Tráfico seguro con certificado ACM | — |
| `RuleContacts` | `AWS::ElasticLoadBalancingV2::ListenerRule` | Prioridad 10: Enruta `/api/contacts/*` a `TGContactsA` | — |
| `RuleSales` | `AWS::ElasticLoadBalancingV2::ListenerRule` | Prioridad 20: Enruta `/api/sales/*` a `TGSalesA` | — |
| `ContactsTaskDefinition` | `AWS::ECS::TaskDefinition` | Fargate 0.25 vCPU / 512 MiB, puerto 8080, logs CloudWatch | — |
| `SalesTaskDefinition` | `AWS::ECS::TaskDefinition` | Fargate 0.25 vCPU / 512 MiB, puerto 8080, logs CloudWatch | — |
| `WebTaskDefinition` | `AWS::ECS::TaskDefinition` | Fargate 0.25 vCPU / 512 MiB, puerto 8080, Nginx unprivileged | — |
| `ContactsService` | `AWS::ECS::Service` | DesiredCount 1, circuit breaker rollback, `AssignPublicIp: ENABLED` | `${EnvironmentName}-ContactsServiceName` |
| `SalesService` | `AWS::ECS::Service` | DesiredCount 1, circuit breaker rollback, `AssignPublicIp: ENABLED` | `${EnvironmentName}-SalesServiceName` |
| `WebService` | `AWS::ECS::Service` | DesiredCount 1, deployment estándar, `AssignPublicIp: ENABLED` | `${EnvironmentName}-WebServiceName` |
| `ContactsLogGroup` | `AWS::Logs::LogGroup` | `/ecs/${EnvironmentName}-contacts`, retención 7 días | — |
| `SalesLogGroup` | `AWS::Logs::LogGroup` | `/ecs/${EnvironmentName}-sales`, retención 7 días | — |
| `WebLogGroup` | `AWS::Logs::LogGroup` | `/ecs/${EnvironmentName}-web`, retención 7 días | — |

---

## 4. Stack de Entrega Continua (`delivery.yaml`)

| Identificador Lógico | Tipo de Recurso AWS | Propósito / Configuración | Export / Referencia |
|---|---|---|---|
| `EcrContacts` | `AWS::ECR::Repository` | Repositorio ECR inmutable `smartsales-contacts` | `${EnvironmentName}-EcrContactsUri` |
| `EcrSales` | `AWS::ECR::Repository` | Repositorio ECR inmutable `smartsales-sales` | `${EnvironmentName}-EcrSalesUri` |
| `EcrWeb` | `AWS::ECR::Repository` | Repositorio ECR inmutable `smartsales-web` | `${EnvironmentName}-EcrWebUri` |
| `ReleaseBucket` | `AWS::S3::Bucket` | Bucket S3 cifrado con versionado para paquetes de release | `${EnvironmentName}-ReleaseBucketName` |
| `MigrationBuildProject`| `AWS::CodeBuild::Project` | Ejecuta tarea efímera de migración EF Core bundle | — |
