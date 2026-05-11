# Spec — nexa-learn

**Fecha de creación**: 2026-05-11
**Estado**: Aprobada
**Autor**: Alejandro Martin Achinelli

---

## Qué problema resuelve este proyecto

nexa-learn es el proyecto fundacional del portfolio técnico. No resuelve un
problema de negocio real: resuelve el problema de demostrar, de forma concreta
y evaluable, dominio de C# moderno, patrones de arquitectura limpia y buenas
prácticas de ingeniería de software con .NET 8.

El proyecto debe poder ser navegado por un arquitecto de software o un
tech lead senior y permitirle evaluar, en menos de 30 minutos, el nivel
de madurez técnica del autor.

---

## Dominio de negocio

### Descripción

Plataforma de gestión de cursos de aprendizaje. Dos actores principales:

- **Instructor**: crea cursos, organiza módulos y lecciones, publica cursos.
- **Estudiante**: se registra, explora cursos publicados, se inscribe, marca
  lecciones como completadas.

### Aggregates y entidades principales

| Aggregate | Entidades internas | Notas |
|-----------|-------------------|-------|
| `Course` | `Module`, `Lesson` | Raíz del aggregate. Controla publicación. |
| `Student` | — | Gestiona inscripciones desde su lado. |
| `Enrollment` | — | Relaciona Student con Course. Registra progreso. |

### Value Objects

| Value Object | Tipo base | Invariante |
|---|---|---|
| `Email` | `string` | Formato válido de email |
| `CourseTitle` | `string` | No vacío, máximo 200 caracteres |
| `Duration` | `int` | Minutos positivos |
| `Money` | `decimal` + `string` (currency) | Monto ≥ 0, currency no vacío |

### Domain Events

| Evento | Generado por | Significado |
|---|---|---|
| `CoursePublished` | `Course.Publish()` | El curso está disponible para inscripciones |
| `StudentEnrolled` | `Enrollment.Create()` | Un estudiante se inscribió en un curso |
| `LessonCompleted` | `Enrollment.CompleteLesson()` | Un estudiante completó una lección |

### Reglas de negocio centrales

1. Un curso solo puede publicarse si tiene al menos un módulo con al menos
   una lección.
2. Un estudiante no puede inscribirse en un curso que no esté publicado.
3. Un estudiante no puede inscribirse dos veces en el mismo curso.
4. Una lección solo puede marcarse como completada si el estudiante está
   inscrito en el curso al que pertenece esa lección.
5. Un curso publicado no puede volver a estado borrador (publicación es
   irreversible en este modelo).

---

## Patrones que se demuestran y en qué orden

### Etapa 1 — Domain layer (fundamentos)

**Objetivo**: dominio rico, sin dependencias externas, completamente testeable
en aislamiento.

Patrones introducidos:
- **Entidades con invariantes**: el constructor y los métodos de dominio
  validan y protegen el estado interno.
- **Value Objects**: inmutables, sin identidad, validación en construcción.
- **Result Pattern**: todos los métodos de dominio que pueden fallar retornan
  `Result<T>` en lugar de lanzar excepciones.
- **Domain Events**: definición de los eventos como clases del dominio
  (sin mecanismo de dispatch todavía).

Criterio de completitud: todos los aggregates, value objects y reglas de
negocio tienen tests unitarios. La capa compila sin referencias a ningún
NuGet externo que no sea primitivos del lenguaje.

---

### Etapa 2 — Application layer (casos de uso)

**Objetivo**: casos de uso expresivos, independientes de infraestructura,
con contratos claros.

Patrones introducidos:
- **CQRS con MediatR**: commands que mutan estado, queries que leen estado.
  Separación estricta.
- **Commands**: `CreateCourse`, `PublishCourse`, `EnrollStudent`,
  `CompleteLesson`.
- **Queries**: `GetCourseById`, `ListPublishedCourses`, `GetStudentProgress`.
- **Validators con FluentValidation**: validación de inputs antes de que
  lleguen al dominio.
- **DTOs con mapeo explícito**: ninguna entidad de dominio se expone
  directamente. Cada DTO tiene su método de mapeo estático.
- **Interfaces de repositorio**: definidas en Application, implementadas
  en Infrastructure. Los handlers conocen la interfaz, no la implementación.

Criterio de completitud: todos los handlers tienen tests con repositorios
en memoria. Los tests verifican tanto el happy path como los casos de error
usando el Result Pattern.

