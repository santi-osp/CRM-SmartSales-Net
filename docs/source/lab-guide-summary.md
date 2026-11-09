# Guía 181128 aportada por el usuario — alcance aplicado

El usuario pegó el 2026-11-08 la guía completa «Creación de microservicios y una canalización de CI/CD con AWS»
y aclaró que el docente permite adaptar el stack. Indicó expresamente: «No tomes linea base» y después
«No tomes mucho en cuenta lo de las evidencias y eso, lo que el profesor pide es que despleguemos nuestro
proyecto en microservicios».

Para SmartSales, el objetivo académico confirmado es desplegar el CRM propio como microservicios en AWS.
La guía sirve como referencia para dos servicios ECS Fargate, ECR, ALB con rutas, RDS, CodeDeploy blue/green,
CodePipeline y escalado independiente. La aplicación de cafetería, su monolito Node.js, Cloud9, CodeCommit,
sus rutas `/admin/*`, nombres customer/employee y presentación de capturas no son requisitos funcionales
de SmartSales indicados por el docente. No se construirá una línea base monolítica.

La guía describe cuatro target groups (dos por microservicio), un listener de producción y uno de prueba,
AppSpec por servicio y conservación de la versión anterior cinco minutos. ARCH-001 ya contempla CodeDeploy
blue/green y target groups por API; al implementar HU11/HU12 se contrastarán permisos y configuración real.
El ejemplo del laboratorio usa HTTP, mientras ARCH-001 exige HTTPS para credenciales públicas; G-03 sigue abierto.

R1–R6 de la guía son contexto. La matriz mínima de HU09-AC03 se limita a arquitectura operativa,
portabilidad, escalado y despliegue automático. Costos y evidencias se documentarán de forma proporcionada
al entregable, sin reproducir todas las tareas tutoriales ni afirmar ejecución no observada.
