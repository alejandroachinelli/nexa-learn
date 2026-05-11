# ADR-001: Decisiones de arquitectura base — nexa-learn

**Fecha**: 2026-05-11
**Estado**: Aceptada
**Autor**: Alejandro Martin Achinelli

---

## Contexto

nexa-learn es el primer proyecto del portfolio técnico profesional. Su objetivo
es demostrar dominio de C# moderno, patrones de arquitectura y buenas prácticas
de ingeniería de software con .NET 8. Todas las decisiones técnicas de este
proyecto sirven de base para los proyectos siguientes (nexa-core, nexa-bank,
nexa-ai). Por eso, es crítico que las decisiones sean explícitas, justificadas
y reproducibles.

---

## Decisión 1 — Dominio: gestión de cursos de aprendizaje

### Decisión

El dominio de negocio del proyecto es una plataforma de cursos con los
agregados principales: `Course`, `Module`, `Lesson`, `Student`, `Enrollment`.

### Alternativas consideradas

#### Opción A — Ecommerce / gestión de órdenes
- **Ventajas**: familiar para reclutadores, ejemplos abundantes como referencia.
- **Desventajas**: saturado en portfolios, no diferencia. Un reclutador ya vio
  cien implementaciones iguales y no puede evaluar criterio propio.

#### Opción B — Task manager
- **Ventajas**: simple de implementar, fácil de comunicar.
- **Desventajas**: modelo de dominio demasiado plano. Escasez natural de value
  objects, reglas de negocio y eventos de dominio que demuestren riqueza
  arquitectural.

#### Opción C — Gestión de cursos (elegida)
- **Ventajas**: dominio universalmente comprensible sin necesidad de explicación,
  entidades ricas con invariantes claras, value objects naturales (Email,
  CourseTitle, Duration, Money), reglas de negocio concretas (no se puede
  inscribir en un curso inactivo, un módulo no puede publicarse vacío), y
  domain events con semántica de negocio real (CoursePublished, StudentEnrolled).
- **Desventajas**: no refleja directamente la experiencia laboral en ERP,
  aunque la arquitectura es idéntica.

### Por qué elegimos esta opción

El objetivo del portfolio no es mostrar conocimiento del negocio sino
criterio arquitectural. Un dominio que genera suficiente riqueza sin requerir
explicación permite que el foco del evaluador sea la arquitectura, no el
contexto. Además, el dominio escala: se puede extender con pagos (nexa-core)
o recomendaciones por IA (nexa-ai) sin cambiar la base.

### Consecuencias

- Se vuelve más fácil: demostrar value objects, aggregates e invariantes de
  dominio de forma natural.
- Se vuelve más difícil: no cubre escenarios de alta concurrencia ni
  transacciones distribuidas — esos quedan para nexa-bank.

---

## Decisión 2 — Arquitectura: Clean Architecture con separación estricta de capas

### Decisión

Se adopta Clean Architecture con cuatro proyectos independientes:
`NexaLearn.Domain`, `NexaLearn.Application`, `NexaLearn.Infrastructure`,
`NexaLearn.Api`. La dirección de dependencias es siempre hacia adentro:
Api → Application → Domain. Infrastructure implementa interfaces definidas
en Domain.

### Alternativas consideradas

#### Opción A — Vertical Slice Architecture
- **Ventajas**: cohesión por feature, menos fricción al agregar funcionalidad
  nueva, popular en equipos modernos con MediatR.
- **Desventajas**: para un proyecto de aprendizaje de arquitectura por capas,
  oculta las dependencias entre capas que precisamente queremos hacer
  explícitas. La enseñanza del patrón se diluye.

#### Opción B — Clean Architecture por capas (elegida)
- **Ventajas**: hace explícita la dirección de dependencias, es el modelo de
  referencia de la industria .NET enterprise, cada proyecto de tests tiene
  un nivel de aislamiento claro y documentable.
- **Desventajas**: más proyectos en la solución, algo más de boilerplate para
  features simples.

### Por qué elegimos esta opción

nexa-learn es explícitamente un proyecto de fundamentos arquitecturales.
Clean Architecture hace que las decisiones de diseño sean visibles en la
estructura de carpetas y en las referencias entre proyectos. Un evaluador
técnico puede navegar la solución y entender la arquitectura sin leer
documentación. Vertical Slice puede aparecer como alternativa documentada
en un ADR futuro cuando sea relevante en otro proyecto del portfolio.

### Consecuencias

- Se vuelve más fácil: aislar tests por capa, razonar sobre dependencias,
  incorporar nuevas implementaciones de infraestructura sin tocar el dominio.
- Se vuelve más difícil: agregar features simples requiere tocar múltiples
  proyectos. Es el trade-off consciente de este modelo.

---

## Decisión 3 — API: Minimal APIs sobre Controllers

### Decisión

La capa de presentación usa Minimal APIs de ASP.NET Core en lugar de
Controllers con atributos.

### Alternativas consideradas

#### Opción A — Controllers con atributos ([ApiController], [Route], etc.)
- **Ventajas**: familiar para reclutadores de entornos enterprise, amplia
  documentación, convenciones bien establecidas.
- **Desventajas**: más ceremony, más boilerplate, patrón más antiguo que
  no refleja la dirección actual del ecosistema .NET.

#### Opción B — Minimal APIs (elegida)
- **Ventajas**: menos ceremony, más explícito, demuestra conocimiento de
  las APIs modernas de ASP.NET Core 6+. Permite organización por feature
  mediante extension methods sobre `IEndpointRouteBuilder`. Mejor integración
  con el modelo de hosting unificado de .NET 6+.
