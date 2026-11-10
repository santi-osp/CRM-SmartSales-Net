# Matriz de Permisos y Roles AWS — SmartSales CRM (ARCH-001 v1)

Este documento especifica los roles IAM y políticas de mínimo privilegio requeridas para el aprovisionamiento y ejecución en AWS, contrastando con el entorno de AWS Academy.

---

## 1. Roles requeridos y políticas mínimas

### A. Execution Role (`ExecutionRoleArn`)
Utilizado por el agente de ECS para preparar la tarea Fargate antes de iniciar el contenedor.
- **Acciones necesarias:**
  - `ecr:GetAuthorizationToken`
  - `ecr:BatchCheckLayerAvailability`, `ecr:GetDownloadUrlForLayer`, `ecr:BatchGetImage`
  - `logs:CreateLogStream`, `logs:PutLogEvents`
  - `ssm:GetParameters`, `ssm:GetParameter` (para inyección de connection strings y claves JWT)
  - `kms:Decrypt` (si los parámetros SSM usan clave KMS personalizada)

### B. Task Role (`TaskRoleArn`)
Asignado al proceso en ejecución dentro del contenedor Kestrel / Nginx.
- **Acciones necesarias:**
  - Ninguna llamada a la API de AWS es requerida para el CRUD normal de SmartSales CRM (principio de mínimo privilegio).
  - Si en el futuro se incorpora envío de métricas custom o trazas con AWS X-Ray, se agregará `xray:PutTraceSegments`.

### C. Rol de CodeBuild para Migraciones (`CodeBuildServiceRoleArn`)
- **Acciones necesarias:**
  - `ecs:RunTask` (en el cluster de SmartSales, restringido a la task definition del migrador).
  - `ecs:DescribeTasks`
  - `iam:PassRole` sobre `ExecutionRoleArn` y `TaskRoleArn`.
  - `logs:CreateLogGroup`, `logs:CreateLogStream`, `logs:PutLogEvents`.

### D. Credenciales de Azure Pipelines hacia AWS (Gate G-04)
- **Acciones necesarias:**
  - `ecr:GetAuthorizationToken`
  - `ecr:PutImage`, `ecr:InitiateLayerUpload`, `ecr:UploadLayerPart`, `ecr:CompleteLayerUpload`
  - `s3:PutObject`, `s3:PutObjectVersionAcl` sobre el bucket de releases
  - `codepipeline:StartPipelineExecution`

---

## 2. Restricciones y consideraciones en AWS Academy

1. **Uso de `LabRole`:**
   En cuentas de estudiante de AWS Academy, la creación de nuevos roles y políticas IAM suele estar bloqueada por la SCP (`Service Control Policy`). En su lugar, el entorno provee un rol preconfigurado llamado `LabRole` con permisos amplios preexistentes.
2. **Expiración de credenciales temporales:**
   Las credenciales de AWS Academy (AccessKeyId, SecretAccessKey y SessionToken) expiran cada **3 a 4 horas**. Por tanto, cualquier integración continua automatizada requiere renovar el token de sesión en las variables de Azure Pipelines antes de cada ejecución.
3. **Imposibilidad de OIDC / Proveedor de Identidad Externo:**
   AWS Academy no permite registrar proveedores OIDC para GitHub o Azure DevOps. La autenticación debe manejarse mediante variables de entorno secretas temporales del laboratorio.
