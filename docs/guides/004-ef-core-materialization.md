# EF Core y materialización de entidades de dominio

Este documento explica los problemas reales que encontré al conectar
el Domain layer de nexa-learn con EF Core, y las decisiones que tomé para
resolverlos sin comprometer las invariantes del dominio.

---

## Qué es la materialización

Cuando EF Core lee una fila de la base de datos, necesita convertirla en
un objeto C#. Ese proceso se llama **materialización**: EF Core construye
la instancia del tipo y llena sus propiedades con los valores de las columnas.

Para hacerlo, EF Core necesita responder dos preguntas:
1. ¿Cómo construyo el objeto? (qué constructor usar)
2. ¿Cómo seteo las propiedades/campos? (property access mode)

En un CRUD clásico con propiedades públicas con setters, esto es trivial. En
un dominio DDD con constructores privados, factory methods y propiedades
inmutables, hay que ser explícito.

---

## Por qué EF Core necesita constructores sin parámetros

EF Core puede materializar objetos de dos formas:

**1. Constructor injection**: EF Core busca un constructor cuyos parámetros
coincidan con propiedades mapeadas y les inyecta los valores leídos de la DB.

**2. Constructor sin parámetros**: EF Core construye el objeto vacío y después
setea las propiedades/campos una a una, directamente via reflexión.

El problema aparece con los owned types. Cuando mapeás `Money` con `OwnsOne`,
EF Core lo trata como una **navegación** (no como una propiedad escalar). Y hay
una regla: las navegaciones no pueden inyectarse en constructores.

El constructor de `Course` es:

```csharp
private Course(Guid id, CourseTitle title, Money price) : base(id)
```

- `id` → Guid escalar → puede inyectarse ✓
- `title` → CourseTitle con ValueConverter → escalar mapeado → puede inyectarse ✓
- `price` → Money configurado con OwnsOne → **navegación** → **no puede inyectarse** ✗

EF Core lanza exactamente este error:

```
Cannot bind 'price' in 'Course(Guid id, CourseTitle title, Money price)'
Note that only mapped properties can be bound to constructor parameters.
Navigations to related entities, including references to owned types, cannot be bound.
```

La solución es darle a EF Core una salida: un constructor sin parámetros.
Cuando EF Core ve múltiples constructores, elige el que puede satisfacer
completamente. Si no puede satisfacer ninguno con parámetros, usa el vacío
y después setea todo via reflexión.

```csharp
// En Course.cs — solo para EF Core
#pragma warning disable CS8618
private Course() { } // for EF Core materialization — properties set via backing fields
#pragma warning restore CS8618

// El constructor real del dominio sigue intacto
private Course(Guid id, CourseTitle title, Money price) : base(id)
{
    Title = title;
    Price = price;
    IsPublished = false;
}
```

Lo mismo aplica a `Entity<TId>` y `AggregateRoot<TId>`, porque `Course` hereda
de ellos y necesita poder encadenar el constructor vacío:

```csharp
protected Entity() { } // for EF Core materialization
protected AggregateRoot() { }  // for EF Core materialization
```

---

## Por qué #pragma CS8618 y no hacer las propiedades nullable

La advertencia CS8618 dice: "esta propiedad no-nullable sale del constructor
sin ser inicializada". Es correcta: el constructor vacío no asigna nada.

La "solución" obvia sería declarar `Title` y `Price` como nullable:

```csharp
// MAL — compromete las invariantes del dominio
public CourseTitle? Title { get; }
public Money? Price { get; }
```

Eso es un error. Si `Title` puede ser `null`, cualquier código que use `Course`
necesita hacer null-checks o puede explotar en runtime. Las invariantes del
dominio dicen que un `Course` válido **siempre** tiene `Title` y `Price`. El
constructor `private Course(...)` lo garantiza.

El constructor vacío no viola esa invariante en producción porque:
1. Solo EF Core lo llama, internamente, para materializar datos que ya existían en la DB
2. Esos datos entraron a la DB a través del constructor real, que sí valida todo
3. EF Core completa `Title` y `Price` inmediatamente después via backing fields

El `#pragma` le dice al compilador: "sé lo que hago aquí, suprimí la advertencia
solo en esta línea". El resto del código mantiene las garantías de null-safety.

---

## PropertyAccessMode.Field vs PropertyAccessMode.Property

Después de usar el constructor vacío, EF Core necesita setear las propiedades.
La pregunta es: ¿cómo las setea?

**`PropertyAccessMode.Property`** (default): usa la propiedad directamente.
Para leer: llama al getter. Para escribir: llama al setter. Si la propiedad
no tiene setter público o privado, falla.

**`PropertyAccessMode.Field`**: accede al backing field directamente via
reflexión, ignorando los getter/setter. Para auto-properties como `{ get; }`,
el compilador genera un campo oculto llamado `<NombrePropiedad>k__BackingField`.
EF Core puede setear ese campo incluso si es `readonly`, porque la reflexión
bypassa las restricciones del compilador.

