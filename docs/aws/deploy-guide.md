# Guía de Despliegue en AWS — SmartSales CRM (ARCH-001 v1)

Guía paso a paso para ejecutar el aprovisionamiento y despliegue cuando se disponga de la autorización explícita del propietario y una sesión válida en AWS.

---

## 1. Requisitos Previos (Preflight)

Antes de cualquier despliegue, ejecutar el preflight check local:
```powershell
pwsh -File deploy/aws/preflight.ps1
```
Para validar la identidad y credenciales activas en AWS CLI (modo solo lectura):
```powershell
pwsh -File deploy/aws/preflight.ps1 -CheckRemote
```

Asegurarse de contar con:
- AWS CLI configurado con sesión activa en `us-east-1`.
- Certificado ACM creado o validado para el dominio del ALB (si aplica).
- Rol `LabRole` o roles de ejecución disponibles.

---

## 2. Secuencia de Despliegue de Infraestructura

### Paso 1: Stack de Red (VPC, Subredes y Security Groups)
```bash
aws cloudformation create-stack \
  --stack-name smartsales-prod-network \
  --template-body file://infra/cloudformation/network.yaml \
  --parameters ParameterKey=EnvironmentName,ParameterValue=smartsales-prod \
  --region us-east-1

aws cloudformation wait stack-create-complete --stack-name smartsales-prod-network --region us-east-1
```

### Paso 2: Stack de Datos (PostgreSQL 17 RDS)
Generar una contraseña segura aleatoria para el usuario maestro:
```bash
MASTER_PASSWORD=$(openssl rand -base64 24)

aws cloudformation create-stack \
  --stack-name smartsales-prod-data \
  --template-body file://infra/cloudformation/data.yaml \
  --parameters ParameterKey=EnvironmentName,ParameterValue=smartsales-prod \
               ParameterKey=NetworkStackName,ParameterValue=smartsales-prod-network \
               ParameterKey=MasterUserPassword,ParameterValue="${MASTER_PASSWORD}" \
  --region us-east-1

aws cloudformation wait stack-create-complete --stack-name smartsales-prod-data --region us-east-1
```

### Paso 3: Inicialización de Bases Lógicas y Migraciones
Conectar administrativamente mediante bastion o tarea Fargate efímera y ejecutar `deploy/local/init-dbs.sql` para crear las bases lógicas `contacts_db` y `sales_db` con sus usuarios independientes `contacts_app` y `sales_app`.
Posteriormente, ejecutar las migraciones EF Core con el bundle `/app/migrate` de cada microservicio.

### Paso 4: Stack de Delivery (ECR y S3 Versionado)
```bash
aws cloudformation create-stack \
  --stack-name smartsales-prod-delivery \
  --template-body file://infra/cloudformation/delivery.yaml \
  --parameters ParameterKey=EnvironmentName,ParameterValue=smartsales-prod \
               ParameterKey=CodeBuildServiceRoleArn,ParameterValue="arn:aws:iam::<ACCOUNT_ID>:role/LabRole" \
  --region us-east-1

aws cloudformation wait stack-create-complete --stack-name smartsales-prod-delivery --region us-east-1
```

### Paso 5: Publicación de Imágenes en ECR
Construir y etiquetar las tres imágenes Docker con el commit SHA de Git, y subirlas a los repositorios ECR recién creados:
```bash
aws ecr get-login-password --region us-east-1 | docker login --username AWS --password-stdin <ACCOUNT_ID>.dkr.ecr.us-east-1.amazonaws.com

# Contacts API
docker build -t <ACCOUNT_ID>.dkr.ecr.us-east-1.amazonaws.com/smartsales-prod-contacts:<COMMIT_SHA> -f src/Contacts/SmartSales.Contacts.Api/Dockerfile .
docker push <ACCOUNT_ID>.dkr.ecr.us-east-1.amazonaws.com/smartsales-prod-contacts:<COMMIT_SHA>

# Sales API
docker build -t <ACCOUNT_ID>.dkr.ecr.us-east-1.amazonaws.com/smartsales-prod-sales:<COMMIT_SHA> -f src/Sales/SmartSales.Sales.Api/Dockerfile .
docker push <ACCOUNT_ID>.dkr.ecr.us-east-1.amazonaws.com/smartsales-prod-sales:<COMMIT_SHA>

# Web Angular
docker build -t <ACCOUNT_ID>.dkr.ecr.us-east-1.amazonaws.com/smartsales-prod-web:<COMMIT_SHA> -f src/Web/Dockerfile src/Web
docker push <ACCOUNT_ID>.dkr.ecr.us-east-1.amazonaws.com/smartsales-prod-web:<COMMIT_SHA>
```

### Paso 6: Stack de Aplicación (ALB, Target Groups y ECS Services)
```bash
aws cloudformation create-stack \
  --stack-name smartsales-prod-app \
  --template-body file://infra/cloudformation/application.yaml \
  --parameters ParameterKey=EnvironmentName,ParameterValue=smartsales-prod \
               ParameterKey=NetworkStackName,ParameterValue=smartsales-prod-network \
               ParameterKey=DataStackName,ParameterValue=smartsales-prod-data \
               ParameterKey=ExecutionRoleArn,ParameterValue="arn:aws:iam::<ACCOUNT_ID>:role/LabRole" \
               ParameterKey=TaskRoleArn,ParameterValue="arn:aws:iam::<ACCOUNT_ID>:role/LabRole" \
               ParameterKey=ContactsImage,ParameterValue="<ACCOUNT_ID>.dkr.ecr.us-east-1.amazonaws.com/smartsales-prod-contacts:<COMMIT_SHA>" \
               ParameterKey=SalesImage,ParameterValue="<ACCOUNT_ID>.dkr.ecr.us-east-1.amazonaws.com/smartsales-prod-sales:<COMMIT_SHA>" \
               ParameterKey=WebImage,ParameterValue="<ACCOUNT_ID>.dkr.ecr.us-east-1.amazonaws.com/smartsales-prod-web:<COMMIT_SHA>" \
  --region us-east-1

aws cloudformation wait stack-create-complete --stack-name smartsales-prod-app --region us-east-1
```

---

## 3. Verificación Post-Despliegue

Obtener la URL pública del ALB y ejecutar el script de verificación y smoke tests:
```powershell
$albDns = (aws cloudformation describe-stacks --stack-name smartsales-prod-app --query "Stacks[0].Outputs[?OutputKey=='AlbDnsName'].OutputValue" --output text)
pwsh -File deploy/aws/verify.ps1 -AlbBaseUrl "http://$albDns"
```