- **Desventajas**: menos familiar en proyectos enterprise legacy, requiere
  más disciplina de organización sin la convención de Controllers.

### Por qué elegimos esta opción

Minimal APIs demuestra criterio técnico actualizado. Un desarrollador que
elige Minimal APIs conscientemente (y puede explicar por qué) muestra más
madurez técnica que uno que usa Controllers por inercia. La organización
por extension methods sobre `IEndpointRouteBuilder` mantiene la cohesión
sin perder la legibilidad.

### Consecuencias

- Se vuelve más fácil: onboarding de rutas, menos archivos de infraestructura
  de API, composición más flexible.
- Se vuelve más difícil: sin convenciones automáticas de Controllers, la
  organización es responsabilidad explícita del desarrollador.

---

## Decisión 4 — Manejo de errores: Result Pattern sin excepciones de negocio

### Decisión

El flujo de negocio no usa excepciones. Todos los casos de uso retornan
`Result<T>` o `Result`. Las excepciones quedan reservadas para errores
de infraestructura inesperados (fallo de red, BD caída, etc.).

### Alternativas consideradas

#### Opción A — Excepciones de dominio (DomainException, ValidationException)
- **Ventajas**: idiomático en C#, simple de implementar, familiaridad alta.
- **Desventajas**: las excepciones son costosas en el runtime de .NET,
  mezclan flujo de control con manejo de errores, dificultan la lectura
  de los casos de uso, y hacen que las reglas de negocio sean implícitas.

#### Opción B — Result Pattern (elegida)
- **Ventajas**: hace explícitos los caminos de error en la firma del método,
  obliga al caller a manejar el error, es composable, no tiene costo de
  performance en el happy path, y documenta las reglas de negocio en el
  tipo de retorno.
- **Desventajas**: más verboso, requiere disciplina del equipo, es menos
  familiar para desarrolladores que vienen de entornos puramente OOP.

### Por qué elegimos esta opción

El Result Pattern convierte los errores de negocio en ciudadanos de primera
clase del sistema de tipos. Un `Result<Enrollment>` que puede fallar con
`CourseNotActive` o `StudentAlreadyEnrolled` documenta las reglas de negocio
más claramente que un try-catch disperso. Además, elimina una categoría
entera de bugs relacionados con excepciones no capturadas en flujos de negocio.

### Consecuencias

- Se vuelve más fácil: razonar sobre los caminos de error, testear casos
  de fallo sin necesidad de `Assert.Throws`, documentar contratos de métodos.
- Se vuelve más difícil: curva de aprendizaje si el equipo no está familiarizado,
  más tipos que mantener.

---

## Decisión 5 — Mapeo: explícito sin AutoMapper

### Decisión

El mapeo entre entidades de dominio y DTOs se hace con métodos estáticos
explícitos. No se usa AutoMapper ni ninguna librería de mapeo por reflexión.

### Alternativas consideradas

#### Opción A — AutoMapper
- **Ventajas**: menos código de mapeo repetitivo, convenciones por nombre
  automáticas.
- **Desventajas**: errores de configuración silenciosos que no se detectan
  en compile time, magia implícita que dificulta el debugging, performance
  inferior por uso de reflexión, y el comportamiento real está oculto en
  profiles que hay que buscar activamente.

#### Opción B — Mapeo explícito (elegida)
- **Ventajas**: visible en el código, debuggeable, compile-time safe,
  performance óptima, y hace explícita cada decisión de mapeo (qué campos
  se exponen y cuáles no).
- **Desventajas**: más código de mapeo que escribir y mantener.

### Por qué elegimos esta opción

En una API, decidir qué campos del dominio se exponen en un DTO es una
decisión de diseño, no mecánica. El mapeo explícito obliga a pensar cada
campo. Cuando el modelo de dominio cambia, el compilador señala exactamente
qué mappings hay que revisar, sin sorpresas en runtime.

### Consecuencias

- Se vuelve más fácil: debugging, refactoring con soporte del compilador,
  auditoría de qué datos se exponen en cada endpoint.
- Se vuelve más difícil: más código de mapeo, potencial de duplicación si
  no se organiza bien.

---

## Decisión 6 — Autenticación: JWT diferida a Etapa 4

### Decisión

JWT se implementa en la Etapa 4 del plan, después de que Domain, Application
e Infrastructure estén completos y testeados. Las primeras tres etapas no
requieren autenticación.

### Por qué elegimos esta opción

La autenticación es una concern transversal que agrega complejidad a los
tests y al setup inicial. Introducirla antes de que el núcleo de la
arquitectura esté establecido redirige el foco hacia detalles de seguridad
cuando el objetivo es demostrar arquitectura de dominio. En producción real
se diseñaría desde el inicio; en un proyecto de aprendizaje, la secuencia
didáctica importa.

### Consecuencias

- Las primeras etapas tienen endpoints públicos — documentar esto claramente
  en el README para que el evaluador entienda que es intencional.
- La incorporación de JWT en Etapa 4 debe hacerse sin modificar los casos
  de uso — el handler no debe saber si el request viene autenticado o no.

---

## Referencias

- Clean Architecture — Robert C. Martin
- Microsoft Docs: Minimal APIs overview — ASP.NET Core
- Microsoft Docs: Result pattern — .NET
- Vertical Slice Architecture — Jimmy Bogard (alternativa documentada, no elegida)
- ADR process: https://adr.github.io/