En nexa-learn necesité `PropertyAccessMode.Field` para las colecciones privadas
y para la navegación `Price`:

```csharp
// Módulos: la colección es private readonly List<Module> _modules
builder.Navigation(c => c.Modules)
    .UsePropertyAccessMode(PropertyAccessMode.Field);

// Price: Money Price { get; } — no tiene setter, EF Core usa <Price>k__BackingField
builder.Navigation(c => c.Price)
    .UsePropertyAccessMode(PropertyAccessMode.Field);
```

Para `_modules` el campo es explícito (lo declaré yo). Para `Price`, el campo
es implícito — el compilador lo genera, EF Core lo descubre por convención.

**¿Cuándo necesitás Field vs Property?**

| Situación | Modo |
|-----------|------|
| Propiedad con setter público | Property (default) |
| Propiedad con setter privado | Property o Field, ambos funcionan |
| Colección privada como backing field (`_items`) | Field |
| Auto-property readonly (`{ get; }`) | Field |
| Propiedad con lógica en el setter que no querés que EF Core llame | Field |

---

## Por qué Navigation() debe ir después de OwnsOne()

Esta es la parte que me quemó en el proceso. Mi primer intento:

```csharp
// MAL — Navigation() antes de OwnsOne()
builder.Navigation(c => c.Price)
    .UsePropertyAccessMode(PropertyAccessMode.Field); // ← explota aquí

builder.OwnsOne(c => c.Price, money => { ... });
```

Error:

```
Navigation 'Course.Price' was not found.
Please add the navigation to the entity type before configuring it.
```

La razón es que `Navigation()` referencia una navegación que el modelo de EF Core
todavía no conoce. La navegación `Price` existe en el modelo solo después de que
`OwnsOne()` la registra. EF Core construye el modelo de forma incremental: cada
llamada al fluent API agrega metadata al modelo. Si intentás configurar una pieza
de metadata que todavía no existe, falla.

El orden correcto:

```csharp
// 1. Primero registrar la navegación con OwnsOne
builder.OwnsOne(c => c.Price, money =>
{
    money.Property(m => m.Amount).HasColumnName("price")...;
    money.Property(m => m.Currency).HasColumnName("currency")...;
});

// 2. Después de que la navegación existe, configurar su access mode
builder.Navigation(c => c.Price)
    .UsePropertyAccessMode(PropertyAccessMode.Field);
```

La regla general: configurá la existencia antes de configurar el comportamiento.

---

## El resultado en la migración generada

Todo esto se traduce en SQL correcto. La migración `InitialCreate` tiene exactamente
lo que esperaba:

```sql
-- courses: Money como dos columnas inline, sin tabla separada
id uuid NOT NULL,
title character varying(200) NOT NULL,
price numeric(18,2) NOT NULL,     -- Money.Amount
currency character varying(3) NOT NULL,  -- Money.Currency
is_published boolean NOT NULL

-- enrollments: uuid[] nativo de PostgreSQL para la lista de lecciones completadas
completed_lesson_ids uuid[] NOT NULL

-- Índice único que previene duplicados a nivel de BD
CREATE UNIQUE INDEX ix_enrollments_student_course
    ON enrollments (student_id, course_id)
```

La invariante "un estudiante no puede inscribirse dos veces al mismo curso" está
garantizada en dos niveles: en el dominio (el handler lo verifica antes de crear)
y en la base de datos (el índice único lo rechaza si llegara a pasar de todas formas).

---

## La pregunta de entrevista

**"¿Cómo manejás la materialización de entidades DDD con constructores
privados en EF Core?"**

Respuesta:

"EF Core necesita poder construir instancias de las entidades cuando lee de la DB.
En dominio DDD con constructores privados, hay dos opciones: o permitís que EF Core
use constructor injection (funciona si todos los parámetros son escalares mapeados),
o agregás un constructor sin parámetros privado solo para EF Core.

El constructor injection falla cuando algún parámetro es un owned type (como un
value object mapeado con OwnsOne), porque EF Core trata las navegaciones de forma
distinta a los escalares y no puede inyectarlas en constructores.

Con el constructor vacío, EF Core lo construye y después setea las propiedades
via reflexión usando los backing fields. Para propiedades `{ get; }` sin setter,
configurás `PropertyAccessMode.Field` y EF Core usa el campo oculto
`<Propiedad>k__BackingField` que genera el compilador. La reflexión en .NET puede
setear esos campos incluso si son readonly.

Nunca hago las propiedades nullable para evitar la advertencia CS8618: eso rompe
las invariantes del dominio. Uso `#pragma warning disable CS8618` solo en el
constructor de EF Core y dejo el resto del código con las garantías de null-safety."
