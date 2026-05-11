# Plan de implementación — Etapa 1: Domain Layer

**Fecha**: 2026-05-11
**Etapa**: 1 de 5 — Domain Layer
**Spec de referencia**: docs/spec.md
**ADR de referencia**: docs/adr/001-decisiones-arquitectura-base.md

---

## Reglas de este plan

- Ciclo obligatorio: **red → green → refactor**. El test se escribe primero
  y debe fallar por la razón correcta antes de escribir el código de producción.
- La capa `NexaLearn.Domain` no puede referenciar ningún NuGet externo.
  Solo primitivos del lenguaje y la BCL de .NET 8.
- Cada tarea es independiente y verificable. Una tarea está terminada cuando
  su test pasa y el código compila sin warnings.
- Orden de implementación: las tareas están numeradas por orden de dependencia.
  No se puede empezar T3 si T2 no está verde.

---

## Tareas

### T1 — Result Pattern

**Qué implementar**

Dos tipos genéricos en `NexaLearn.Domain/Common/`:
- `Result` — para operaciones que no retornan valor (solo éxito o fallo con mensaje).
- `Result<T>` — para operaciones que retornan un valor en el caso exitoso.

Ambos deben tener:
- `bool IsSuccess` / `bool IsFailure`
- `string Error` (vacío cuando es éxito)
- Factory methods estáticos: `Success()`, `Success(T value)`, `Failure(string error)`
- `T Value` en `Result<T>` — lanza `InvalidOperationException` si se accede en fallo.

**Por qué existe esta tarea**

Es la base de todo el manejo de errores de negocio. Sin este tipo, el resto
del dominio no puede expresar fallos sin lanzar excepciones. Cada otro tipo
en el dominio depende de `Result`. Va primero.

**Criterio de éxito**

- `Result.Success()` crea un resultado exitoso.
- `Result.Failure("error")` crea un resultado fallido con el mensaje exacto.
- `Result<T>.Success(value)` expone el valor correctamente.
- Acceder a `.Value` en un `Result<T>` fallido lanza `InvalidOperationException`.
- `Result<T>.Failure("error")` no requiere valor.
- Los tipos son inmutables: no hay setters públicos.

**Tests a escribir** (`tests/NexaLearn.Domain.Tests/Common/ResultTests.cs`)

```
Result_Success_IsSuccessTrue
Result_Success_IsFailureFalse
Result_Failure_IsFailureTrue
Result_Failure_HasCorrectErrorMessage
Result_Failure_IsSuccessFalse
ResultT_Success_ExposesValue
ResultT_Failure_AccessingValueThrowsInvalidOperationException
ResultT_Failure_HasCorrectErrorMessage
ResultT_Success_ErrorIsEmpty
```

---

### T2 — Tipos base del dominio

**Qué implementar**

Cuatro abstracciones en `NexaLearn.Domain/Common/`:

- `IDomainEvent` — interfaz marcadora. Sin métodos. Todos los domain events
  la implementan.
- `Entity<TId>` — clase abstracta. Tiene `Id` de tipo genérico `TId`,
  igualdad basada en `Id` (no en referencia), y colección privada de
  `IDomainEvent` con método `AddDomainEvent(IDomainEvent)` y propiedad
  `IReadOnlyList<IDomainEvent> DomainEvents`. Constructor protegido.
- `AggregateRoot<TId>` — hereda de `Entity<TId>`. En esta etapa no agrega
  comportamiento adicional, pero la distinción semántica entre entidad y raíz
  del aggregate es parte del modelo.
- `ValueObject` — clase abstracta. Igualdad estructural basada en
  `GetEqualityComponents()` (método abstracto que retorna
  `IEnumerable<object>`). Implementa `Equals`, `GetHashCode` y los
  operadores `==` y `!=`.

**Por qué existe esta tarea**

Estos tipos son la gramática del modelo de dominio. Sin ellos no hay forma
de expresar la diferencia entre una entidad (identidad por Id) y un value
object (identidad por valor), ni de adjuntar domain events a un aggregate.
Todo el código de dominio que sigue hereda de estas abstracciones.

**Criterio de éxito**

- Dos entidades con el mismo `Id` son iguales (`Equals` retorna `true`).
- Dos entidades con distinto `Id` no son iguales.
- `AddDomainEvent` agrega el evento a la colección y es recuperable.
- Dos value objects con los mismos componentes son iguales.
- Dos value objects con distinto componente no son iguales.
- Los operadores `==` y `!=` funcionan correctamente en value objects.
- `ValueObject` no puede ser instanciado directamente (abstracta).