---

### Etapa 3 — Infrastructure layer (persistencia)

**Objetivo**: implementaciones concretas que satisfacen las interfaces del
dominio, sin filtrar detalles de ORM hacia arriba.

Patrones introducidos:
- **Repository + Unit of Work**: implementaciones concretas con EF Core.
  El Unit of Work encapsula el `DbContext` y el commit de la transacción.
- **EF Core Fluent API**: configuración de entidades sin data annotations
  en las clases de dominio. El dominio no sabe que existe EF.
- **Options Pattern**: configuración de cadena de conexión y otros settings
  de infraestructura mediante `IOptions<T>` con validación en startup.
- **Migrations**: historial de esquema versionado con EF Core migrations.

Tecnología: PostgreSQL 16 via Npgsql.

Criterio de completitud: tests de integración con Testcontainers levantan
una instancia real de PostgreSQL. Los tests verifican que los repositorios
persisten y recuperan datos correctamente.

---

### Etapa 4 — API layer + cross-cutting concerns

**Objetivo**: superficie pública de la API con manejo robusto de errores,
observabilidad y seguridad básica.

Patrones introducidos:
- **Minimal APIs organizadas por feature**: extension methods sobre
  `IEndpointRouteBuilder`. Un archivo por aggregate (`CourseEndpoints`,
  `StudentEndpoints`, `EnrollmentEndpoints`).
- **Middleware de manejo de errores global**: captura excepciones no
  esperadas y las convierte en respuestas HTTP estructuradas con
  `ProblemDetails` (RFC 7807). El Result Pattern del dominio se traduce
  a códigos HTTP en los endpoints, no en el middleware.
- **Decorator Pattern con MediatR Pipeline Behaviors**: logging de todos
  los commands y queries sin tocar los handlers. Se agregan behaviors para
  logging, validación y (futuro) performance monitoring.
- **JWT authentication**: los endpoints de escritura requieren token válido.
  Los handlers no conocen la identidad del caller — la resolución del
  `StudentId` desde el token ocurre en el endpoint, no en el caso de uso.

Criterio de completitud: tests de integración con `WebApplicationFactory`
verifican los endpoints completos. La documentación OpenAPI (Swagger)
está generada y es navegable.

---

### Etapa 5 — Observabilidad y CI

**Objetivo**: el sistema es observable en producción y el pipeline de CI
valida cada push.

Patrones introducidos:
- **Outbox Pattern**: los domain events se persisten en la misma transacción
  que la operación de negocio. Un worker los procesa y los despacha de forma
  eventual. Garantía: ningún evento se pierde si el proceso falla entre el
  commit y el dispatch.
- **OpenTelemetry**: traces y métricas básicas de los endpoints y los
  handlers de MediatR.
- **GitHub Actions CI**: build, test y cobertura en cada push a main y en
  cada pull request.

Criterio de completitud: el pipeline de CI corre en verde. Los traces son
visibles en un collector local (OTEL collector via docker-compose).

---

## Estructura de carpetas

