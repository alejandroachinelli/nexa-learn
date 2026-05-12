# Plan de implementación — Etapa 2: Application Layer

**Fecha**: 2026-05-12
**Etapa**: 2 de 5 — Application Layer
**Spec de referencia**: docs/spec.md
**ADR de referencia**: docs/adr/001-decisiones-arquitectura-base.md
**Prerequisito**: Etapa 1 completa (118 tests en verde)

---

## Reglas de esta etapa

- Ciclo obligatorio: **red → green → refactor**. El test se escribe primero.
- Los handlers se testean con **repositorios en memoria**, nunca con mocks.
  Un mock que devuelve lo que le pedís no detecta que tu query está mal.
  Un repositorio en memoria sí, porque tenés que haberlo cargado antes.
- Ningún handler lanza excepciones para flujo de negocio. Todos retornan
  `Result` o `Result<T>`.
- Los DTOs tienen mapeo estático explícito. Sin AutoMapper.
- Los commands y queries son records inmutables.
- Orden de implementación: las tareas están numeradas por dependencia.

---

## Estructura de carpetas que produce esta etapa

```
src/NexaLearn.Application/
├── Common/
│   ├── Interfaces/
│   │   └── IUnitOfWork.cs
│   └── Behaviors/
│       ├── ValidationBehavior.cs
│       └── LoggingBehavior.cs
├── Courses/
│   ├── Commands/
│   │   ├── CreateCourse/
│   │   │   ├── CreateCourseCommand.cs
│   │   │   ├── CreateCourseCommandHandler.cs
│   │   │   └── CreateCourseCommandValidator.cs
│   │   └── PublishCourse/
│   │       ├── PublishCourseCommand.cs
│   │       └── PublishCourseCommandHandler.cs
│   ├── Queries/
│   │   ├── GetCourseById/
│   │   │   ├── GetCourseByIdQuery.cs
│   │   │   └── GetCourseByIdQueryHandler.cs
│   │   └── ListPublishedCourses/
│   │       ├── ListPublishedCoursesQuery.cs
│   │       └── ListPublishedCoursesQueryHandler.cs
│   └── DTOs/
│       ├── CourseDto.cs
│       └── CourseDetailDto.cs
├── Students/
│   └── Commands/
│       └── RegisterStudent/
│           ├── RegisterStudentCommand.cs
│           ├── RegisterStudentCommandHandler.cs
│           └── RegisterStudentCommandValidator.cs
└── Enrollments/
    ├── Commands/
    │   ├── EnrollStudent/
    │   │   ├── EnrollStudentCommand.cs
    │   │   └── EnrollStudentCommandHandler.cs
    │   └── CompleteLesson/
    │       ├── CompleteLessonCommand.cs
    │       └── CompleteLessonCommandHandler.cs
    └── Queries/
        └── GetStudentProgress/
            ├── GetStudentProgressQuery.cs
            ├── GetStudentProgressQueryHandler.cs
            └── StudentProgressDto.cs

tests/NexaLearn.Application.Tests/
├── Infrastructure/                     ← repositorios en memoria (compartidos)
│   ├── InMemoryCourseRepository.cs
│   ├── InMemoryStudentRepository.cs
│   ├── InMemoryEnrollmentRepository.cs
│   └── InMemoryUnitOfWork.cs
├── Courses/
│   ├── CreateCourseCommandHandlerTests.cs
│   ├── PublishCourseCommandHandlerTests.cs
│   ├── GetCourseByIdQueryHandlerTests.cs
│   └── ListPublishedCoursesQueryHandlerTests.cs
├── Students/
│   └── RegisterStudentCommandHandlerTests.cs
├── Enrollments/
│   ├── EnrollStudentCommandHandlerTests.cs
│   ├── CompleteLessonCommandHandlerTests.cs
│   └── GetStudentProgressQueryHandlerTests.cs
└── Behaviors/
    ├── ValidationBehaviorTests.cs
    └── LoggingBehaviorTests.cs
```

---

## Tareas

