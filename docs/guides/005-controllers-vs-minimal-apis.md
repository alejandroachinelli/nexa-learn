# Guía 005 — Controllers vs Minimal APIs

**Fecha**: 2026-05-15
**Autor**: Alejandro Martin Achinelli

---

## El punto de partida

Cuando empecé con .NET, la única forma de construir una API era con Controllers. En .NET 6
Microsoft introdujo las Minimal APIs como alternativa. No son un reemplazo obligatorio, son
una opción diferente con tradeoffs distintos. Esta guía explica cuándo elegir cada una y
por qué en nexa-learn elegí Minimal APIs.

---

## Controllers tradicionales: cómo funcionan

Un Controller es una clase que hereda de `ControllerBase` (o `Controller` si necesitás vistas).
El framework usa convenciones para mapear rutas: si la clase se llama `CoursesController` y el
método se llama `GetById`, el atributo `[Route]` define el path.

```csharp
[ApiController]
[Route("api/courses")]
public class CoursesController : ControllerBase
{
    private readonly IMediator _mediator;

    public CoursesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetCourseByIdQuery(id), ct);
        return result.IsFailure ? NotFound(result.Error) : Ok(result.Value);
    }
}
```

El framework infiere mucho: que los parámetros vienen del body o de la ruta según el tipo,
que la respuesta se serializa a JSON, que los errores de model binding devuelven 400. Todo eso
es convención implícita que aprendés una vez y usás siempre.

---

## Minimal APIs: qué son y por qué existen

Las Minimal APIs son lambdas o métodos registrados directamente en el pipeline sin clase
contenedora. Microsoft las introdujo porque los Controllers tienen demasiado ceremony para
casos simples: crear una clase, heredar, inyectar en el constructor, decorar con atributos.
Para una API pequeña ese overhead es desproporcionado.

```csharp
app.MapGet("/api/courses/{id:guid}", async (Guid id, IMediator mediator, CancellationToken ct) =>
{
    var result = await mediator.Send(new GetCourseByIdQuery(id), ct);
    return result.IsFailure ? Results.NotFound(result.Error) : Results.Ok(result.Value);
});
```

El mismo endpoint, menos líneas, sin clase, sin herencia, sin atributos. Los parámetros se
resuelven por tipo: `Guid id` viene de la ruta, `IMediator mediator` viene del DI container.

---

## Diferencias concretas

### Ceremony y estructura

Los Controllers requieren una clase por grupo de endpoints, herencia de `ControllerBase` y
inyección en el constructor. Las Minimal APIs son funciones: no hay clase obligatoria, no hay
herencia, la inyección es por parámetro en cada handler.

### Convenciones vs explicitación

En Controllers, muchos comportamientos son implícitos: `[ApiController]` activa model binding
automático, validación de ModelState, inferencia de fuente de parámetros. En Minimal APIs todo
es explícito. Si el parámetro viene del body, del header o de la ruta, lo sabés mirando la firma.

### Performance

Las Minimal APIs tienen menos overhead en el pipeline de routing. En benchmarks de TechEmpower,
la diferencia es medible pero raramente relevante en aplicaciones de negocio reales. Si tu
cuello de botella es el router de ASP.NET Core, tenés otros problemas más urgentes.

### Herramientas y madurez

Los Controllers tienen más años de ecosistema: documentación, filtros, action results, convenciones
de testing. Las Minimal APIs cerraron mucha brecha en .NET 7 y .NET 8, pero algunos patrones
avanzados (filtros globales, model binders customizados) siguen siendo más naturales en Controllers.

---

## Cuándo elegir cada uno

**Elegí Controllers cuando:**
- El equipo viene de .NET Framework o versiones anteriores y la curva de aprendizaje importa
- Usás mucho el pipeline de filtros de MVC (action filters, result filters)
- Tenés una API grande con muchas convenciones compartidas que se benefician del modelo de herencia