**Tests a escribir** (`tests/NexaLearn.Domain.Tests/Common/`)

```
EntityTests.cs
  Entity_SameId_AreEqual
  Entity_DifferentId_AreNotEqual
  Entity_AddDomainEvent_EventIsInCollection
  Entity_DomainEvents_InitiallyEmpty

ValueObjectTests.cs
  ValueObject_SameComponents_AreEqual
  ValueObject_DifferentComponents_AreNotEqual
  ValueObject_EqualityOperator_WorksCorrectly
  ValueObject_InequalityOperator_WorksCorrectly
```

---

### T3 — Value Object: Email

**Qué implementar**

`Email` en `NexaLearn.Domain/ValueObjects/Email.cs`.

- Hereda de `ValueObject`.
- Factory method estático: `Email.Create(string value)` retorna `Result<Email>`.
- Falla si el string es nulo, vacío, o no tiene formato de email válido
  (contiene `@` y al menos un `.` después del `@`).
- Propiedad `Value` expone el email en minúsculas normalizadas.
- `GetEqualityComponents()` retorna `Value`.

**Por qué existe esta tarea**

Demuestra el patrón Value Object con validación en la construcción: no puede
existir un `Email` inválido en el sistema. La validación via `Create` +
`Result` es la forma correcta de aplicar invariantes sin excepciones.

**Criterio de éxito**

- Email válido crea `Result<Email>` exitoso.
- Email nulo retorna `Result.Failure`.
- Email vacío retorna `Result.Failure`.
- Email sin `@` retorna `Result.Failure`.
- Email con `@` pero sin dominio retorna `Result.Failure`.
- Dos `Email` con el mismo valor son iguales.
- El valor se normaliza a minúsculas.

**Tests a escribir** (`tests/NexaLearn.Domain.Tests/ValueObjects/EmailTests.cs`)

```
Email_ValidAddress_CreatesSuccessfully
Email_NullValue_ReturnsFailure
Email_EmptyValue_ReturnsFailure
Email_WithoutAtSign_ReturnsFailure
Email_WithoutDomain_ReturnsFailure
Email_NormalizedToLowercase
Email_SameValue_AreEqual
Email_DifferentValues_AreNotEqual
```

---

### T4 — Value Object: CourseTitle

**Qué implementar**

`CourseTitle` en `NexaLearn.Domain/ValueObjects/CourseTitle.cs`.

- Hereda de `ValueObject`.
- Factory method: `CourseTitle.Create(string value)` retorna `Result<CourseTitle>`.
- Falla si es nulo, vacío, solo espacios, o supera los 200 caracteres.
- `Value` expone el título con espacios extremos recortados (`Trim`).

**Por qué existe esta tarea**

Un string no es un título de curso. Un `CourseTitle` garantiza que el
string cumple las reglas del negocio en cualquier punto del sistema donde
aparezca. Demuestra que los value objects son más que wrappers: son
guardianes de invariantes.

**Criterio de éxito**

- Título válido crea `Result<CourseTitle>` exitoso.
- Nulo, vacío y solo espacios retornan `Result.Failure`.
- Título de 201 caracteres retorna `Result.Failure`.
- Título de exactamente 200 caracteres es válido.
- El valor se recorta con `Trim`.
- Dos `CourseTitle` con el mismo valor son iguales.

**Tests a escribir** (`tests/NexaLearn.Domain.Tests/ValueObjects/CourseTitleTests.cs`)

```
CourseTitle_ValidTitle_CreatesSuccessfully
CourseTitle_NullValue_ReturnsFailure
CourseTitle_EmptyValue_ReturnsFailure
CourseTitle_WhitespaceOnly_ReturnsFailure
CourseTitle_ExactlyMaxLength_CreatesSuccessfully
CourseTitle_ExceedsMaxLength_ReturnsFailure
CourseTitle_TrimsWhitespace
CourseTitle_SameValue_AreEqual
```

---

### T5 — Value Object: Duration

**Qué implementar**

`Duration` en `NexaLearn.Domain/ValueObjects/Duration.cs`.

- Hereda de `ValueObject`.
- Factory method: `Duration.Create(int minutes)` retorna `Result<Duration>`.
- Falla si `minutes` es menor o igual a cero.
- Propiedad `Minutes` expone el valor.
- Propiedad calculada `Hours` retorna `Minutes / 60.0`.