### T1 — Configuración del proyecto Application

**Qué implementar**

1. Crear proyecto `NexaLearn.Application` (classlib, net8.0) en `src/`.
2. Crear proyecto `NexaLearn.Application.Tests` (xunit, net8.0) en `tests/`.
3. Agregar paquetes al proyecto Application:
   - `MediatR` — dispatcher de commands y queries
   - `FluentValidation` — validación de inputs
   - `Microsoft.Extensions.Logging.Abstractions` — para LoggingBehavior
4. Agregar paquetes al proyecto de tests:
   - `FluentAssertions 7.0.0` — consistente con Domain.Tests
   - `Microsoft.Extensions.Logging.Abstractions` — para tests de behaviors
5. Referencias:
   - `Application` → `Domain`
   - `Application.Tests` → `Application`, `Domain`
6. Agregar ambos proyectos al `NexaLearn.slnx`.

**Por qué existe esta tarea**

Sin el proyecto configurado con las dependencias correctas, nada de lo que
sigue compila. MediatR es el dispatcher central de CQRS. FluentValidation
es el motor de validación que usa `ValidationBehavior`. Las abstracciones
de logging permiten testear `LoggingBehavior` sin un logger real.

**Criterio de éxito**

- `dotnet build NexaLearn.slnx` compila sin errores ni warnings.
- `NexaLearn.Application` no referencia `NexaLearn.Infrastructure`.
- Los paquetes están instalados en las versiones correctas.

**Tests a escribir**

No hay tests unitarios para esta tarea. La verificación es la compilación
y la ejecución de `dotnet list package` para confirmar las versiones.

---

### T2 — IUnitOfWork

**Qué implementar**

`IUnitOfWork` en `NexaLearn.Application/Common/Interfaces/IUnitOfWork.cs`:

```
Task<int> SaveChangesAsync(CancellationToken ct);
```

**Por qué existe esta tarea**

Los handlers de commands necesitan un mecanismo para persistir cambios
al finalizar su trabajo. `IUnitOfWork` encapsula ese commit. Definirlo
en Application (no en Infrastructure) mantiene la dirección de dependencias:
el handler conoce el contrato, no la implementación con EF Core.

Un handler típico hace: carga entidades via repositorios, ejecuta lógica
de dominio, y llama a `unitOfWork.SaveChangesAsync()`. Si el `SaveChanges`
falla, los cambios no se persisten y el handler puede retornar un error.

**Criterio de éxito**

- La interfaz compila sin referencias a EF Core ni Npgsql.
- Tiene exactamente un método con `CancellationToken`.

**Tests a escribir**

No hay tests unitarios para una interfaz. La implementación concreta
y sus tests llegan en Etapa 3.

---

### T3 — Repositorios en memoria (infraestructura de tests)

**Qué implementar**

Cuatro clases en `tests/NexaLearn.Application.Tests/Infrastructure/`:

`InMemoryCourseRepository : ICourseRepository`
- Almacena cursos en una `List<Course>` interna.
- `GetByIdAsync`: busca por Id.
- `GetPublishedAsync`: filtra `IsPublished == true`.
- `AddAsync`: agrega a la lista.
- `Update`: no-op (la lista ya tiene la referencia).

`InMemoryStudentRepository : IStudentRepository`
- `GetByIdAsync`, `GetByEmailAsync`, `AddAsync`.

`InMemoryEnrollmentRepository : IEnrollmentRepository`
- `GetByIdAsync`, `GetByStudentAndCourseAsync`, `AddAsync`.

`InMemoryUnitOfWork : IUnitOfWork`
- `SaveChangesAsync`: retorna `Task.FromResult(1)`. No-op intencional:
  los repos en memoria ya tienen las referencias, no hay nada que "commitear".

**Por qué existe esta tarea**

Los tests de handlers necesitan repositorios que funcionen sin base de datos.
Los repositorios en memoria replican el comportamiento real: cargan y
persisten objetos. Esto es fundamentalmente distinto a un mock configurado
para retornar un valor fijo, que no detecta bugs como "el handler nunca
llamó a `AddAsync`".

