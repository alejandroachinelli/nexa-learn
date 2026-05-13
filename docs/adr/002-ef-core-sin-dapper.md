# ADR-002: EF Core como único ORM en Etapa 3

**Fecha**: 2026-05-13
**Estado**: Aceptada
**Autor**: Alejandro Martin Achinelli

---

## Contexto

En la Etapa 3 implementamos el Infrastructure layer: repositorios concretos,
Unit of Work y la configuración de EF Core contra PostgreSQL. Dado que el
Application layer ya separa commands de queries (CQRS), existe la opción
de usar herramientas distintas para escritura y lectura.

La pregunta concreta: ¿usamos solo EF Core, o usamos EF Core para escrituras
y Dapper para lecturas?

---

## Decisión

Usar EF Core exclusivamente en esta etapa, tanto para escrituras como para
lecturas. Las queries de lectura se implementan con `IQueryable<T>` y
projections via `.Select()` directamente en EF Core.

---

## Alternativas consideradas

### Opción A — Solo EF Core (elegida)

- **Ventajas**: una sola herramienta, una sola configuración, las projections
  con `.Select()` generan SQL eficiente, los tests de integración son
  homogéneos, la curva de mantenimiento es baja.
- **Desventajas**: para queries con joins muy complejos o agregaciones pesadas,
  EF Core puede generar SQL subóptimo. No tenemos el control total del SQL
  que se ejecuta.

### Opción B — EF Core para escrituras + Dapper para lecturas

- **Ventajas**: SQL explícito en lecturas, control total del rendimiento,
  Dapper es extremadamente rápido para queries de solo lectura porque no
  tiene tracking overhead.
- **Desventajas**: dos herramientas, dos formas de manejar la conexión, dos
  formas de escribir tests de integración. La complejidad operacional se
  duplica. Requiere justificación de performance medida con benchmarks reales,
  no asumida.

---

## Por qué elegimos EF Core solo

El dominio de nexa-learn tiene tres aggregates: `Course`, `Student` y
`Enrollment`. Las queries actuales son:

- `GetCourseByIdQuery` — un curso por ID, projection a `CourseDto`
- `ListPublishedCoursesQuery` — lista de cursos publicados, projection a lista de `CourseDto`
- `GetStudentProgressQuery` — un estudiante + sus inscripciones + cursos relacionados

Ninguna de estas queries tiene 5+ joins, subqueries correlacionadas ni
agregaciones complejas. EF Core genera SQL perfectamente aceptable para este
tipo de consultas cuando se usa `IQueryable<T>` con `.Select()` en lugar de
cargar entidades completas y filtrar en memoria.

El criterio para introducir Dapper es concreto: queries con joins complejos que
generan SQL problemático medido con un profiler real, o reportes con agregaciones
que EF Core no puede traducir bien. Eso no es el caso hoy, y probablemente no
lo sea hasta nexa-core o nexa-bank donde el modelo de datos es más complejo.

Agregar Dapper anticipadamente sería optimización prematura — pagaríamos el
costo de la complejidad sin el beneficio de performance demostrado.

---

## Consecuencias

**Se vuelve más fácil:**
- Configurar y mantener el Infrastructure layer: una sola herramienta, una
  sola abstracción de conexión.
- Tests de integración con Testcontainers: todos los repositorios usan el
  mismo DbContext, sin mezclar connection strings ni transaction scopes.
- Evolución del esquema: las migraciones de EF Core cubren tanto el modelo de
  escritura como el de lectura.

**Se vuelve más difícil o genera deuda:**
- Si aparecen queries de reporting complejo, vamos a necesitar evaluar Dapper
  o vistas de base de datos. Ese es el momento correcto para introducirlo.
- EF Core tracking en queries de solo lectura tiene overhead mínimo pero
  medible. Mitigamos esto con `.AsNoTracking()` en todas las queries.

**Señales para revisar esta decisión:**
- Una query específica genera SQL con N+1 o joins que el profiler marca como
  problemáticos.
- Necesitamos una query que EF Core no puede traducir a SQL (LINQ intraducible).
- Benchmarks muestran que esa query es un cuello de botella real bajo carga.

---

## Decisión adicional: HasMany en lugar de OwnsMany para Module y Lesson

Durante la implementación de los tests de integración se descubrió una limitación
conocida de EF Core: cuando se usa `OwnsMany` con backing fields privados
(`private readonly List<T>`) y `PropertyAccessMode.Field`, EF Core no puede
detectar nuevas entidades agregadas a la colección en escenarios de update
post-carga. Llamar a `context.Add()` sobre una owned entity en `OwnsMany`
causa que EF Core desanexe las entidades existentes y marque las nuevas como
`Modified` en lugar de `Added`, generando `UPDATE` en vez de `INSERT`.

**Decisión**: usar `HasMany` con `OnDelete(DeleteBehavior.Cascade)` para las
relaciones `Course → Module → Lesson`. El esquema de base de datos es idéntico
(mismas tablas, mismas columnas, mismos índices), se agregan FK constraints
explícitas. El dominio no cambia.

**Restricción de diseño que emerge**: en este modelo, los módulos se crean y
asocian al curso antes de persistirlo. Una vez que el curso está en la base de
datos, no se agregan módulos nuevos via `Update()` — se usa `AddAsync()` + `SaveChanges`
en el contexto de creación inicial. El test `Update_ChangesArePersistedAfterSaveChanges`
fue reemplazado por `Update_PublishCourse_PersistsIsPublishedTrue` que valida
el caso de uso real de `Update()`: persistir cambios en propiedades escalares
(como `IsPublished`) de un aggregate ya guardado.

---

## Referencias

- [EF Core — Raw SQL queries](https://learn.microsoft.com/en-us/ef/core/querying/raw-sql)
- [Dapper — GitHub](https://github.com/DapperLib/Dapper)
- [docs/guides/003-ef-core-vs-dapper.md](../guides/003-ef-core-vs-dapper.md)
- [docs/guides/004-ef-core-materialization.md](../guides/004-ef-core-materialization.md)
