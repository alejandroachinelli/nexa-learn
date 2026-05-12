# Tipos de tests y cuándo usar cada uno

Este documento explica cómo pienso la estrategia de testing en nexa-learn,
con ejemplos del código real del proyecto.

---

## La pirámide de tests

```
        /\
       /  \
      / E2E \        ← pocos, lentos, costosos
     /--------\
    /Integration\    ← medianos, algo lentos
   /------------\
  /  Unit Tests  \   ← muchos, rápidos, baratos
 /________________\
```

La proporción importa. Si la pirámide está invertida (más E2E que unitarios),
el feedback loop se destruye: cada cambio implica esperar minutos para saber
si rompiste algo, y cuando algo falla es difícil saber exactamente qué fue.

En nexa-learn apuntamos a algo como: **80% unitarios / 15% integración / 5% E2E**.

---

## Tests unitarios — el Domain layer

Un test unitario verifica una sola unidad de lógica en completo aislamiento.
Sin base de datos, sin red, sin sistema de archivos. Solo código en memoria.

El Domain layer de nexa-learn es el ejemplo perfecto de por qué esto funciona.
Mirá el `.csproj` del proyecto de tests de dominio:

```xml
<!-- tests/NexaLearn.Domain.Tests/NexaLearn.Domain.Tests.csproj -->
<ItemGroup>
  <PackageReference Include="FluentAssertions" Version="7.0.0" />
  <PackageReference Include="xunit" Version="2.5.3" />
  <ProjectReference Include="..\..\src\NexaLearn.Domain\NexaLearn.Domain.csproj" />
</ItemGroup>
```

Solo xunit y FluentAssertions. El Domain layer no tiene ninguna dependencia externa:
sin EF Core, sin HTTP, sin nada que necesite levantarse. Por eso los 118 tests
de dominio corren en **menos de 35ms**.

### Ejemplo concreto

```csharp
// tests/NexaLearn.Domain.Tests/Aggregates/EnrollmentTests.cs
[Fact]
public void Enrollment_Create_UnpublishedCourse_ReturnsFailure()
{
    var result = Enrollment.Create(
        Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
        courseIsPublished: false);

    result.IsFailure.Should().BeTrue();
}
```

Este test no necesita una base de datos con cursos reales. No necesita un repositorio.
No necesita un handler de MediatR. Solo verifica que el aggregate aplica la regla de
negocio: no se puede inscribir a un curso que no está publicado.

Eso es posible porque el `Enrollment.Create()` recibe un `bool courseIsPublished`
en lugar de un objeto `Course`. El aggregate no depende de otro aggregate —
recibe solo el dato que necesita para tomar la decisión.

Otro ejemplo, verificando que se dispara el domain event correcto:

```csharp
[Fact]
public void Enrollment_Create_RaisesStudentEnrolledEvent()
{
    var result = Enrollment.Create(
        Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
        courseIsPublished: true);

    result.Value.DomainEvents.Should().ContainSingle()
        .Which.Should().BeOfType<StudentEnrolled>();
}
```

Todo en memoria, todo determinístico, sin efectos secundarios.

### Por qué el Domain layer no tiene dependencias

La arquitectura limpia pone el dominio en el centro exactamente por esto.
Si `Enrollment` dependiera de EF Core para consultar si el curso existe,
el test necesitaría una base de datos o un mock de DbContext.
Al recibir solo el `bool`, el dominio no sabe ni le importa de dónde viene ese dato.

Consecuencia directa: 118 tests que corren en 35ms. En un CI con 500 tests,
la diferencia entre 35ms y 5 segundos por test es la diferencia entre feedback
instantáneo y esperar 40 minutos para saber si el PR está roto.

---

## Tests de integración — el Application layer

Un test de integración verifica que múltiples piezas colaboran correctamente.
No una sola unidad, sino un flujo completo — pero todavía sin infraestructura real.

En la Etapa 2, los tests de Application layer son tests de integración livianos:
verifican que un handler de MediatR coordina bien con los repositorios, pero
usan repositorios en memoria en lugar de PostgreSQL.

```csharp
// tests/NexaLearn.Application.Tests/Common/InMemory/InMemoryCourseRepository.cs
public class InMemoryCourseRepository : ICourseRepository
{
    private readonly List<Course> _courses = [];

    public Task<Course?> GetByIdAsync(Guid id, CancellationToken ct) =>
        Task.FromResult(_courses.FirstOrDefault(c => c.Id == id));

    public Task AddAsync(Course course, CancellationToken ct)
    {
        _courses.Add(course);
        return Task.CompletedTask;
    }
    // ...
}
```