```
nexa-learn/
├── src/
│   ├── NexaLearn.Domain/
│   │   ├── Aggregates/
│   │   │   ├── Courses/
│   │   │   │   ├── Course.cs
│   │   │   │   ├── Module.cs
│   │   │   │   ├── Lesson.cs
│   │   │   │   └── Events/
│   │   │   ├── Students/
│   │   │   │   └── Student.cs
│   │   │   └── Enrollments/
│   │   │       ├── Enrollment.cs
│   │   │       └── Events/
│   │   ├── ValueObjects/
│   │   │   ├── Email.cs
│   │   │   ├── CourseTitle.cs
│   │   │   ├── Duration.cs
│   │   │   └── Money.cs
│   │   ├── Common/
│   │   │   ├── Entity.cs
│   │   │   ├── AggregateRoot.cs
│   │   │   ├── ValueObject.cs
│   │   │   ├── IDomainEvent.cs
│   │   │   └── Result.cs
│   │   └── Interfaces/
│   │       ├── ICourseRepository.cs
│   │       ├── IStudentRepository.cs
│   │       └── IEnrollmentRepository.cs
│   │
│   ├── NexaLearn.Application/
│   │   ├── Courses/
│   │   │   ├── Commands/
│   │   │   │   ├── CreateCourse/
│   │   │   │   └── PublishCourse/
│   │   │   ├── Queries/
│   │   │   │   ├── GetCourseById/
│   │   │   │   └── ListPublishedCourses/
│   │   │   └── DTOs/
│   │   ├── Students/
│   │   │   └── Commands/
│   │   │       └── RegisterStudent/
│   │   ├── Enrollments/
│   │   │   ├── Commands/
│   │   │   │   ├── EnrollStudent/
│   │   │   │   └── CompleteLesson/
│   │   │   └── Queries/
│   │   │       └── GetStudentProgress/
│   │   └── Common/
│   │       ├── Behaviors/
│   │       │   ├── LoggingBehavior.cs
│   │       │   └── ValidationBehavior.cs
│   │       └── Interfaces/
│   │           └── IUnitOfWork.cs
│   │
│   ├── NexaLearn.Infrastructure/
│   │   ├── Persistence/
│   │   │   ├── NexaLearnDbContext.cs
│   │   │   ├── Configurations/
│   │   │   ├── Repositories/
│   │   │   └── Migrations/
│   │   ├── Outbox/
│   │   └── DependencyInjection.cs
│   │
│   └── NexaLearn.Api/
│       ├── Endpoints/
│       │   ├── CourseEndpoints.cs
│       │   ├── StudentEndpoints.cs
│       │   └── EnrollmentEndpoints.cs
│       ├── Middleware/
│       │   └── GlobalExceptionHandler.cs
│       ├── Program.cs
│       └── appsettings.json
│
├── tests/
│   ├── NexaLearn.Domain.Tests/
│   │   ├── Aggregates/
│   │   └── ValueObjects/
│   ├── NexaLearn.Application.Tests/
│   │   ├── Courses/
│   │   ├── Students/
│   │   └── Enrollments/
│   └── NexaLearn.Infrastructure.Tests/
│       └── Repositories/
│
├── docs/
│   ├── adr/
│   │   └── 001-decisiones-arquitectura-base.md
│   ├── templates/
│   │   ├── ADR-template.md
│   │   └── README-template.md
│   ├── guides/
│   └── spec.md
│
├── docker/
│   └── docker-compose.yml
│
├── NexaLearn.sln
├── CLAUDE.md
└── README.md
```

### Criterio de organización de carpetas

- `Domain`: cero dependencias externas. Si algo en esta carpeta requiere un
  NuGet que no sea primitivos del lenguaje, hay un problema de diseño.
- `Application`: organizado por aggregate/feature, no por tipo técnico. Los
  commands y queries de `Courses` están en `Application/Courses/`, no en
  `Application/Commands/`.
- `Infrastructure`: implementa interfaces. No define ningún contrato nuevo
  que no exista ya en `Domain` o `Application`.
- `Api`: referencia `Infrastructure` únicamente para el registro de DI en
  `Program.cs`. Ninguna clase de `Api` fuera de ese archivo conoce tipos
  concretos de `Infrastructure`.
- `tests/`: separado por capa. El nivel de aislamiento de cada proyecto de
  tests es una decisión explícita, no accidental.

---

## Restricciones y decisiones fijas

| Tema | Decisión | Referencia |
|---|---|---|
| Runtime | .NET 8 LTS | ADR-001 |
| Base de datos | PostgreSQL 16 | ADR-001 |
| ORM | EF Core 8 con Fluent API | ADR-001 |
| API style | Minimal APIs | ADR-001 |
| Mapeo | Explícito, sin AutoMapper | ADR-001 |
| Errores de negocio | Result Pattern, sin excepciones | ADR-001 |
| Autenticación | JWT en Etapa 4 | ADR-001 |
| Idioma del código | Inglés | CLAUDE.md |
| Idioma de docs | Español | CLAUDE.md |
| Commits | Conventional Commits | CLAUDE.md |

---

## Lo que esta spec no cubre (fuera de alcance)

- Autorización por roles (queda para nexa-core)
- Pagos y facturación (queda para nexa-bank)
- Recomendaciones por IA (queda para nexa-ai)
- Frontend (este proyecto es API-only)
- Alta disponibilidad y escalabilidad horizontal
- Rate limiting y throttling

---

## Próximo paso

Con esta spec aprobada, el siguiente artefacto es `docs/plan.md`:
el plan de tareas con criterios de éxito verificables y tests que los validan,
siguiendo el flujo de spec-driven development definido en CLAUDE.md.