**Por qué existe esta tarea**

Demuestra un value object numérico con invariante simple pero significativa:
una duración negativa o cero no tiene sentido en este dominio. También
muestra que los value objects pueden tener comportamiento calculado.

**Criterio de éxito**

- `Duration.Create(90)` es exitoso, `Minutes` es `90`, `Hours` es `1.5`.
- `Duration.Create(0)` retorna `Result.Failure`.
- `Duration.Create(-1)` retorna `Result.Failure`.
- Dos `Duration` con los mismos minutos son iguales.

**Tests a escribir** (`tests/NexaLearn.Domain.Tests/ValueObjects/DurationTests.cs`)

```
Duration_PositiveMinutes_CreatesSuccessfully
Duration_ZeroMinutes_ReturnsFailure
Duration_NegativeMinutes_ReturnsFailure
Duration_Hours_IsCorrectlyCalculated
Duration_SameMinutes_AreEqual
Duration_DifferentMinutes_AreNotEqual
```

---

### T6 — Value Object: Money

**Qué implementar**

`Money` en `NexaLearn.Domain/ValueObjects/Money.cs`.

- Hereda de `ValueObject`.
- Factory method: `Money.Create(decimal amount, string currency)` retorna
  `Result<Money>`.
- Falla si `amount` es negativo.
- Falla si `currency` es nulo, vacío, o no tiene exactamente 3 caracteres
  (ISO 4217 básico).
- `currency` se normaliza a mayúsculas.
- Propiedades: `Amount` y `Currency`.
- Método de instancia: `Add(Money other)` retorna `Result<Money>`. Falla si
  las currencies son distintas.
- Constante estática: `Money.Free` equivale a `0 USD`.

**Por qué existe esta tarea**

`Money` es el value object más rico de la Etapa 1. Demuestra que los
value objects pueden tener comportamiento de negocio (`Add`), constantes
de dominio (`Free`), y múltiples componentes de igualdad (`Amount` +
`Currency`).

**Criterio de éxito**

- `Money.Create(10, "usd")` crea exitoso con `Currency` = `"USD"`.
- Monto negativo retorna `Result.Failure`.
- Currency de 2 o 4 caracteres retorna `Result.Failure`.
- `Money.Free` tiene `Amount = 0` y `Currency = "USD"`.
- `Add` de monedas iguales suma correctamente.
- `Add` de monedas distintas retorna `Result.Failure`.
- Igualdad considera `Amount` y `Currency`.

**Tests a escribir** (`tests/NexaLearn.Domain.Tests/ValueObjects/MoneyTests.cs`)

```
Money_ValidAmountAndCurrency_CreatesSuccessfully
Money_NegativeAmount_ReturnsFailure
Money_ZeroAmount_CreatesSuccessfully
Money_NullCurrency_ReturnsFailure
Money_InvalidCurrencyLength_ReturnsFailure
Money_CurrencyNormalizedToUppercase
Money_Free_IsZeroUsd
Money_Add_SameCurrency_ReturnsCorrectSum
Money_Add_DifferentCurrency_ReturnsFailure
Money_SameAmountAndCurrency_AreEqual
Money_DifferentAmount_AreNotEqual
Money_DifferentCurrency_AreNotEqual
```

---

### T7 — Aggregate: Course (con Module y Lesson)

**Qué implementar**

Tres clases en `NexaLearn.Domain/Aggregates/Courses/`:

`Lesson` — entidad interna (no es aggregate root):
- `Id` (Guid), `Title` (string), `Duration` (value object `Duration`).
- Constructor privado. Factory method estático:
  `Lesson.Create(Guid id, string title, Duration duration)` retorna
  `Result<Lesson>`. Falla si `title` es nulo o vacío.

`Module` — entidad interna:
- `Id` (Guid), `Title` (string), colección privada de `Lesson`.
- Factory method: `Module.Create(Guid id, string title)` retorna
  `Result<Module>`. Falla si `title` es nulo o vacío.
- Método: `AddLesson(Lesson lesson)` retorna `Result`. Falla si ya existe
  una lección con el mismo `Id`.
- Propiedad: `IReadOnlyList<Lesson> Lessons`.
- Propiedad calculada: `bool HasLessons`.

