# CQRS y MediatR — separar la intención de leer de la de escribir

Este documento explica cómo implementamos CQRS con MediatR en nexa-learn
y por qué es una de las decisiones que más impacta en la mantenibilidad.

---

## El problema que resuelve CQRS

En un sistema CRUD clásico, tenés un servicio que hace todo:

```csharp
// Lo que NO hacemos
public class CourseService
{
    public Task<Course> GetById(Guid id) { ... }
    public Task<List<Course>> GetPublished() { ... }
    public Task<Guid> Create(string title, decimal price) { ... }
    public Task Publish(Guid id) { ... }
    public Task AddModule(Guid courseId, string title) { ... }
}
```

Cuando ese servicio crece, terminás con una clase de 500 líneas que mezcla
la lógica de leer con la de escribir. Eso tiene un problema concreto: las
lecturas suelen necesitar optimizaciones que no aplican a las escrituras
(joins específicos, projections, caché) y las escrituras tienen reglas de
negocio que no aplican a las lecturas.

**CQRS** (Command Query Responsibility Segregation) dice: separalos desde el
principio. Un Command cambia estado y no retorna datos de dominio (puede retornar
el ID del recurso creado). Una Query lee estado y no lo modifica.

No es una regla técnica, es una regla semántica. La ventaja es que cada
operación tiene exactamente una responsabilidad y podés razonar sobre ella
en aislamiento.

---

## Qué es MediatR y cómo actúa de mediador

MediatR es una librería que implementa el patrón Mediator: en lugar de que
el caller llame directamente al handler, le pasa un mensaje al mediador y él
se encarga de encontrar quién lo procesa.

```
Controller → IMediator.Send(command) → MediatR → RegisterStudentCommandHandler
```

El Controller no sabe que existe `RegisterStudentCommandHandler`. Solo sabe que
existe `RegisterStudentCommand`. Esta desacoplamiento tiene consecuencias reales:
podés cambiar toda la implementación del handler sin tocar el controller.

En nexa-learn, el flujo para registrar un estudiante es:

1. El controller (Etapa 3) crea un `RegisterStudentCommand` y llama a `_mediator.Send(command)`
2. MediatR busca el handler registrado para ese command
3. Antes de llegar al handler, pasa por el pipeline de behaviors
4. El handler recibe el command y ejecuta la lógica

---

## Command vs Query — con ejemplos reales

### Command: cambia estado

```csharp
// src/NexaLearn.Application/Students/Commands/RegisterStudentCommand.cs
public record RegisterStudentCommand(string Name, string Email) : IRequest<Result<Guid>>;
```

El handler aplica reglas de negocio, persiste y retorna solo el ID:

```csharp
public async Task<Result<Guid>> Handle(RegisterStudentCommand request, CancellationToken cancellationToken)
{
    var emailResult = Email.Create(request.Email);
    if (emailResult.IsFailure)
        return Result<Guid>.Failure(emailResult.Error);

    var existing = await _students.GetByEmailAsync(emailResult.Value, cancellationToken);
    if (existing is not null)
        return Result<Guid>.Failure("Ya existe un estudiante registrado con ese email.");

    var studentResult = Student.Create(Guid.NewGuid(), emailResult.Value, request.Name);
    if (studentResult.IsFailure)
        return Result<Guid>.Failure(studentResult.Error);

    await _students.AddAsync(studentResult.Value, cancellationToken);
    await _uow.SaveChangesAsync(cancellationToken);

    return Result<Guid>.Success(studentResult.Value.Id);
}
```

Notá que retorna `Result<Guid>` (el ID del estudiante creado), no el objeto
`Student` completo. Eso es intencional: quien llamó al command tiene el ID si
necesita hacer una query posterior.

### Query: lee sin modificar

```csharp
// src/NexaLearn.Application/Courses/Queries/GetCourseByIdQuery.cs
public record GetCourseByIdQuery(Guid CourseId) : IRequest<Result<CourseDto>>;
```

El handler solo lee y proyecta:

```csharp
public async Task<Result<CourseDto>> Handle(GetCourseByIdQuery request, CancellationToken cancellationToken)
{
    var course = await _courses.GetByIdAsync(request.CourseId, cancellationToken);
    if (course is null)
        return Result<CourseDto>.Failure("Curso no encontrado.");

    return Result<CourseDto>.Success(CourseDto.FromDomain(course));
}
```

Sin reglas de negocio, sin `SaveChanges`. En Etapa 3, las queries van a poder
tener implementaciones completamente distintas a los commands: podríamos usar
Dapper con SQL crudo para lecturas sin pasar por EF Core, y el handler no
necesitaría cambiar nada — solo el repositorio.

---

