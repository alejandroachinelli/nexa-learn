# Plan Etapa 5 — Observabilidad y CI

## T1: GitHub Actions CI ✅

**Descripción**: Workflow de CI que corre en push y pull_request a main.

**Lo que hace**:
- Build en Release
- Tests de Domain y Application (rápidos, sin Docker)
- Tests de Infrastructure con Testcontainers (Docker disponible en ubuntu-latest)
- Cache de paquetes NuGet
- Publica resultados de tests como artefacto aunque fallen

**Criterio de éxito**: badge verde en el README tras el primer push.

---

## T2: OpenTelemetry básico en la API

**Descripción**: Instrumentación de trazas y métricas en NexaLearn.Api.

**Lo que hace**:
- Agregar paquetes `OpenTelemetry.Extensions.Hosting`, `OpenTelemetry.Instrumentation.AspNetCore`, `OpenTelemetry.Instrumentation.Http`, `OpenTelemetry.Exporter.Console`
- Configurar `AddOpenTelemetry()` en Program.cs con tracing de ASP.NET Core y HTTP
- Exporter de consola para desarrollo (visible en el output de `dotnet run`)
- Exporter OTLP para producción (configurable via `appsettings`)

**Criterio de éxito**: al llamar a `GET /api/courses`, la consola muestra un span con atributos `http.method`, `http.route` y `http.status_code`.

---

## T3: Outbox Pattern para domain events

**Descripción**: Persistir y despachar domain events de forma confiable.

**Lo que hace**:
- Tabla `outbox_messages` en PostgreSQL (Id, OccurredOn, Type, Payload, ProcessedOn)
- Al llamar `SaveChangesAsync`, interceptar los domain events de los aggregates, serializarlos y guardarlos en la misma transacción
- Worker (`BackgroundService`) que cada N segundos lee mensajes no procesados y los despacha via MediatR
- Migración de EF Core para la tabla outbox

**Criterio de éxito**: `StudentEnrolled` se persiste en `outbox_messages` cuando se llama a `POST /api/enrollments`. El worker lo procesa y actualiza `ProcessedOn`.

---

## T4: README final completo

**Descripción**: README listo para evaluadores técnicos.

**Lo que hace**:
- Badge de CI (estado del workflow)
- Sección de decisiones técnicas actualizada con Etapa 4 y 5
- ADR-003 para OpenTelemetry si corresponde
- ADR-004 para Outbox Pattern

**Criterio de éxito**: un evaluador puede entender el proyecto completo en menos de 30 minutos navegando el README y los ADRs.