`Course` — aggregate root, hereda de `AggregateRoot<Guid>`:
- Propiedades: `CourseTitle Title`, `Money Price`, `bool IsPublished`.
- Colección privada de `Module`.
- Factory method: `Course.Create(Guid id, CourseTitle title, Money price)`
  retorna `Result<Course>`. Estado inicial: no publicado.
- Método: `AddModule(Module module)` retorna `Result`. Falla si ya existe
  un módulo con el mismo `Id` o si el curso está publicado.
- Método: `Publish()` retorna `Result`. Falla si no hay módulos, o si
  ningún módulo tiene lecciones. Si tiene éxito, agrega el domain event
  `CoursePublished` (definido en T8 — se puede dejar como stub por ahora
  o implementar en paralelo).
- Propiedad: `IReadOnlyList<Module> Modules`.

**Por qué existe esta tarea**

Es el aggregate más complejo de la Etapa 1. Demuestra: aggregate root con
entidades internas, invariantes que protegen el estado (`Publish` falla si
no hay contenido), la regla de que un curso publicado no acepta modificaciones,
y el dispatch de domain events desde el aggregate.

**Criterio de éxito**

- `Course.Create` crea un curso no publicado.
- `AddModule` agrega módulos al curso.
- `AddModule` falla si el módulo ya existe (mismo Id).
- `AddModule` falla si el curso está publicado.
- `Publish` falla si no hay módulos.
- `Publish` falla si hay módulos pero ninguno tiene lecciones.
- `Publish` tiene éxito con al menos un módulo con al menos una lección.
- Después de `Publish`, `IsPublished` es `true`.
- Después de `Publish`, `DomainEvents` contiene un evento `CoursePublished`.
- `Module.AddLesson` falla si la lección ya existe.

**Tests a escribir** (`tests/NexaLearn.Domain.Tests/Aggregates/CourseTests.cs`)

```
Course_Create_IsNotPublished
Course_Create_HasNoModules
Course_AddModule_AddsSuccessfully
Course_AddModule_DuplicateId_ReturnsFailure
Course_AddModule_WhenPublished_ReturnsFailure
Course_Publish_WithNoModules_ReturnsFailure
Course_Publish_WithModuleButNoLessons_ReturnsFailure
Course_Publish_WithModuleAndLesson_Succeeds
Course_Publish_SetsIsPublishedTrue
Course_Publish_RaisesCoursePublishedEvent
Course_Publish_AlreadyPublished_ReturnsFailure
Module_AddLesson_AddsSuccessfully
Module_AddLesson_DuplicateId_ReturnsFailure
Module_HasLessons_FalseWhenEmpty
Module_HasLessons_TrueAfterAddingLesson
```

---

### T8 — Domain Events

**Qué implementar**

Tres records en sus carpetas correspondientes, todos implementando
`IDomainEvent`:

- `CoursePublished` en `NexaLearn.Domain/Aggregates/Courses/Events/`:
  propiedades `Guid CourseId`, `DateTimeOffset OccurredAt`.

- `StudentEnrolled` en `NexaLearn.Domain/Aggregates/Enrollments/Events/`:
  propiedades `Guid EnrollmentId`, `Guid StudentId`, `Guid CourseId`,
  `DateTimeOffset OccurredAt`.

- `LessonCompleted` en `NexaLearn.Domain/Aggregates/Enrollments/Events/`:
  propiedades `Guid EnrollmentId`, `Guid StudentId`, `Guid LessonId`,
  `DateTimeOffset OccurredAt`.

Usar `record` de C#: inmutabilidad e igualdad estructural gratis.

**Por qué existe esta tarea**

Los domain events son la forma en que el dominio comunica que algo
significativo ocurrió. Definirlos como tipos explícitos (no strings, no
diccionarios) hace que el sistema de tipos del compilador sea el contrato.
En esta etapa solo se definen: el mecanismo de dispatch llega en Etapa 5.

**Criterio de éxito**

- Los tres tipos compilan e implementan `IDomainEvent`.
- Son inmutables: todas las propiedades son `init`-only.
- Dos instancias con los mismos valores son iguales (igualdad de `record`).
- `CoursePublished` es generado por `Course.Publish()` (verificado en T7).

**Tests a escribir** (`tests/NexaLearn.Domain.Tests/Aggregates/DomainEventsTests.cs`)

```
CoursePublished_HasCorrectCourseId
CoursePublished_HasCorrectOccurredAt
StudentEnrolled_HasCorrectIds
LessonCompleted_HasCorrectIds
DomainEvents_SameValues_AreEqual
```

