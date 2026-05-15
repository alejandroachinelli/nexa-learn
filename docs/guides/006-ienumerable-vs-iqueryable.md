# Guía 006 — IEnumerable vs IQueryable

**Fecha**: 2026-05-15
**Autor**: Alejandro Martin Achinelli

---

## La diferencia fundamental

`IEnumerable<T>` ejecuta el filtro **en memoria**, en el proceso de .NET.
`IQueryable<T>` ejecuta el filtro **en la base de datos**, traduciendo la expresión LINQ a SQL.

Esa distinción determina si tu query trae 10 filas o 100.000 filas por la red.

---

## Ejemplo concreto: la query mal escrita

```csharp
// MAL: trae todos los cursos a memoria y filtra después
public async Task<IReadOnlyList<Course>> GetPublishedAsync(CancellationToken ct)
{
    IEnumerable<Course> courses = await _context.Courses.ToListAsync(ct);
    return courses.Where(c => c.IsPublished).ToList();
}
```

Lo que pasa aquí en dos pasos:

1. `ToListAsync()` ejecuta `SELECT * FROM courses` — sin filtro. Todos los registros viajan
   por la red hasta el proceso de .NET.
2. `.Where(c => c.IsPublished)` filtra en memoria con LINQ-to-Objects.

Si la tabla tiene 50.000 cursos y 200 están publicados, trajiste 49.800 filas innecesarias.

---

## La query bien escrita

```csharp
// BIEN: el filtro viaja a PostgreSQL como cláusula WHERE
public async Task<IReadOnlyList<Course>> GetPublishedAsync(CancellationToken ct)
{
    return await _context.Courses
        .Where(c => c.IsPublished)
        .AsNoTracking()
        .ToListAsync(ct);
}
```

Aquí `_context.Courses` devuelve `IQueryable<Course>`. El `.Where(c => c.IsPublished)` no
ejecuta nada todavía — construye un árbol de expresiones. Recién cuando llega `.ToListAsync(ct)`
EF Core traduce ese árbol a SQL y lo envía a PostgreSQL:

```sql
SELECT c.id, c.title, c.price, c.currency, c.is_published
FROM courses AS c
WHERE c.is_published = true
```

Solo vienen los 200 cursos publicados.

---

## Qué pasa en producción si te confundís

El error no es obvio porque el resultado es correcto: ambas versiones devuelven los mismos datos.
La diferencia aparece en el profiler:

- Versión con `IEnumerable`: `SELECT * FROM courses` — sin WHERE. 50.000 filas. 800ms.
- Versión con `IQueryable`: `SELECT ... WHERE is_published = true` — 200 filas. 4ms.

En desarrollo con 10 registros de prueba, las dos versiones se comportan igual. El bug solo
se manifiesta bajo carga real, cuando ya está en producción.

---

## Cómo EF Core usa IQueryable internamente

`DbSet<T>` implementa `IQueryable<T>`. Cada vez que encadenás un método LINQ sobre él
(`.Where()`, `.Select()`, `.OrderBy()`), EF Core acumula la expresión en un árbol. Ese
árbol se traduce a SQL solo cuando se materializa la query, es decir, cuando llamás a:

- `.ToListAsync()` / `.ToList()`
- `.FirstOrDefaultAsync()` / `.SingleOrDefaultAsync()`
- `.CountAsync()`
- `.AnyAsync()`
- O cuando iterás con `await foreach`

Mientras no materializás, podés seguir agregando condiciones y EF Core las agrega al SQL:

```csharp
IQueryable<Course> query = _context.Courses.Where(c => c.IsPublished);

if (searchTerm is not null)
    query = query.Where(c => c.Title.Contains(searchTerm));  // agrega AND al SQL

if (maxPrice is not null)
    query = query.Where(c => c.Price <= maxPrice);           // agrega otro AND

var results = await query.ToListAsync(ct);
// Un solo SELECT con todos los filtros aplicados en PostgreSQL
```

Este patrón, llamado **query building dinámico**, es imposible con `IEnumerable` porque cada
`.Where()` ya habría materializado la colección anterior.

---

## El truco silencioso: AsEnumerable()

```csharp
// Esto corta la cadena IQueryable en el medio
var results = _context.Courses
    .Where(c => c.IsPublished)     // se traduce a SQL ✅
    .AsEnumerable()                // materializa acá — trae los cursos publicados a memoria
    .Where(c => c.Title.Length > 5); // filtra en memoria ⚠️
```

`.AsEnumerable()` fuerza la materialización. Todo lo que viene después es LINQ-to-Objects.
A veces es intencional (cuando la expresión no se puede traducir a SQL), pero es fácil hacerlo
sin darse cuenta y crear un filtrado parcial en DB y parcial en memoria.

---

## Por qué en GetPublishedAsync usamos IQueryable

```csharp
// src/NexaLearn.Infrastructure/Persistence/Repositories/CourseRepository.cs
public async Task<IReadOnlyList<Course>> GetPublishedAsync(CancellationToken ct)
{
    return await _context.Courses
        .Where(c => c.IsPublished)
        .AsNoTracking()
        .ToListAsync(ct);
}
```

El `.Where(c => c.IsPublished)` opera sobre `IQueryable<Course>` (que es lo que devuelve
`_context.Courses`). EF Core lo traduce a `WHERE is_published = true`. Solo llegan a memoria
los cursos que cumplen la condición.

`.AsNoTracking()` es la otra decisión importante: como esta query es de solo lectura, le decimos
a EF Core que no agregue las entidades al change tracker. Menos overhead de memoria, más velocidad.

---

## La confusión más frecuente: retornar IQueryable desde el repositorio

```csharp
// ¿Es buena idea retornar IQueryable desde el repositorio?
public IQueryable<Course> GetAll() => _context.Courses;
```

Parece conveniente porque el llamador puede agregar filtros. El problema es que filtra la
abstracción: el Application layer (que solo debería conocer Domain) ahora tiene acceso directo
a la capacidad de construir queries. Cualquier cambio en el esquema puede romper código fuera
de Infrastructure sin que el compilador lo detecte.

La alternativa correcta es que el repositorio reciba los parámetros de búsqueda y retorne
`IReadOnlyList<T>` o `Task<IReadOnlyList<T>>`. El `IQueryable` queda encapsulado dentro de
Infrastructure.

---

## Pregunta de entrevista clásica

**"¿Cuál es la diferencia entre IEnumerable e IQueryable? ¿Cuándo usarías cada uno?"**

Es la pregunta más frecuente en entrevistas .NET de nivel senior. La respuesta que esperan:

`IEnumerable` ejecuta en memoria: el filtro ya viajó desde la DB. Sirve cuando ya tenés los
datos en memoria y querés operar sobre ellos con LINQ.

`IQueryable` es una expresión pendiente de ejecutar. Cada operación LINQ agrega condiciones
al árbol que luego se traduce a SQL. Sirve cuando querés que el motor de base de datos haga
el trabajo pesado (filtrar, ordenar, paginar) antes de traer datos por la red.

El error clásico: llamar `.ToList()` antes del `.Where()`, convirtiendo `IQueryable` en
`IEnumerable` y filtrando en memoria sin darse cuenta. El resultado es correcto pero la
performance es catastrófica bajo carga real.