Esta infraestructura vive en el proyecto de tests, no en Application ni
en Infrastructure. No es código de producción.

**Criterio de éxito**

- Los cuatro tipos compilan e implementan sus interfaces correctamente.
- `InMemoryCourseRepository.GetPublishedAsync` solo retorna cursos publicados.
- `InMemoryEnrollmentRepository.GetByStudentAndCourseAsync` filtra
  por ambos IDs simultáneamente.

**Tests a escribir** (`tests/NexaLearn.Application.Tests/Infrastructure/InMemoryRepositoryTests.cs`)

```
InMemoryCourseRepository_AddAndGetById_ReturnsCorrectCourse
InMemoryCourseRepository_GetPublished_ReturnsOnlyPublishedCourses
InMemoryStudentRepository_GetByEmail_ReturnsCorrectStudent
InMemoryEnrollmentRepository_GetByStudentAndCourse_ReturnsCorrectEnrollment
InMemoryEnrollmentRepository_GetByStudentAndCourse_WrongIds_ReturnsNull
```

---

### T4 — DTOs con mapeo explícito

**Qué implementar**

Cinco DTOs, cada uno con un método estático `FromDomain`:

`CourseDto` en `Application/Courses/DTOs/CourseDto.cs`:
- `Guid Id`, `string Title`, `decimal Price`, `string Currency`,
  `bool IsPublished`
- `static CourseDto FromDomain(Course course)`

`CourseDetailDto` en `Application/Courses/DTOs/CourseDetailDto.cs`:
- Todo lo anterior más `IReadOnlyList<ModuleDto> Modules`
- `static CourseDetailDto FromDomain(Course course)`

`ModuleDto` en `Application/Courses/DTOs/ModuleDto.cs`:
- `Guid Id`, `string Title`, `IReadOnlyList<LessonDto> Lessons`
- `static ModuleDto FromDomain(Module module)`

`LessonDto` en `Application/Courses/DTOs/LessonDto.cs`:
- `Guid Id`, `string Title`, `int DurationMinutes`
- `static LessonDto FromDomain(Lesson lesson)`

`StudentProgressDto` en `Application/Enrollments/Queries/GetStudentProgress/StudentProgressDto.cs`:
- `Guid EnrollmentId`, `Guid CourseId`, `int CompletedLessons`,
  `int TotalLessons`, `IReadOnlyList<Guid> CompletedLessonIds`
- `static StudentProgressDto FromDomain(Enrollment enrollment, int totalLessons)`

**Por qué existe esta tarea**

Los DTOs son el contrato público de la API. Ninguna entidad de dominio
se expone directamente — eso expondría detalles internos y acoplaría
el contrato de la API al modelo de dominio. El mapeo explícito hace que
cada campo expuesto sea una decisión consciente. Si el dominio cambia,
el compilador señala exactamente qué mappings revisar.

**Criterio de éxito**

- Ningún DTO expone tipos del dominio directamente
  (no `CourseTitle`, no `Money`, no `Email` — solo primitivos).
- `CourseDetailDto.FromDomain` mapea todos los módulos y lecciones.
- `StudentProgressDto` calcula correctamente `CompletedLessons`.

**Tests a escribir** (`tests/NexaLearn.Application.Tests/Courses/CourseDtoTests.cs`)

```
CourseDto_FromDomain_MapsAllFields
CourseDetailDto_FromDomain_IncludesModulesAndLessons
CourseDetailDto_FromDomain_EmptyModules_ReturnsEmptyList
StudentProgressDto_FromDomain_CalculatesCompletedLessonsCount
```

---

### T5 — Command: RegisterStudent

**Qué implementar**

Tres archivos en `Application/Students/Commands/RegisterStudent/`:

`RegisterStudentCommand : IRequest<Result<Guid>>`:
- Properties: `Guid StudentId`, `string Name`, `string Email`