---

### T9 — Entidad: Student

**Qué implementar**

`Student` en `NexaLearn.Domain/Aggregates/Students/Student.cs`.
Hereda de `AggregateRoot<Guid>`.

- Propiedades: `Email Email`, `string Name`.
- Factory method: `Student.Create(Guid id, Email email, string name)`
  retorna `Result<Student>`. Falla si `name` es nulo, vacío o solo espacios.
- `Name` se guarda con `Trim`.

No hay métodos de negocio complejos en `Student` en esta etapa. La lógica
de inscripción pertenece al aggregate `Enrollment`.

**Por qué existe esta tarea**

Demuestra la creación de un aggregate root simple con un value object
embebido (`Email`) y validación de entrada via Result Pattern. Es
deliberadamente simple para contrastar con la complejidad de `Course`.

**Criterio de éxito**

- `Student.Create` con datos válidos crea exitoso.
- Nombre nulo, vacío o solo espacios retorna `Result.Failure`.
- `Name` se guarda con `Trim`.
- `Email` es el value object, no el string crudo.

**Tests a escribir** (`tests/NexaLearn.Domain.Tests/Aggregates/StudentTests.cs`)

```
Student_Create_ValidData_Succeeds
Student_Create_NullName_ReturnsFailure
Student_Create_EmptyName_ReturnsFailure
Student_Create_WhitespaceName_ReturnsFailure
Student_Create_TrimsName
Student_Email_IsValueObject
```

---

### T10 — Aggregate: Enrollment

**Qué implementar**

`Enrollment` en `NexaLearn.Domain/Aggregates/Enrollments/Enrollment.cs`.
Hereda de `AggregateRoot<Guid>`.

- Propiedades: `Guid StudentId`, `Guid CourseId`, `DateTimeOffset EnrolledAt`,
  colección privada de `Guid` (IDs de lecciones completadas).
- Factory method estático:
  `Enrollment.Create(Guid id, Guid studentId, Guid courseId, bool courseIsPublished)`
  retorna `Result<Enrollment>`. Falla si `courseIsPublished` es `false`.
  Si tiene éxito, agrega el domain event `StudentEnrolled`.
- Método: `CompleteLesson(Guid lessonId, bool lessonBelongsToCourse)`
  retorna `Result`. Falla si `lessonBelongsToCourse` es `false`.
  Falla si la lección ya está completada. Si tiene éxito, agrega el
  domain event `LessonCompleted`.
- Propiedad: `IReadOnlyList<Guid> CompletedLessonIds`.
- Propiedad calculada: `bool HasCompletedLesson(Guid lessonId)`.

**Nota de diseño**: el aggregate `Enrollment` recibe booleans en lugar de
objetos completos (`Course`, `Lesson`) para evitar dependencias entre
aggregates. Esta es una decisión deliberada de diseño DDD: los aggregates
no se referencian entre sí directamente.

**Por qué existe esta tarea**

Demuestra las reglas de negocio más críticas de la Etapa 1: un estudiante
no puede inscribirse en un curso no publicado, y no puede completar una
lección que no pertenece al curso. También muestra que los domain events
se disparan desde el aggregate que posee la transición de estado.

**Criterio de éxito**

- `Enrollment.Create` con curso publicado tiene éxito.
- `Enrollment.Create` con curso no publicado retorna `Result.Failure`.
- Después de `Create` exitoso, `DomainEvents` contiene `StudentEnrolled`.
- `CompleteLesson` con lección válida tiene éxito.
- `CompleteLesson` con lección que no pertenece al curso retorna
  `Result.Failure`.
- `CompleteLesson` con lección ya completada retorna `Result.Failure`.
- Después de `CompleteLesson` exitoso, `DomainEvents` contiene
  `LessonCompleted`.
- `HasCompletedLesson` retorna `true` para lecciones completadas.

**Tests a escribir** (`tests/NexaLearn.Domain.Tests/Aggregates/EnrollmentTests.cs`)

```
Enrollment_Create_PublishedCourse_Succeeds
Enrollment_Create_UnpublishedCourse_ReturnsFailure
Enrollment_Create_RaisesStudentEnrolledEvent
Enrollment_CompleteLesson_ValidLesson_Succeeds
Enrollment_CompleteLesson_LessonNotInCourse_ReturnsFailure
Enrollment_CompleteLesson_AlreadyCompleted_ReturnsFailure
Enrollment_CompleteLesson_RaisesLessonCompletedEvent
Enrollment_HasCompletedLesson_ReturnsTrueAfterCompletion
Enrollment_HasCompletedLesson_ReturnsFalseInitially
```

