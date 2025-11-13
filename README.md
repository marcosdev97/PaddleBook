# 🏓 PaddleBook

**PaddleBook** es un proyecto backend desarrollado en **.NET 8** que simula un sistema real de **reservas de pistas de pádel** utilizando una arquitectura moderna, orientada a escalabilidad y buenas prácticas: *Clean Architecture*, *event-driven*, microservicios y tests de integración.

Este proyecto forma parte de mi portfolio profesional para demostrar experiencia en C#, .NET y diseño backend avanzado.

---

## 🚀 Tecnologías principales

- **.NET 8 / ASP.NET Core Web API**
- **Entity Framework Core 8**
- **PostgreSQL** (Docker)
- **RabbitMQ** como message broker (eventos)
- **Identity Core** (sin UI) para autenticación JWT
- **xUnit + FluentAssertions** para tests de integración
- **Docker Compose** para orquestación
- Arquitectura por capas: **Domain, Application, Infrastructure, API**

---

## 🧩 Arquitectura del proyecto

El proyecto sigue una arquitectura limpia, separando responsabilidades de forma clara:

| Proyecto | Responsabilidad |
|----------|----------------|
| **PaddleBook.Api** | Endpoints minimal API, autenticación y publicación de eventos |
| **PaddleBook.Application** | Lógica de negocio, servicios, validaciones |
| **PaddleBook.Domain** | Entidades, Value Objects, lógica de dominio |
| **PaddleBook.Infrastructure** | EF Core, configuración de Identity y persistencia |
| **NotificationService.Api** | Microservicio independiente que escucha eventos de RabbitMQ |
| **PaddleBook.Test** | Pruebas de integración y API |

---

## 🧠 Funcionalidades actuales

### ✔️ Implementado

- **Autenticación JWT** con Identity Core
- **CRUD de pistas de pádel**
  - Endpoints públicos y protegidos
  - Roles: *admin* y *player*
- **Eventos de dominio → RabbitMQ**
  - Al crear una reserva, se publica el evento `booking.created`
- **Microservicio NotificationService**
  - Se subscribe a RabbitMQ y procesa eventos recibidos
- **Tests de integración**
  - Probar endpoints protegidos
  - Crear un admin → login → crear pista
- **Docker Compose**
  - PostgreSQL
  - RabbitMQ (con panel en localhost:15672)
  - Servicios en contenedores

---

## 🧪 Pruebas e integración continua

- Uso de `WebApplicationFactory` para pruebas de API reales
- DB InMemory para tests
- Simulación de tokens JWT válidos

---

## 🐳 Docker

Para levantar todo el entorno:

```bash
docker compose up -d
```

Esto levantará:

- PostgreSQL → `localhost:5432`
- RabbitMQ Management UI → `http://localhost:15672`
- API PaddleBook
- NotificationService

---

## 🔧 Configuración de ejemplo

```json
"Rabbit": {
  "Host": "localhost",
  "Port": 5672,
  "User": "paddle",
  "Pass": "paddle",
  "Exchange": "paddle.events",
  "Queue": "paddle.notifications",
  "RoutingKey": "booking.created"
}
```

---

## 📈 Mejoras previstas (próximos pasos)

### 🟡 En progreso
- Comando de creación de reservas en PaddleBook.Application  
- Publicación consistente del evento `booking.created`  
- Mejor manejo de errores en NotificationService  

### 🔜 Próximas mejoras
- Sistema de envío de email/SMS en NotificationService  
- Dashboard de administración (posible Blazor o React)
- Migración hacia microservicios completos
- Auditoría y métricas (OpenTelemetry, Serilog, Prometheus)
- Implementar patrón Outbox para garantizar consistencia entre DB y eventos

---

## 📸 Diagrama conceptual

```
[Cliente] → [PaddleBook.Api] → [Application Layer] → [Infrastructure / PostgreSQL]
                                      |
                                      |→ RabbitMQ Exchange → [NotificationService.Api]
```

---

## 👨‍💻 Autor

**Marcos Pérez**  
Desarrollador .NET y Unity XR  
Repositorios y contacto:  
🔗 https://github.com/marcosdev97

---

## ⭐ Resumen

PaddleBook simula un sistema real de reservas, aplicando los conceptos esenciales que hoy buscan las empresas en desarrolladores backend:

- Buenas prácticas
- Arquitectura limpia
- Microservicios
- Mensajería asíncrona
- Contenedores
- Seguridad y pruebas automatizadas

Ideal para demostrar habilidades prácticas en C# / .NET.