`RegisterStudentCommandValidator : AbstractValidator<RegisterStudentCommand>`:
- `Name`: no vacío, máximo 100 caracteres.
- `Email`: no vacío, formato de email válido (FluentValidation built-in).

`RegisterStudentCommandHandler : IRequestHandler<RegisterStudentCommand, Result<Guid>>`:
- Verifica que el email no esté registrado con `IStudentRepository.GetByEmailAsync`.
- Crea el `Email` value object — retorna `Result.Failure` si falla.
- Crea el `Student` con `Student.Create`.
- Persiste con `AddAsync` y `SaveChangesAsync`.
- Retorna `Result<Guid>.Success(command.StudentId)`.

**Por qué existe esta tarea**

Es el caso de uso más simple de escritura. Introduce el patrón completo:
command inmutable → validator → handler → repositorio → unit of work.
Al implementarlo primero se establece la estructura que todos los demás
commands seguirán.

**Criterio de éxito**

- Registrar un estudiante con datos válidos retorna `Result<Guid>` exitoso.
- Registrar con email ya existente retorna `Result.Failure`.
- Registrar con nombre vacío retorna `Result.Failure` (atrapado por el validator
  antes de llegar al handler).
- Registrar con email con formato inválido retorna `Result.Failure`.
- El estudiante queda en el repositorio después del comando exitoso.

**Tests a escribir** (`tests/NexaLearn.Application.Tests/Students/RegisterStudentCommandHandlerTests.cs`)

```
RegisterStudent_ValidCommand_ReturnsSuccessWithStudentId
RegisterStudent_ValidCommand_StudentIsPersistedInRepository
RegisterStudent_DuplicateEmail_ReturnsFailure
RegisterStudent_EmptyName_ValidatorReturnsFailure
RegisterStudent_InvalidEmailFormat_ValidatorReturnsFailure
RegisterStudent_InvalidEmailFormat_HandlerNeverReached
```

---

### T6 — Command: CreateCourse

**Qué implementar**

Tres archivos en `Application/Courses/Commands/CreateCourse/`:

`CreateCourseCommand : IRequest<Result<Guid>>`:
- `Guid CourseId`, `string Title`, `decimal Price`, `string Currency`

`CreateCourseCommandValidator`:
- `Title`: no vacío, máximo 200 caracteres.
- `Price`: mayor o igual a cero.
- `Currency`: exactamente 3 caracteres.

`CreateCourseCommandHandler`:
- Crea `CourseTitle` y `Money` — retorna fallo si alguno falla.
- Crea `Course.Create(...)`.
- Persiste y retorna el Id.

**Por qué existe esta tarea**

Introduce el patrón de crear value objects dentro del handler y manejar
sus posibles fallos. La validación de FluentValidation cuida el formato,
pero las invariantes de negocio las cuida el dominio. Ambas capas tienen
responsabilidades distintas y complementarias.

**Criterio de éxito**

- Crear curso con datos válidos retorna `Result<Guid>` exitoso.
- El curso queda en el repositorio con `IsPublished = false`.
- Título vacío y precio negativo son rechazados por el validator.
- El handler no es alcanzado si el validator falla.

**Tests a escribir** (`tests/NexaLearn.Application.Tests/Courses/CreateCourseCommandHandlerTests.cs`)

```
CreateCourse_ValidCommand_ReturnsSuccessWithCourseId
CreateCourse_ValidCommand_CourseIsPersistedNotPublished
CreateCourse_EmptyTitle_ValidatorReturnsFailure
CreateCourse_NegativePrice_ValidatorReturnsFailure
CreateCourse_InvalidCurrencyLength_ValidatorReturnsFailure
```

---

### T7 — Command: PublishCourse

**Qué implementar**

Dos archivos en `Application/Courses/Commands/PublishCourse/`:

`PublishCourseCommand : IRequest<Result>`:
- `Guid CourseId`

`PublishCourseCommandHandler`:
- Carga el curso por Id — retorna `Result.Failure("Curso no encontrado")` si es null.
- Llama a `course.Publish()` — propaga el `Result` del dominio si falla.
- Persiste con `Update` y `SaveChangesAsync`.

