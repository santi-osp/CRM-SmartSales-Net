# Guía de Teardown y Limpieza de Costos — SmartSales CRM

Instrucciones para desmantelar la infraestructura en AWS Academy de forma ordenada, evitando cargos remanentes de laboratorio.

---

## 1. Orden de Eliminación por Dependencias

Para evitar errores de dependencias de CloudFormation, los stacks deben eliminarse en **orden inverso** al de creación:

```
[4. Delivery Stack] -> [3. Application Stack] -> [2. Data Stack] -> [1. Network Stack]
```

---

## 2. Ejecución con el Script Seguro

El repositorio incluye un script con doble confirmación de seguridad para evitar eliminaciones accidentales:

```powershell
pwsh -File deploy/aws/teardown.ps1 -ConfirmAWSTeardown -EnvironmentName smartsales-prod -Region us-east-1
```
El script solicitará explícitamente escribir `ELIMINAR` antes de ejecutar cualquier comando en AWS.

---

## 3. Comandos Manuales Paso a Paso

Si se prefiere ejecutar manualmente desde AWS CLI:

### Paso 1: Vaciar Bucket S3 de Releases y Eliminar Stack Delivery
Los buckets de S3 con objetos versionados no pueden eliminarse por CloudFormation sin vaciarse previamente:
```bash
BUCKET_NAME=$(aws cloudformation describe-stacks --stack-name smartsales-prod-delivery --query "Stacks[0].Outputs[?OutputKey=='ReleaseBucketName'].OutputValue" --output text)

# Eliminar todas las versiones de objetos en el bucket
aws s3api delete-objects --bucket "$BUCKET_NAME" \
  --delete "$(aws s3api list-object-versions --bucket "$BUCKET_NAME" --query='{Objects: Versions[].{Key:Key,VersionId:VersionId}}' --output json)" 2>/dev/null || true

# Eliminar marcadores de borrado
aws s3api delete-objects --bucket "$BUCKET_NAME" \
  --delete "$(aws s3api list-object-versions --bucket "$BUCKET_NAME" --query='{Objects: DeleteMarkers[].{Key:Key,VersionId:VersionId}}' --output json)" 2>/dev/null || true

# Eliminar stack delivery
aws cloudformation delete-stack --stack-name smartsales-prod-delivery --region us-east-1
aws cloudformation wait stack-delete-complete --stack-name smartsales-prod-delivery --region us-east-1
```

### Paso 2: Eliminar Stack de Aplicación
```bash
aws cloudformation delete-stack --stack-name smartsales-prod-app --region us-east-1
aws cloudformation wait stack-delete-complete --stack-name smartsales-prod-app --region us-east-1
```

### Paso 3: Eliminar Stack de Datos (PostgreSQL RDS)
*Nota:* `data.yaml` tiene configurado `DeletionPolicy: Snapshot`. Si se desea omitir el snapshot final durante la limpieza de laboratorio:
```bash
aws cloudformation delete-stack --stack-name smartsales-prod-data --region us-east-1
aws cloudformation wait stack-delete-complete --stack-name smartsales-prod-data --region us-east-1
```

### Paso 4: Eliminar Stack de Red (VPC)
```bash
aws cloudformation delete-stack --stack-name smartsales-prod-network --region us-east-1
aws cloudformation wait stack-delete-complete --stack-name smartsales-prod-network --region us-east-1
```

---

## 4. Verificación de Recursos Remanentes

Comprobar que no queden recursos huérfanos:
```bash
# Verificar que no queden balanceadores activos
aws elbv2 describe-load-balancers --region us-east-1 --query "LoadBalancers[?contains(LoadBalancerName, 'smartsales')]"

# Verificar instancias RDS
aws rds describe-db-instances --region us-east-1 --query "DBInstances[?contains(DBInstanceIdentifier, 'smartsales')]"

# Verificar clúster ECS
aws ecs describe-clusters --clusters smartsales-prod-cluster --region us-east-1
```