**Elegí Minimal APIs cuando:**
- Querés máxima legibilidad: la firma del método documenta exactamente qué entra y qué sale
- Preferís explicitación sobre convención
- El proyecto es nuevo y no cargás deuda de Controllers existentes
- Querés el menor overhead posible en el pipeline

---

## Por qué elegí Minimal APIs en nexa-learn

El argumento principal no fue performance. Fue legibilidad y explicitación. En un proyecto de
portfolio, la claridad importa más que la velocidad de escritura. Cuando un evaluador lee el
endpoint de `CompleteLesson`:

```csharp
private static async Task<IResult> CompleteLesson(
    Guid id,
    CompleteLessonRequest request,
    IMediator mediator,
    CancellationToken ct) { ... }
```

No necesita saber cómo funciona el model binding de MVC. La firma dice todo: `id` viene de la
ruta (está en el path `/{id:guid}`), `request` viene del body (es un tipo complejo que no es
un servicio), `mediator` viene del DI container.

---

## Cómo organizamos los endpoints para que no sea un caos

El riesgo de las Minimal APIs es terminar con cientos de lambdas en `Program.cs`. La solución
es organizar los endpoints en clases estáticas con extension methods sobre `IEndpointRouteBuilder`:

```csharp
// Cada grupo de endpoints tiene su propio archivo
internal static class CourseEndpoints
{
    internal static void MapCourseEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/courses").WithTags("Courses");

        group.MapGet("/", ListPublished);
        group.MapGet("/{id:guid}", GetById);
        group.MapPost("/", Create).RequireAuthorization();
        group.MapPost("/{id:guid}/publish", Publish).RequireAuthorization();
    }

    private static async Task<IResult> GetById(Guid id, IMediator mediator, CancellationToken ct)
    {
        var result = await mediator.Send(new GetCourseByIdQuery(id), ct);
        return result.IsFailure ? Results.NotFound(result.Error) : Results.Ok(result.Value);
    }
    // ...
}

// Program.cs queda limpio
app.MapCourseEndpoints();
app.MapStudentEndpoints();
app.MapEnrollmentEndpoints();
```

`MapGroup` agrupa las rutas bajo un prefijo común y permite aplicar auth, tags de OpenAPI y
otras políticas a todo el grupo sin repetirlas en cada endpoint.

---

## El mismo endpoint en ambos enfoques

**Controller:**
```csharp
[ApiController]
[Route("api/courses")]
public class CoursesController : ControllerBase
{
    public CoursesController(IMediator mediator) => _mediator = mediator;
    private readonly IMediator _mediator;

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Create(CreateCourseCommand command, CancellationToken ct)
    {
        var result = await _mediator.Send(command, ct);
        return result.IsFailure
            ? BadRequest(result.Error)
            : Created($"/api/courses/{result.Value}", result.Value);
    }
}
```

**Minimal API:**
```csharp
group.MapPost("/", async (CreateCourseCommand command, IMediator mediator, CancellationToken ct) =>
{
    var result = await mediator.Send(command, ct);
    return result.IsFailure
        ? Results.Problem(result.Error, statusCode: 400)
        : Results.Created($"/api/courses/{result.Value}", result.Value);
}).RequireAuthorization();
```

El Controller tiene más líneas pero la estructura es familiar para cualquier developer .NET.
La Minimal API es más densa pero no requiere saber cómo funciona `ControllerBase`.

---

## Pregunta de entrevista clásica

**"¿Cuál es la diferencia entre Controllers y Minimal APIs y cuándo usarías cada uno?"**

La respuesta que espera un senior: no hay una opción objetivamente mejor, son tradeoffs distintos.
Controllers tienen más ceremony pero también más convenciones compartidas y un ecosistema más
maduro para patrones avanzados como filtros. Minimal APIs son más explícitas, tienen menos overhead
y son más legibles cuando los endpoints son simples. En proyectos nuevos con .NET 6+ prefiero
Minimal APIs organizadas con extension methods. En proyectos con Controllers existentes, no
migraría por el solo hecho de migrar — el costo supera el beneficio.
