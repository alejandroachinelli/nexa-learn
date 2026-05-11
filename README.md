# Nexa Portfolio

Portfolio técnico profesional de proyectos de software.
Arquitectura limpia, buenas prácticas y documentación detallada.

## 🗂️ Proyectos

| Proyecto | Descripción | Stack principal | Demo |
|----------|-------------|-----------------|------|
| [nexa-learn](./projects/nexa-learn) | Fundamentos C# moderno y arquitectura | .NET 8, PostgreSQL | — |
| nexa-core | ERP modular multi-tenant | .NET 8, Kafka, Redis | próximamente |
| nexa-bank | Plataforma bancaria digital | .NET 8, gRPC, pgcrypto | próximamente |
| nexa-ai | Asistente inteligente con RAG | Python, Claude API | próximamente |
| nexa-folio | Portfolio web personal | Next.js 14, TypeScript | próximamente |

## 🏗️ Cómo están construidos estos proyectos

Todos los proyectos del portfolio siguen la misma base arquitectónica.
Cada carpeta tiene su README con diagramas detallados, decisiones técnicas
documentadas y un comando de setup para correrlo localmente.

## 🚀 Correr cualquier proyecto localmente

Cada proyecto tiene un `docker-compose.yml` completo.
El setup siempre es el mismo:

\```bash
cd projects/nombre-proyecto
cp .env.example .env
docker-compose up -d
\```

## 📐 Estándares que usamos

- **Arquitectura**: Clean Architecture con CQRS y DDD
- **Tests**: cobertura mínima del 80% en capa Application
- **Documentación**: cada decisión técnica tiene su ADR
- **Commits**: Conventional Commits en inglés
- **Código**: inglés — documentación: español

## 📁 Estructura del repositorio

\```
nexa-portfolio/
├── projects/          # Un proyecto por carpeta
├── docs/
│   ├── standards/     # Estándares y convenciones
│   ├── decisions/     # Decisiones globales del portfolio
│   ├── guides/        # Guías de setup y herramientas
│   ├── assets/        # Imágenes y diagramas
│   └── templates/     # Plantillas de README y ADR
└── scripts/           # Scripts de utilidad
\```