No hay validator porque el único campo es un `Guid` no vacío — la validación
de existencia es responsabilidad del handler, no del validator.

**Por qué existe esta tarea**

Es el primer command que modifica una entidad existente en lugar de crear
una nueva. Introduce el patrón: cargar → modificar → persistir. También
demuestra que los errores del dominio (`Result.Failure` de `course.Publish()`)
se propagan directamente al caller sin transformación.

**Criterio de éxito**

- Publicar un curso con módulos y lecciones retorna `Result` exitoso.
- Publicar un curso inexistente retorna `Result.Failure`.
- Publicar un curso sin módulos retorna `Result.Failure` (error del dominio).
- Publicar un curso ya publicado retorna `Result.Failure` (error del dominio).
- Después del comando exitoso, el curso tiene `IsPublished = true` en el repositorio.

**Tests a escribir** (`tests/NexaLearn.Application.Tests/Courses/PublishCourseCommandHandlerTests.cs`)

```
PublishCourse_ValidCourse_ReturnsSuccess
PublishCourse_ValidCourse_CourseIsPublishedInRepository
PublishCourse_CourseNotFound_ReturnsFailure
PublishCourse_CourseWithNoModules_ReturnsFailure
PublishCourse_AlreadyPublishedCourse_ReturnsFailure
```

---

### T8 — Command: EnrollStudent

**Qué implementar**

Dos archivos en `Application/Enrollments/Commands/EnrollStudent/`:

`EnrollStudentCommand : IRequest<Result<Guid>>`:
- `Guid EnrollmentId`, `Guid StudentId`, `Guid CourseId`

`EnrollStudentCommandHandler`:
- Carga el curso — falla si no existe.
- Carga el estudiante — falla si no existe.
- Verifica que no exista inscripción previa con `GetByStudentAndCourseAsync`
  — falla si ya está inscripto.
- Crea `Enrollment.Create(id, studentId, courseId, course.IsPublished)`.
- Propaga el `Result` del dominio (que falla si el curso no está publicado).
- Persiste y retorna el Id.

**Por qué existe esta tarea**

Es el caso de uso que más reglas de negocio coordina: debe verificar
existencia de dos aggregates distintos y una precondición de negocio
(no inscripción duplicada). Demuestra cómo el application layer orquesta
sin contener lógica de dominio — la regla "no publicado → fallo" la
aplica el aggregate, no el handler.

**Criterio de éxito**

- Inscribir en curso publicado retorna `Result<Guid>` exitoso.
- Curso inexistente retorna `Result.Failure`.
- Estudiante inexistente retorna `Result.Failure`.
- Curso no publicado retorna `Result.Failure`.
- Inscripción duplicada retorna `Result.Failure`.
- La inscripción queda en el repositorio después del comando exitoso.

**Tests a escribir** (`tests/NexaLearn.Application.Tests/Enrollments/EnrollStudentCommandHandlerTests.cs`)

```
EnrollStudent_ValidCommand_ReturnsSuccessWithEnrollmentId
EnrollStudent_ValidCommand_EnrollmentIsPersistedInRepository
EnrollStudent_CourseNotFound_ReturnsFailure
EnrollStudent_StudentNotFound_ReturnsFailure
EnrollStudent_CourseNotPublished_ReturnsFailure
EnrollStudent_DuplicateEnrollment_ReturnsFailure
```

---

### T9 — Command: CompleteLesson

**Qué implementar**

Dos archivos en `Application/Enrollments/Commands/CompleteLesson/`:

`CompleteLessonCommand : IRequest<Result>`:
- `Guid EnrollmentId`, `Guid LessonId`

`CompleteLessonCommandHandler`:
- Carga la inscripción — falla si no existe.
- Verifica que la lección pertenezca al curso: carga el curso de la
  inscripción y busca la lección en sus módulos.
- Llama `enrollment.CompleteLesson(lessonId, lessonBelongsToCourse)`.
- Persiste con `Update` y `SaveChangesAsync`.

