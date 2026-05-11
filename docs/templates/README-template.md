# [Nombre del proyecto]

> [Una línea que explique qué hace este proyecto y para quién]

[![CI](https://github.com/tu-usuario/nexa-portfolio/actions/workflows/[proyecto]-ci.yml/badge.svg)](...)
[![Cobertura](https://img.shields.io/badge/cobertura-85%25-brightgreen)]()
[![.NET](https://img.shields.io/badge/.NET-8.0-purple)]()

---

## ¿Qué es esto?

[2-3 párrafos explicando el proyecto como si se lo contaras a alguien
que no sabe nada de tecnología. Qué problema resuelve, quién lo usaría.]

## Diagrama de arquitectura

\```mermaid
[diagrama va acá]
\```

## Estructura del proyecto

\```
src/
├── [Proyecto].Domain/          # Entidades, reglas de negocio
├── [Proyecto].Application/     # Casos de uso, CQRS
├── [Proyecto].Infrastructure/  # Base de datos, servicios externos
└── [Proyecto].API/             # Controllers, configuración
\```

## Decisiones técnicas destacadas

| Decisión | Por qué |
|----------|---------|
| [tecnología/patrón] | [razón en una línea] |

Ver carpeta [`docs/adr/`](./docs/adr/) para el detalle completo.

## Correr el proyecto

\```bash
cp .env.example .env
docker-compose up -d
dotnet run --project src/[Proyecto].API
\```

## Tests

\```bash
dotnet test
\```

## Lo que aprendí construyendo esto

[Sección honesta sobre qué fue difícil, qué cambiarías, qué aprendiste.
Esto es lo que diferencia un portfolio de un repositorio de código.]