¿Por qué repositorio en memoria y no un mock con NSubstitute?

Con un mock podría escribir:

```csharp
// Esto parece equivalente pero no lo es
var repo = Substitute.For<ICourseRepository>();
repo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
    .Returns(Task.FromResult<Course?>(course));
```

El problema es que el mock solo verifica que se llamó a los métodos correctos con
los parámetros correctos. El repositorio en memoria verifica que los datos fluyen:
si el handler llama a `AddAsync` y después otro handler llama a `GetByIdAsync`,
el repositorio en memoria devuelve el curso que se agregó. Un mock no hace eso.

Es un matiz sutil, pero hay una categoría entera de bugs que solo aparecen cuando
el estado persiste entre operaciones — exactamente lo que un repositorio hace en
producción.

---

## Tests de integración reales — Etapa 3 con Testcontainers

En la Etapa 3, cuando implementemos el Infrastructure layer con EF Core y PostgreSQL,
vamos a agregar una capa más: tests que corren contra una base de datos real.

Testcontainers levanta un contenedor Docker de PostgreSQL para cada suite de tests:

```csharp
// Así se vería en Etapa 3
public class CourseRepositoryTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16")
        .Build();

    public async Task InitializeAsync() => await _postgres.StartAsync();
    public async Task DisposeAsync() => await _postgres.DisposeAsync();

    [Fact]
    public async Task AddAsync_ThenGetById_ReturnsSameCourse()
    {
        // Aquí sí hay una base de datos real, con transacciones reales
        // y EF Core de por medio
    }
}
```

Estos tests van a tardar varios segundos cada uno porque levantan Docker, aplican
migraciones y ejecutan SQL real. Eso está bien — son los que detectan problemas
de mapeo con EF Core, índices faltantes, o comportamientos de PostgreSQL que no
replican en memoria.

La diferencia con la Etapa 2 es intencional:
- **Etapa 2**: repositorios en memoria → feedback rápido mientras diseñamos la lógica
- **Etapa 3**: Testcontainers → confianza en que la infraestructura funciona

Los tests en memoria de Etapa 2 no desaparecen cuando llegamos a Etapa 3.
Siguen corriendo porque siguen siendo útiles: son rápidos y verifican la lógica
de aplicación independientemente de la base de datos.

---

## Tests end-to-end

Un test E2E dispara una petición HTTP real contra la API levantada, con todos
los componentes activos: base de datos, autenticación, middleware, todo.

En nexa-learn los vamos a escribir en Etapa 4, cuando tengamos el API layer completo.
Algo así:

```csharp
// Concepto — se implementa en Etapa 4
[Fact]
public async Task POST_courses_enroll_Returns201_WhenCourseIsPublished()
{
    // Levanta la app completa con WebApplicationFactory
    // Autentica con un token JWT real
    // Hace POST /api/enrollments
    // Verifica que el status es 201 y que el enrollment existe en la DB
}
```

Estos tests son los más valiosos para detectar problemas de integración entre
capas, pero también los más costosos de mantener. Un cambio en la estructura de
un endpoint puede romper decenas de tests E2E. Por eso los mantenemos al mínimo:
solo los flujos principales (happy path) y los casos de error más críticos.

---

## Cuándo usar cada tipo

| Situación | Tipo de test |
|-----------|--------------|
| Validar una regla de negocio del dominio | Unitario |
| Verificar que un Value Object rechaza datos inválidos | Unitario |
| Confirmar que un domain event se dispara correctamente | Unitario |
| Verificar que un handler coordina bien repositorios | Integración (en memoria) |
| Confirmar que el mapeo de EF Core funciona con PostgreSQL | Integración (Testcontainers) |
| Verificar que un endpoint responde con el status correcto | E2E |
| Confirmar que el flujo completo de inscripción funciona | E2E |

La regla práctica: **si podés escribirlo como unitario sin hacer trampa, hacelo
unitario**. Hacer trampa sería, por ejemplo, meter lógica de negocio en el
handler para poder testearla ahí en lugar de en el dominio.

Si la unidad necesita colaboradores para tener sentido (un handler que coordina
repositorios), usá integración. Si necesitás verificar que todo el sistema
funciona junto, E2E.

---

## El número que me importa en el día a día

Cuando trabajo en una feature nueva, lo primero que hago es correr:

```bash
dotnet test
```

Si tarda más de 5 segundos, algo está mal en la arquitectura de los tests.
El feedback loop roto es la señal de que hay demasiadas dependencias en el
lugar equivocado.

Hoy mismo: 155 tests, todos pasando, en menos de 100ms totales.
Así tiene que seguir cuando lleguemos a 500 tests.