**Por qué existe esta tarea**

Demuestra la decisión de diseño DDD del plan: `Enrollment.CompleteLesson`
recibe un `bool`, no un objeto `Course`. El handler es quien tiene acceso
a ambos aggregates y puede resolver la pregunta "¿esta lección pertenece
a este curso?". El aggregate solo aplica la regla — no necesita conocer
el otro aggregate para eso.

**Criterio de éxito**

- Completar una lección válida retorna `Result` exitoso.
- Inscripción inexistente retorna `Result.Failure`.
- Lección que no pertenece al curso retorna `Result.Failure`.
- Lección ya completada retorna `Result.Failure` (error del dominio).
- La lección queda en `CompletedLessonIds` después del comando exitoso.

**Tests a escribir** (`tests/NexaLearn.Application.Tests/Enrollments/CompleteLessonCommandHandlerTests.cs`)

```
CompleteLesson_ValidCommand_ReturnsSuccess
CompleteLesson_ValidCommand_LessonIsMarkedAsCompleted
CompleteLesson_EnrollmentNotFound_ReturnsFailure
CompleteLesson_LessonNotInCourse_ReturnsFailure
CompleteLesson_LessonAlreadyCompleted_ReturnsFailure
```

---

### T10 — Query: GetCourseById

**Qué implementar**

Dos archivos en `Application/Courses/Queries/GetCourseById/`:

`GetCourseByIdQuery : IRequest<Result<CourseDetailDto>>`:
- `Guid CourseId`

`GetCourseByIdQueryHandler`:
- Carga el curso — retorna `Result.Failure("Curso no encontrado")` si es null.
- Retorna `Result<CourseDetailDto>.Success(CourseDetailDto.FromDomain(course))`.

**Por qué existe esta tarea**

Es la query más simple. Introduce el patrón de queries: sin modificación
de estado, retorna un DTO (nunca la entidad de dominio), y el "not found"
se expresa como `Result.Failure`, no como null ni excepción.

**Criterio de éxito**

- Consultar un curso existente retorna `Result<CourseDetailDto>` exitoso con todos los datos.
- Consultar un Id inexistente retorna `Result.Failure`.
- El DTO incluye módulos y lecciones cuando el curso los tiene.

**Tests a escribir** (`tests/NexaLearn.Application.Tests/Courses/GetCourseByIdQueryHandlerTests.cs`)

```
GetCourseById_ExistingCourse_ReturnsDetailDto
GetCourseById_NonExistentCourse_ReturnsFailure
GetCourseById_CourseWithModules_DtoIncludesModulesAndLessons
```

---

### T11 — Query: ListPublishedCourses

**Qué implementar**

Dos archivos en `Application/Courses/Queries/ListPublishedCourses/`:

`ListPublishedCoursesQuery : IRequest<Result<IReadOnlyList<CourseDto>>>`:
- `int Page` (mínimo 1), `int PageSize` (entre 1 y 100)

`ListPublishedCoursesQueryHandler`:
- Carga todos los cursos publicados vía `GetPublishedAsync`.
- Aplica paginación en memoria: `courses.Skip((page-1) * pageSize).Take(pageSize)`.
- Mapea a `CourseDto` y retorna la lista.

`ListPublishedCoursesQueryValidator`:
- `Page`: mayor o igual a 1.
- `PageSize`: entre 1 y 100.

**Por qué existe esta tarea**

Es la única query con paginación. El spec establece que todas las queries
de listado deben ser paginadas desde el inicio — es un hábito de producción
que vale la pena demostrar incluso con paginación en memoria. En Etapa 3,
cuando llegue EF Core, la paginación se moverá a la base de datos con
`Skip`/`Take` en la query SQL.

**Criterio de éxito**

- Retorna solo cursos publicados.
- Paginación funciona: página 1 con `PageSize=2` retorna los primeros dos.
- Página fuera de rango retorna lista vacía (no error).
- `Page=0` o `PageSize=0` son rechazados por el validator.
- `PageSize=101` es rechazado por el validator.