---

### T11 — Interfaces de repositorio

**Qué implementar**

Tres interfaces en `NexaLearn.Domain/Interfaces/`:

`ICourseRepository`:
- `Task<Course?> GetByIdAsync(Guid id, CancellationToken ct)`
- `Task<IReadOnlyList<Course>> GetPublishedAsync(CancellationToken ct)`
- `Task AddAsync(Course course, CancellationToken ct)`
- `void Update(Course course)`

`IStudentRepository`:
- `Task<Student?> GetByIdAsync(Guid id, CancellationToken ct)`
- `Task<Student?> GetByEmailAsync(Email email, CancellationToken ct)`
- `Task AddAsync(Student student, CancellationToken ct)`

`IEnrollmentRepository`:
- `Task<Enrollment?> GetByIdAsync(Guid id, CancellationToken ct)`
- `Task<Enrollment?> GetByStudentAndCourseAsync(Guid studentId, Guid courseId, CancellationToken ct)`
- `Task AddAsync(Enrollment enrollment, CancellationToken ct)`

**Por qué existe esta tarea**

Las interfaces de repositorio viven en el dominio, no en la infraestructura.
Esto es el núcleo de la Dependency Inversion Principle en Clean Architecture:
el dominio define el contrato que necesita, la infraestructura lo satisface.
La Application layer en Etapa 2 dependerá de estas interfaces para que los
handlers sean independientes del ORM.

**Criterio de éxito**

- Las tres interfaces compilan sin errores.
- Solo usan tipos definidos en `NexaLearn.Domain` (no EF Core, no Npgsql).
- Todos los métodos son `async` con `CancellationToken`.
- `GetByIdAsync` retorna `null` cuando no encuentra el registro — no lanza
  excepción.

**Tests a escribir**

No hay tests unitarios para interfaces. La verificación es la compilación
sin errores y la coherencia con los tipos del dominio. Los tests de integración
de las implementaciones llegan en Etapa 3.

---

## Resumen de tareas y orden

| # | Tarea | Depende de | Archivos en Domain.Tests |
|---|---|---|---|
| T1 | Result Pattern | — | `Common/ResultTests.cs` |
| T2 | Tipos base (Entity, AggregateRoot, ValueObject, IDomainEvent) | T1 | `Common/EntityTests.cs`, `Common/ValueObjectTests.cs` |
| T3 | Value Object: Email | T2 | `ValueObjects/EmailTests.cs` |
| T4 | Value Object: CourseTitle | T2 | `ValueObjects/CourseTitleTests.cs` |
| T5 | Value Object: Duration | T2 | `ValueObjects/DurationTests.cs` |
| T6 | Value Object: Money | T2 | `ValueObjects/MoneyTests.cs` |
| T7 | Aggregate: Course (con Module y Lesson) | T2, T4, T5, T8 | `Aggregates/CourseTests.cs` |
| T8 | Domain Events | T2 | `Aggregates/DomainEventsTests.cs` |
| T9 | Entidad: Student | T2, T3 | `Aggregates/StudentTests.cs` |
| T10 | Aggregate: Enrollment | T2, T8 | `Aggregates/EnrollmentTests.cs` |
| T11 | Interfaces de repositorio | T3, T7, T9, T10 | — (solo compilación) |

**Total de tests de la Etapa 1**: ~65 tests unitarios.
**Dependencias externas del proyecto Domain**: ninguna (solo BCL de .NET 8).

---

## Criterio de completitud de la Etapa 1

La Etapa 1 está completa cuando:

1. `dotnet test tests/NexaLearn.Domain.Tests` pasa en verde sin warnings.
2. `dotnet build src/NexaLearn.Domain` compila sin referencias a NuGet externos.
3. Todos los value objects rechazan datos inválidos en construcción.
4. Todos los aggregates protegen sus invariantes y retornan `Result.Failure`
   en lugar de lanzar excepciones.
5. Los domain events son generados por los aggregates que corresponden.
6. Las interfaces de repositorio están definidas y usan solo tipos del dominio.

Cuando estos seis criterios se cumplen, se puede iniciar la Etapa 2
(Application layer + CQRS).