## El pipeline de behaviors

Cuando MediatR despacha un request, lo pasa por una cadena de behaviors antes
de llegar al handler. Es como el middleware de ASP.NET pero para la capa de
aplicación.

```
Send(command)
    → LoggingBehavior (antes)
        → ValidationBehavior (antes)
            → RegisterStudentCommandHandler.Handle()
        → ValidationBehavior (después)
    → LoggingBehavior (después)
```

### ValidationBehavior

Intercepta el request antes del handler y corre todos los validators de FluentValidation
registrados para ese tipo. Si hay errores, retorna `Result.Failure` directamente
sin llegar al handler:

```csharp
// src/NexaLearn.Application/Common/Behaviors/ValidationBehavior.cs
public async Task<TResponse> Handle(
    TRequest request,
    RequestHandlerDelegate<TResponse> next,
    CancellationToken cancellationToken)
{
    if (!_validators.Any())
        return await next(cancellationToken);

    var failures = _validators
        .Select(v => v.Validate(new ValidationContext<TRequest>(request)))
        .SelectMany(r => r.Errors)
        .Where(f => f is not null)
        .ToList();

    if (failures.Count == 0)
        return await next(cancellationToken);

    var errorMessage = string.Join("; ", failures.Select(f => f.ErrorMessage));

    // Reflexión solo aquí: TResponse puede ser Result o Result<T>
    if (typeof(TResponse) == typeof(Result))
        return (TResponse)(object)Result.Failure(errorMessage);

    var failureMethod = typeof(TResponse).GetMethod("Failure", [typeof(string)]);
    return (TResponse)failureMethod!.Invoke(null, [errorMessage])!;
}
```

El truco de la reflexión está aislado acá porque `TResponse` puede ser `Result`
(no genérico) o `Result<T>` (genérico con T desconocido en compile time).
Lo documenté explícitamente en el código porque es el único lugar en todo el
proyecto donde usamos reflexión intencionalmente.

### LoggingBehavior

Wrappea la llamada con un `Stopwatch` y loguea si fue éxito o failure:

```csharp
_logger.LogInformation("Ejecutando {Request}", requestName);
var response = await next(cancellationToken);
// ... log con tiempo y resultado
```

El resultado es que cada command y query tiene logging automático sin que
ningún handler tenga que importar `ILogger`. Los handlers no saben que existe
el logger.

---

## Por qué el handler no sabe que existe HTTP ni EF Core

Mirá las dependencias de `RegisterStudentCommandHandler`:

```csharp
public RegisterStudentCommandHandler(IStudentRepository students, IUnitOfWork uow)
```

Recibe interfaces, no implementaciones. No hay `HttpContext`, no hay `DbContext`,
no hay ninguna referencia a la infraestructura real.

Eso permite que los 80 tests de la Application layer corran sin levantar
ningún servidor ni base de datos. Los tests le pasan repositorios en memoria:

```csharp
var repo = new InMemoryStudentRepository();
var uow = new InMemoryUnitOfWork();
var handler = new RegisterStudentCommandHandler(repo, uow);
```

Cuando en Etapa 3 implementemos los repositorios reales con EF Core, los
handlers no van a cambiar ni una línea. Las implementaciones concretas se
registran en el contenedor de DI y se inyectan automáticamente.

Esto es la promesa central de la Clean Architecture: el Application layer define
lo que necesita (interfaces), el Infrastructure layer provee lo que el Application
necesita (implementaciones). La flecha de dependencia siempre apunta hacia adentro.

---

## La pregunta de entrevista clásica

**"¿CQRS significa que tenés dos bases de datos, una para lecturas y otra
para escrituras?"**

No necesariamente. Eso es una optimización posible que se llama **read model
separado** o **proyecciones**, y es una de las formas más avanzadas de aplicar
CQRS. Pero no es parte de la definición del patrón.

CQRS en su forma básica — que es la que implementamos en nexa-learn — solo
dice que la **interfaz de tu sistema** debe separar las operaciones que cambian
estado de las que lo leen. Podés implementarlo con una sola base de datos,
un solo ORM y un solo esquema.

La confusión viene de que CQRS y Event Sourcing se mencionan juntos
frecuentemente. Event Sourcing sí requiere un modelo de datos separado para
las lecturas (porque el store de eventos no es óptimo para queries). Pero
podés usar CQRS sin Event Sourcing, que es exactamente lo que hacemos acá.

La respuesta correcta en entrevista: "CQRS es una separación de responsabilidades
en la capa de aplicación. La separación física de stores de datos es una
optimización opcional que tiene sentido solo cuando las necesidades de lectura
y escritura son tan distintas que justifican el costo operacional de mantener
dos sistemas sincronizados."