**Tests a escribir** (`tests/NexaLearn.Application.Tests/Courses/ListPublishedCoursesQueryHandlerTests.cs`)

```
ListPublishedCourses_ReturnsOnlyPublishedCourses
ListPublishedCourses_ExcludesUnpublishedCourses
ListPublishedCourses_Pagination_ReturnsCorrectPage
ListPublishedCourses_EmptyRepository_ReturnsEmptyList
ListPublishedCourses_PageBeyondEnd_ReturnsEmptyList
ListPublishedCourses_InvalidPage_ValidatorReturnsFailure
ListPublishedCourses_InvalidPageSize_ValidatorReturnsFailure
```

---

### T12 — Query: GetStudentProgress

**Qué implementar**

Dos archivos en `Application/Enrollments/Queries/GetStudentProgress/`:

`GetStudentProgressQuery : IRequest<Result<StudentProgressDto>>`:
- `Guid StudentId`, `Guid CourseId`

`GetStudentProgressQueryHandler`:
- Carga la inscripción con `GetByStudentAndCourseAsync`.
- Retorna fallo si no existe (el estudiante no está inscripto).
- Carga el curso para calcular el total de lecciones.
- Retorna `StudentProgressDto.FromDomain(enrollment, totalLessons)`.

**Por qué existe esta tarea**

Es la query que cruza dos aggregates para construir la respuesta. Muestra
el patrón correcto: el handler hace las dos lecturas y el DTO los compone,
en lugar de poner esa lógica de composición en el dominio.

**Criterio de éxito**

- Retorna el progreso correcto para un estudiante inscripto.
- Retorna `Result.Failure` si el estudiante no está inscripto en ese curso.
- `CompletedLessons` refleja las lecciones completadas reales.
- `TotalLessons` refleja el total de lecciones del curso.

**Tests a escribir** (`tests/NexaLearn.Application.Tests/Enrollments/GetStudentProgressQueryHandlerTests.cs`)

```
GetStudentProgress_EnrolledStudent_ReturnsProgressDto
GetStudentProgress_NotEnrolled_ReturnsFailure
GetStudentProgress_WithCompletedLessons_ReturnsCorrectCount
GetStudentProgress_NoLessonsCompleted_ReturnsZeroCompleted
```

---

### T13 — Pipeline Behavior: ValidationBehavior

**Qué implementar**

`ValidationBehavior<TRequest, TResponse>` en `Application/Common/Behaviors/ValidationBehavior.cs`:

- Implementa `IPipelineBehavior<TRequest, TResponse>`.
- Recibe `IEnumerable<IValidator<TRequest>>` por inyección de dependencias.
- Si no hay validators registrados, pasa directamente al siguiente handler.
- Ejecuta todos los validators en paralelo con `ValidateAsync`.
- Recolecta todos los errores de todos los validators.
- Si hay errores, retorna `Result.Failure` con el primer mensaje de error
  sin llamar al handler.
- `TResponse` debe ser un `Result` o `Result<T>` — el behavior usa reflexión
  para construir el failure response del tipo correcto.

**Por qué existe esta tarea**

Sin este behavior, la validación tendría que vivir en cada handler.
Con el behavior, todos los commands son validados automáticamente antes
de llegar al handler — sin que el handler sepa que existe validación.
Es el Decorator Pattern aplicado al pipeline de MediatR.

**Criterio de éxito**

- Un command con validator fallido nunca llega al handler.
- Un command sin validator registrado llega al handler normalmente.
- Un command con validator exitoso llega al handler normalmente.
- Múltiples errores de validación son reportados (no solo el primero).

**Tests a escribir** (`tests/NexaLearn.Application.Tests/Behaviors/ValidationBehaviorTests.cs`)

```
ValidationBehavior_WithFailingValidator_ReturnsFailure
ValidationBehavior_WithFailingValidator_HandlerNeverExecuted
ValidationBehavior_WithPassingValidator_HandlerIsExecuted
ValidationBehavior_WithNoValidators_HandlerIsExecuted
ValidationBehavior_WithMultipleErrors_ReturnsFailureWithFirstError
```

