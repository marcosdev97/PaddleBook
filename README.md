# PaddleBook

PaddleBook es un proyecto personal orientado al aprendizaje y a la
creación de un portfolio profesional en **.NET 8**, explorando conceptos
de arquitectura, microservicios, mensajería y observabilidad.\
El objetivo es construir un sistema realista para gestionar reservas de
pistas de pádel y practicar tecnologías que hoy se usan en proyectos
backend modernos.

------------------------------------------------------------------------

## 🎯 Objetivos del proyecto

-   Practicar el desarrollo de APIs con **ASP.NET Core** y **Minimal
    APIs**.
-   Aplicar principios de **Clean Architecture** a pequeña escala.
-   Implementar un flujo de eventos entre microservicios usando
    **RabbitMQ**.
-   Aprender patrones de resiliencia:
    -   Trazabilidad con **CorrelationId / CausationId**
    -   **Idempotencia** en consumidores
    -   **Reintentos automáticos** + **DLQ**
-   Mejorar observabilidad con:
    -   **HealthChecks**
    -   **Serilog**
    -   **Prometheus (métricas)**
    -   **OpenTelemetry (tracing)**
-   Ejecutar todo con **Docker Compose**.
-   Añadir integración continua (CI) real con **GitHub Actions** y
    publicación de imágenes en GHCR.

Proyecto ideal para demostrar conocimientos profesionales aun siendo
junior.

------------------------------------------------------------------------

## 🧱 Arquitectura general

``` text
+--------------------+         RabbitMQ          +-----------------------------+
|   PaddleBook.Api   |  --------------------->   |  NotificationService.Api    |
|  (API pública)     |      booking.created      |  (microservicio interno)    |
+--------------------+                           +-----------------------------+
        |                                                        |
        | EF Core                                               | EF Core
        v                                                        v
+--------------------+                           +-----------------------------+
|  PostgreSQL (DB)   |                           |   PostgreSQL (idempotencia) |
+--------------------+                           +-----------------------------+
```

### 🟦 PaddleBook.Api

-   Gestiona **pistas** y **reservas**.
-   Publica eventos a RabbitMQ usando `EventEnvelope<T>`.
-   Middleware de **CorrelationId**.
-   Health checks, métricas, logs estructurados y trazas.

### 🟧 NotificationService.Api

-   Escucha el evento `booking.created`.
-   Implementa:
    -   **Idempotencia**
    -   **Reintentos con delay**
    -   **DLQ**
-   Procesa las notificaciones de forma fiable.

------------------------------------------------------------------------

## 🧪 Tecnologías empleadas

### Backend

-   .NET 8 + ASP.NET Core
-   Minimal APIs
-   FluentValidation
-   EF Core + PostgreSQL
-   JWT Authentication

### Mensajería y resiliencia

-   RabbitMQ
-   `EventEnvelope<T>` con CorrelationId/CausationId
-   Idempotencia basada en tabla `ProcessedMessages`
-   Reintentos controlados via exchange de retry + DLQ

### Observabilidad

-   **Serilog** → Logging estructurado (JSON)
-   **HealthChecks** para API, DB y RabbitMQ
-   **Prometheus** → `/metrics`
-   **OpenTelemetry**:
    -   ASP.NET Core instrumentation
    -   EF Core instrumentation
    -   HttpClient instrumentation
    -   Spans personalizados

### DevOps

-   Docker + Docker Compose
-   GitHub Actions (CI)
    -   build → test → docker build → push to GHCR

------------------------------------------------------------------------

## 📁 Estructura de la solución

``` text
PaddleBook.sln
│
├── PaddleBook.Api/               # API pública
│   ├── Contracts/
│   ├── Messaging/                # Envelope, publisher
│   ├── Middleware/               # CorrelationId
│   ├── Validation/
│   ├── appsettings.json
│   └── Program.cs
│
├── NotificationService.Api/      # Microservicio interno
│   ├── Messaging/
│   ├── Persistence/
│   ├── appsettings.json
│   └── Program.cs
│
├── PaddleBook.Domain/            # Entidades
├── PaddleBook.Infrastructure/     # EF Core + repositorios
├── PaddleBook.Application/        # Casos de uso (ligero)
│
├── docker-compose.yml
└── .github/workflows/
       └── ci.yml                 # Pipeline CI
```

------------------------------------------------------------------------

## ▶️ Cómo ejecutar el proyecto

### Requisitos

-   Docker Desktop instalado

### Levantar todo

``` bash
docker compose up --build
```

Esto inicia:

-   API → http://localhost:5000
-   Swagger → http://localhost:5000/swagger
-   RabbitMQ → http://localhost:15672 (user/pass: paddle/paddle)
-   Métricas → http://localhost:5000/metrics

------------------------------------------------------------------------

## 📡 Flujo de eventos (booking.created)

1.  El usuario crea una reserva desde la API.\
2.  Se genera un `EventEnvelope<T>` con:
    -   CorrelationId
    -   CausationId
    -   MessageId
    -   Payload (la reserva)
3.  El evento se publica en RabbitMQ.
4.  NotificationService.Api lo consume:
    -   Comprueba idempotencia
    -   Procesa el mensaje
    -   Reintenta si falla
    -   Envía a DLQ si supera el límite

------------------------------------------------------------------------

## 🔍 Observabilidad

### Health Checks

-   `/health`\
    Comprueba API, Postgres y RabbitMQ.

### Prometheus

-   `/metrics`\
    Métricas HTTP + personalizadas.

### OpenTelemetry Tracing

-   Instrumentación completa para:
    -   Solicitudes HTTP
    -   DB queries
    -   Mensajes procesados
-   Exportación a consola en contenedores (fácil de conectar luego a
    Jaeger/Tempo).

------------------------------------------------------------------------

## 🔄 CI/CD (solo CI activado actualmente)

Este repositorio incluye un pipeline **CI** con GitHub Actions:

-   Compila la solución
-   Ejecuta tests
-   Construye imágenes Docker
-   Publica en **GitHub Container Registry (GHCR)**

Imágenes disponibles:

    ghcr.io/marcosdev97/paddlebook-api:latest
    ghcr.io/marcosdev97/notificationservice-api:latest

------------------------------------------------------------------------

## 🌱 Trabajo futuro (ideas para seguir creciendo)

-   Migrar RabbitMQ → **Azure Service Bus**
-   Añadir un microservicio adicional (ej. "Payments")
-   Añadir dashboards reales con **Grafana**
-   Añadir CD real hacia Azure Container Apps
-   Crear tests de integración del flujo de mensajería
-   Añadir endpoints avanzados para administración

------------------------------------------------------------------------

## 👤 Autor

Proyecto creado por **Marcos Pérez**, desarrollador .NET en crecimiento,
con el objetivo de aprender arquitectura moderna, mensajería y
observabilidad, y construir un portfolio técnico sólido.
