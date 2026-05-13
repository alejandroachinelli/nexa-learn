# EF Core vs Dapper — cuándo usar cada herramienta

La pregunta aparece en casi toda entrevista de .NET: ¿EF Core o Dapper?
La respuesta correcta no es ninguno de los dos. Es saber cuándo cada uno
es la herramienta adecuada.

---

## Qué es cada uno internamente

### EF Core

EF Core es un ORM (Object-Relational Mapper) completo. Mantiene un grafo
de objetos en memoria (el `DbContext`) y rastrea los cambios que hacés sobre
esos objetos. Cuando llamás a `SaveChanges()`, traduce esos cambios a SQL.

Lo que hace por vos:
- Traduce LINQ a SQL en tiempo de ejecución
- Rastrea cambios en entidades (change tracking)
- Maneja relaciones entre entidades
- Genera y aplica migraciones del esquema
- Cachea metadata del modelo en memoria

Lo que cuesta:
- El change tracker tiene overhead: cada entidad que cargás se registra
  en el DbContext con su estado original para poder detectar cambios.
- La traducción de LINQ a SQL tiene límites: expresiones complejas a veces
  generan SQL subóptimo o directamente fallan con "could not be translated".

### Dapper

Dapper es un micro-ORM. No hace nada automáticamente. Vos escribís el SQL,
él mapea el resultado a objetos.

```csharp
var courses = await connection.QueryAsync<CourseDto>(
    "SELECT id, title, price FROM courses WHERE is_published = true",
    commandType: CommandType.Text);
```

Lo que hace por vos:
- Mapea filas de resultados a objetos C# por nombre de columna
- Maneja parámetros de forma segura (protege contra SQL injection)
- Es extremadamente rápido porque no tiene overhead de tracking ni
  traducción de LINQ

Lo que no hace:
- No rastrea cambios
- No genera migraciones
- No entiende relaciones entre entidades (lo hacés a mano con JOINs)

---

## La diferencia real de performance y cuándo importa

Dapper es más rápido que EF Core para lecturas. Eso es un hecho. La
diferencia típica en benchmarks es 30-50% en favor de Dapper para queries simples.

Pero el 50% más rápido de ¿cuánto? Si una query tarda 2ms con EF Core y 1ms
con Dapper, la diferencia absoluta es 1ms. Eso no es perceptible para un usuario
y no va a aparecer en tus métricas de SLA.

La diferencia importa cuando:
- La query tarda 200ms con EF Core por SQL ineficiente y 20ms con Dapper
  (eso sí es una diferencia real)
- Estás ejecutando esa query miles de veces por segundo bajo alta concurrencia
- El profiler de base de datos muestra que esa query específica es el
  cuello de botella del sistema

La diferencia NO importa cuando:
- La query es simple (un SELECT con uno o dos joins)
- El tiempo de red a la base de datos es 10x el tiempo de procesamiento del ORM
- Todavía no mediste nada con un profiler real

La regla que sigo: no cambio de herramienta sin un benchmark que justifique
el costo de la complejidad adicional.

---

## IEnumerable vs IQueryable — dónde se ejecuta el filtro

Este es el tema más importante de EF Core y el que más bugs silenciosos genera.

`IQueryable<T>` representa una query que todavía no se ejecutó. Cuando encadenás
operaciones LINQ sobre un `IQueryable`, EF Core las acumula y las traduce a SQL
cuando finalmente iterás el resultado (con `.ToList()`, `.FirstOrDefault()`, etc.).
El filtro se ejecuta en la base de datos.

`IEnumerable<T>` representa datos que ya están en memoria. Cuando encadenás
operaciones LINQ sobre un `IEnumerable`, C# las ejecuta en el proceso local.
El filtro se ejecuta en tu servidor de aplicación, no en la base de datos.

---

## Ejemplo concreto: query mal escrita vs bien escrita

Imaginate que quiero obtener los cursos publicados del repositorio.

### Versión mal escrita con IEnumerable

```csharp
// MAL — carga todos los cursos a memoria y filtra en C#
public async Task<IReadOnlyList<Course>> GetPublishedAsync(CancellationToken ct)
{
    // ToListAsync() materializa TODA la tabla en memoria
    var allCourses = await _context.Courses.ToListAsync(ct);

    // Este filtro corre en C#, no en PostgreSQL
    return allCourses.Where(c => c.IsPublished).ToList();
}
```

El SQL generado es:
```sql
SELECT id, title, price, currency, is_published FROM courses
```

Trae todos los cursos de la base de datos, aunque solo publiqués uno de cien.

### Versión bien escrita con IQueryable

```csharp
// BIEN — el filtro viaja a PostgreSQL
public async Task<IReadOnlyList<Course>> GetPublishedAsync(CancellationToken ct)
{
    return await _context.Courses
        .Where(c => c.IsPublished)    // se acumula en el query tree
        .AsNoTracking()               // sin overhead de change tracking
        .ToListAsync(ct);             // acá se ejecuta el SQL
}
```

El SQL generado es:
```sql
SELECT id, title, price, currency, is_published
FROM courses
WHERE is_published = true
```

El filtro viaja a PostgreSQL. Si tenés un índice en `is_published`, lo usa.

### Por qué el error es tan común

La firma `IEnumerable<T>` aparece en muchos repositorios porque se ve "más simple".
El compilador no protesta porque `IQueryable<T>` implementa `IEnumerable<T>`.
El bug solo aparece en performance, no en correctness — los tests pasan igual
porque el resultado es el mismo, pero en producción con millones de filas
el query tarda 30 segundos en lugar de 30ms.

En nexa-learn, los repositorios van a devolver `IReadOnlyList<T>` ya materializado
o van a exponer `IQueryable<T>` solo internamente para composición. Nunca
una colección lazy sobre datos de base de datos.

---

## Cuándo usar cada herramienta

| Situación | Herramienta |
|-----------|-------------|
| CRUD estándar, queries simples con 1-2 joins | EF Core |
| Escrituras con reglas de negocio complejas | EF Core |
| Queries con 5+ joins o agregaciones pesadas | Dapper o raw SQL de EF Core |
| Reportes con GROUP BY complejos | Dapper |
| Performance crítica demostrada con benchmarks | Dapper |
| Migraciones de esquema | EF Core (siempre) |
| Tests de integración con Testcontainers | EF Core (más simple de configurar) |

La combinación que usamos en proyectos más complejos (nexa-core, nexa-bank):
EF Core para el modelo de escritura y para queries simples, Dapper para queries
de reporting o dashboards donde el SQL tiene que ser exactamente lo que escribo.

---

## La pregunta de entrevista clásica

**"¿Cuándo usarías Dapper sobre EF Core?"**

La respuesta que no querés dar: "Dapper es más rápido, siempre que puedo uso Dapper."
Eso muestra que optimizás sin medir.

La respuesta correcta:

"Dapper tiene sentido cuando tengo una query específica donde EF Core genera
SQL subóptimo o intraducible, y donde el profiler confirma que esa query es
un cuello de botella real. El criterio no es la herramienta en abstracto sino
la necesidad concreta.

Para la mayoría de operaciones CRUD y queries con projections, EF Core con
`IQueryable` y `.Select()` genera SQL eficiente y me da migraciones,
change tracking y type safety. Solo introduzco Dapper cuando tengo una razón
medida para hacerlo.

También prefiero EF Core para escrituras porque el change tracker simplifica
el manejo de transacciones y relaciones. Mezclar Dapper en escrituras es
arriesgado porque perdés el Unit of Work que garantiza consistencia."
