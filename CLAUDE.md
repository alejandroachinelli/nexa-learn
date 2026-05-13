# Contexto del proyecto — Nexa Portfolio

## Quién soy
Desarrollador backend con experiencia en SQL Server y ecosistema .NET,
en proceso de convertirme en arquitecto de software. Trabajo con C#, .NET,
y estoy incorporando Node.js, Python, PostgreSQL y todo el stack DevOps.
Nivel: intermedio-avanzado. Aprendo rápido, prefiero entender el por qué
antes de implementar.

## Qué es este repositorio
Carpeta raíz de mi portfolio técnico profesional. Contiene múltiples
proyectos que demuestran arquitectura de software, buenas prácticas,
patrones de diseño y uso de tecnologías modernas. Todo está documentado
en español.

## Proyectos en este portfolio
- `projects/nexa-learn/` — proyecto de fundamentos: C# moderno, patrones,
  arquitectura limpia. Base de todo lo demás.
- `projects/nexa-core/` — ERP modular (próximo)
- `projects/nexa-bank/` — plataforma bancaria (próximo)
- `projects/nexa-ai/` — integración con IA (próximo)
- `projects/nexa-folio/` — portfolio web personal (próximo)

## Stack tecnológico principal
- Backend: C# / .NET 8 (LTS), Node.js 20, Python 3.12
- Frontend: React 18, Next.js 14, TypeScript
- Bases de datos: PostgreSQL 16, MongoDB, Redis
- Mensajería: Kafka, RabbitMQ (vía MassTransit)
- DevOps: Docker, Kubernetes, GitHub Actions, Terraform
- Observabilidad: OpenTelemetry, Prometheus, Grafana
- IA: Anthropic Claude API, Semantic Kernel

## Arquitectura base que usamos en todos los proyectos .NET
Clean Architecture con estas capas:
- Domain: entidades, value objects, interfaces, domain events
- Application: use cases, CQRS (commands/queries), DTOs, validators
- Infrastructure: implementaciones concretas, repositorios, EF Core, servicios externos
- API: controllers o minimal APIs, middleware, configuración

## Patrones que aplicamos siempre
- CQRS con MediatR
- Repository + Unit of Work
- Result Pattern (no exceptions para flujo de negocio)
- Outbox Pattern para eventos de dominio
- Options Pattern para configuración
- Decorator Pattern para cross-cutting concerns

## Convenciones de código
- Idioma del código: inglés (variables, clases, métodos, comentarios inline)
- Idioma de documentación: español (README, ADRs, comentarios explicativos, tener en cuenta acentos y ñ)
- Commits en inglés siguiendo Conventional Commits:
  feat: add user authentication
  fix: resolve null reference in order service
  docs: update architecture diagram
  refactor: extract payment processor interface
  test: add unit tests for order aggregate
- Branches: feature/nombre-descriptivo, fix/descripcion, docs/tema
- Los commits siempre se hacen con el autor configurado en git local. No agregar
  Co-authored-by ni referencias a Claude en los mensajes de commit. El código es
  mío, Claude es una herramienta de desarrollo como cualquier otra.

## Cómo trabajo con Claude CLI
Cuando me ayudás a generar código:
1. Siempre explicá el patrón o decisión antes de mostrar el código
2. Si hay múltiples formas de hacer algo, explicá por qué elegimos esta
3. El código debe estar listo para producción: sin TODO pendientes sin aclarar,
   con manejo de errores, con logging apropiado
4. Después de generar código, sugerí qué documentar (ADR, README, etc.)
5. Usá siempre el mismo estilo que ya existe en el proyecto

## Decisiones técnicas tomadas
- .NET 8 como target: es el LTS vigente, mercado lo adopta masivamente
- PostgreSQL como DB principal: features enterprise, row-level security,
  pgvector para IA
- MongoDB para logs de auditoría: append-only, semi-estructurado, ideal
- No usar excepciones para flujo de negocio: Result Pattern siempre

## Lo que NO hacemos
- No ponemos lógica de negocio en Controllers
- No exponemos entidades de dominio directamente en la API (siempre DTOs)
- No subimos .env al repositorio jamás
- No generamos código sin entender qué hace y por qué

## Flujo de trabajo con Claude CLI — spec-driven development

Para cada feature o módulo nuevo seguimos este flujo obligatorio:

### Fase 1 — Brainstorming y spec
Antes de escribir código, Claude pregunta:
- ¿Qué problema resuelve esto exactamente?
- ¿Qué alternativas de diseño existen? (siempre al menos 2)
- ¿Qué restricciones tenemos? (performance, seguridad, compatibilidad)
El output es un archivo `spec.md` en la carpeta del feature.

### Fase 2 — Plan de tareas
La spec aprobada se convierte en tareas pequeñas y verificables.
Cada tarea tiene: descripción, criterio de éxito, test que lo valida.
Output: `plan.md` en la carpeta del feature.

### Fase 3 — Implementación con TDD
Ciclo red-green-refactor para cada tarea del plan.
Primero se escribe el test que falla, después el código que lo pasa,
después se refactoriza si es necesario.

### Fase 4 — Review y documentación
Antes de hacer commit: revisar que no hay código muerto, que los
nombres son claros, que el ADR está escrito si hubo decisión técnica.

## Skills de Claude Code instaladas
- .NET / C# patterns y convenciones
- PostgreSQL y EF Core
- Clean Architecture
- Docker y docker-compose
(Se instalan manualmente en cada proyecto, ver docs/guides/claude-setup.md)