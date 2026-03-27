# SmartHealth – Distributed Healthcare Booking Platform

SmartHealth is a .NET 8 microservices solution for appointment booking and notifications. It demonstrates transactional safety, event-driven communication via RabbitMQ, containerized deployment, and Kubernetes orchestration.

# Architecture Overview
The system follows a microservices architecture with asynchronous communication. The AppointmentService owns booking logic and persists data in PostgreSQL. It publishes domain events to RabbitMQ after successful transactions. The NotificationService consumes these events in the background to demonstrate decoupled processing. Distributed system concepts include transactional consistency, idempotency, dead-letter queues, and correlation IDs for traceable logs.

Diagram (text description):
Client -> AppointmentService (REST API) -> PostgreSQL (transactional storage)
AppointmentService -> RabbitMQ (appointment.created / appointment.cancelled events)
RabbitMQ -> NotificationService (consumer)
RabbitMQ -> DLQ (failed messages)
All services -> structured logs + health endpoints

# Tech Stack
- .NET 8, ASP.NET Core Minimal API
- Entity Framework Core
- PostgreSQL
- RabbitMQ (with DLQ)
- Redis (distributed locking)
- Docker, Docker Compose
- Kubernetes (manifests + HPA)
- GitHub Actions CI/CD
- Serilog structured logging
- Prometheus + Grafana (metrics and dashboards)

# Folder Structure
- `AppointmentService/` - REST API, booking logic, EF Core, event publishing, metrics.
- `NotificationService/` - background worker that consumes RabbitMQ events.
- `k8s/` - Kubernetes manifests (deployments, services, HPA, secrets, monitoring).
- `docker-compose.yml` - local multi-container orchestration.
- `.github/` - GitHub Actions workflows.

# Features
- Appointment booking
- Cancel appointment
- Availability search
- Transaction safety (SERIALIZABLE isolation)
- Unique slot protection
- RabbitMQ event publishing
- Dead-letter queue handling
- Correlation ID logging
- Health checks
- Docker orchestration
- Kubernetes deployment
- Horizontal autoscaling
- CI pipeline

# Prerequisites
- .NET 8 SDK
- Docker Desktop
- Kubernetes enabled in Docker Desktop
- Git
- Node.js (optional, for tooling if needed)

# How to Run Locally (Docker Compose)
```bash
docker compose build
docker compose up
```

URLs:
- Appointment API (Swagger): `http://localhost:8080/swagger`
- RabbitMQ dashboard: `http://localhost:15672` (guest / guest)
- Prometheus: `http://localhost:9090`
- Grafana: `http://localhost:3000` (admin / admin)

# How to Test API
Create appointment:
```bash
curl -X POST http://localhost:8080/appointments \
  -H "Content-Type: application/json" \
  -H "X-API-KEY: <API_KEY>" \
  -d '{
    "patientId": "11111111-1111-1111-1111-111111111111",
    "doctorId": "22222222-2222-2222-2222-222222222222",
    "slotTime": "2026-04-01T10:00:00Z"
  }'
```

Cancel appointment:
```bash
curl -X DELETE http://localhost:8080/appointments/<APPOINTMENT_ID> \
  -H "X-API-KEY: <API_KEY>"
```

Get availability:
```bash
curl "http://localhost:8080/appointments/availability?doctorId=22222222-2222-2222-2222-222222222222&from=2026-04-01T00:00:00Z&to=2026-04-02T00:00:00Z"
```

Get appointments:
```bash
curl http://localhost:8080/appointments
```

# How to Run with Kubernetes
```bash
kubectl apply -f k8s/
kubectl port-forward service/appointment-service 8080:80
```

Open:
- `http://localhost:8080/swagger`

# Environment Variables
- `POSTGRES_DB` - PostgreSQL database name.
- `POSTGRES_USER` - PostgreSQL username.
- `POSTGRES_PASSWORD` - PostgreSQL password.
- `RABBITMQ_HOST` - RabbitMQ host (or `RabbitMQ__Host` in .NET config).
- `API_KEY` - API key required for protected endpoints.

# Event Flow
- `appointment.created` event is published after a successful booking transaction.
- `appointment.cancelled` event is published after cancellation.
- NotificationService consumes events and logs processing.
- Failed messages are routed to a Dead Letter Queue (DLQ) for inspection.

# Health Checks
Endpoints:
- `/health/live` - liveness probe (service is running).
- `/health/ready` - readiness probe (dependencies are available).

# CI Pipeline
GitHub Actions workflow performs:
- Restore and build
- Test (if any test projects exist)
- Docker build and push on `main`
- Optional Kubernetes deploy if `KUBECONFIG` is provided

# Observability
- Structured JSON logs via Serilog
- Correlation IDs propagated per request
- Health endpoints for liveness/readiness
- `/metrics` (JSON) and `/metrics/prometheus` for Prometheus scraping

# Scaling
Horizontal Pod Autoscaler (HPA) is configured for services:
- CPU target: 70%
- Min replicas: 2
- Max replicas: 5

# Future Improvements
- Outbox pattern hardening (cleanup, retries, and visibility)
- Advanced metrics and alerting
- Distributed tracing (OpenTelemetry)
- Authentication/authorization
- Reschedule appointment feature

# How to Continue Development
1. Add new services under their own folders.
2. Define new domain events and update consumers.
3. Add EF Core migrations for schema changes.
4. Extend Kubernetes manifests for new components.

# License
MIT (placeholder)