---

### T14 — Pipeline Behavior: LoggingBehavior

**Qué implementar**

`LoggingBehavior<TRequest, TResponse>` en `Application/Common/Behaviors/LoggingBehavior.cs`:

- Implementa `IPipelineBehavior<TRequest, TResponse>`.
- Recibe `ILogger<LoggingBehavior<TRequest, TResponse>>`.
- Antes de llamar al handler: loguea `Information` con el nombre del request.
- Llama al siguiente handler y mide el tiempo con `Stopwatch`.
- Después: loguea `Information` con el tiempo de ejecución.
- Si el response es un `Result` fallido: loguea `Warning` con el error.

**Por qué existe esta tarea**

Logging de todos los commands y queries sin tocar ningún handler.
Demuestra el poder del pipeline de MediatR para cross-cutting concerns.
Un handler no sabe que está siendo logueado — el comportamiento se
agrega desde afuera, sin herencia ni modificación del código existente.

**Criterio de éxito**

- El handler es ejecutado correctamente.
- Los mensajes de log se emiten en el orden correcto (antes y después).
- Un resultado fallido genera un log de `Warning`.
- Un resultado exitoso no genera `Warning`.

**Tests a escribir** (`tests/NexaLearn.Application.Tests/Behaviors/LoggingBehaviorTests.cs`)

```
LoggingBehavior_LogsRequestNameBeforeExecution
LoggingBehavior_LogsExecutionTimeAfterCompletion
LoggingBehavior_FailedResult_LogsWarning
LoggingBehavior_SuccessfulResult_DoesNotLogWarning
LoggingBehavior_HandlerIsAlwaysExecuted
```

---

## Resumen de tareas y orden

| # | Tarea | Depende de | Tests |
|---|---|---|---|
| T1 | Configuración del proyecto | — | Solo compilación |
| T2 | IUnitOfWork | T1 | Solo compilación |
| T3 | Repositorios en memoria | T1, T2 | `InMemoryRepositoryTests` |
| T4 | DTOs con mapeo | T1 | `CourseDtoTests` |
| T5 | RegisterStudent command | T2, T3, T4 | `RegisterStudentCommandHandlerTests` |
| T6 | CreateCourse command | T2, T3, T4 | `CreateCourseCommandHandlerTests` |
| T7 | PublishCourse command | T6 | `PublishCourseCommandHandlerTests` |
| T8 | EnrollStudent command | T6, T5 | `EnrollStudentCommandHandlerTests` |
| T9 | CompleteLesson command | T8 | `CompleteLessonCommandHandlerTests` |
| T10 | GetCourseById query | T4, T6 | `GetCourseByIdQueryHandlerTests` |
| T11 | ListPublishedCourses query | T4, T6 | `ListPublishedCoursesQueryHandlerTests` |
| T12 | GetStudentProgress query | T4, T8 | `GetStudentProgressQueryHandlerTests` |
| T13 | ValidationBehavior | T5 | `ValidationBehaviorTests` |
| T14 | LoggingBehavior | T5 | `LoggingBehaviorTests` |

**Total estimado de tests de la Etapa 2**: ~65 tests de integración de handlers.

---

## Criterio de completitud de la Etapa 2

La Etapa 2 está completa cuando:

1. `dotnet test NexaLearn.slnx` pasa en verde — todos los tests de
   Domain (118) y Application (~65) sin errores.
2. Todos los handlers retornan `Result` o `Result<T>` — ninguno lanza
   excepciones para flujo de negocio.
3. Ningún handler conoce tipos concretos de Infrastructure.
4. `ValidationBehavior` intercepta commands inválidos antes del handler.
5. `LoggingBehavior` loguea todos los commands y queries.
6. Ningún DTO expone tipos del dominio — solo primitivos de C#.

Cuando estos seis criterios se cumplen, se puede iniciar la Etapa 3
(Infrastructure: EF Core, PostgreSQL, Testcontainers).
