# SmartHealth – Distributed Healthcare Booking Platform

SmartHealth is a .NET 8 microservices solution for appointment booking and notifications. It demonstrates transactional safety, event-driven communication via RabbitMQ, containerized deployment, and Kubernetes orchestration.

# Architecture Overview
The system follows a microservices architecture with asynchronous communication. The AppointmentService owns booking logic and persists data in PostgreSQL. It publishes domain events to RabbitMQ after successful transactions. The NotificationService consumes these events in the background to demonstrate decoupled processing. Distributed system concepts include transactional consistency, idempotency, dead-letter queues, and correlation IDs for traceable logs.

# Architecture Diagram (Text)
```
Client
  -> AppointmentService (REST API)
       -> PostgreSQL (transactional storage)
       -> Redis (distributed locking)
       -> RabbitMQ (appointment.created / appointment.cancelled)
            -> NotificationService (consumer)
            -> DLQ (failed messages)
  -> Frontend (Next.js UI)

Prometheus -> /metrics (AppointmentService)
Grafana -> Prometheus dashboards
```

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
- `backend/` - .NET services and solution.
- `backend/AppointmentService/` - REST API, booking logic, EF Core, event publishing, metrics.
- `backend/NotificationService/` - background worker that consumes RabbitMQ events.
- `backend/SmartHealthcare.slnx` - .NET solution.
- `backend/docker-compose.yml` - includes infra compose for local dev.
- `frontend/` - Next.js UI (App Router, React Query, Tailwind).
- `infra/` - infrastructure assets.
- `infra/k8s/` - Kubernetes manifests (deployments, services, HPA, secrets, monitoring).
- `infra/docker-compose.yml` - local multi-container orchestration.
- `infra/monitoring/` - Prometheus/Grafana configuration.
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
- Unified authentication UI (login/register)

# Prerequisites
- .NET 8 SDK
- Docker Desktop
- Kubernetes enabled in Docker Desktop
- Git
- Node.js 20+

# How to Run Locally (Docker Compose)
```bash
cd backend
docker compose build
docker compose up
```

URLs:
- Appointment API (Swagger): `http://localhost:8080/swagger`
- RabbitMQ dashboard: `http://localhost:15672` (guest / guest)
- Prometheus: `http://localhost:9090`
- Grafana: `http://localhost:3000` (admin / admin)

# How to Run Frontend
```bash
cd frontend
npm install
npm run dev
```

Open:
- `http://localhost:3000`
- Auth portal: `http://localhost:3000/auth`

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
kubectl apply -f infra/k8s/
kubectl port-forward service/appointment-service 8080:80
```

Open:
- `http://localhost:8080/swagger`

Note: `infra/k8s/secrets.yaml` contains placeholder values. Replace or override them before production deployment.

# Environment Variables
- `POSTGRES_DB` - PostgreSQL database name.
- `POSTGRES_USER` - PostgreSQL username.
- `POSTGRES_PASSWORD` - PostgreSQL password.
- `RABBITMQ_HOST` - RabbitMQ host (or `RabbitMQ__Host` in .NET config).
- `REDIS_PASSWORD` - Redis password (if enabled).
- `API_KEY` - API key required for protected endpoints.
- `NEXT_PUBLIC_API_URL` - Frontend API base URL.
- `NEXT_PUBLIC_API_KEY` - Frontend API key header (optional).
- `Frontend__Origin` - Allowed frontend origin for CORS (defaults to `http://localhost:3000` in development).
- `OTEL_EXPORTER_OTLP_ENDPOINT` - OpenTelemetry endpoint (placeholder).
- `OTEL_SERVICE_NAME` - Service name for tracing (placeholder).
- `OTEL_RESOURCE_ATTRIBUTES` - Additional trace attributes (placeholder).

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
- Frontend build (Next.js)
- Docker build and push on `main`
- Optional Kubernetes deploy if `KUBECONFIG` is provided

# Observability
- Structured JSON logs via Serilog
- Correlation IDs propagated per request
- Health endpoints for liveness/readiness
- `/metrics` (JSON) and `/metrics/prometheus` for Prometheus scraping
- Log schema includes: `serviceName`, `environment`, `correlationId`
- Tracing placeholders via `OTEL_*` environment variables

# Testing
```bash
cd backend
dotnet test
```

# Runbook
1. Start local stack: `cd backend && docker compose up`
2. Check API health: `GET /health/live` and `GET /health/ready`
3. View API logs: `docker logs -f sh-appointment`
4. View worker logs: `docker logs -f sh-notification`
5. RabbitMQ UI: `http://localhost:15672` (guest/guest)
6. Grafana UI: `http://localhost:3000` (admin/admin)
7. Reset local DB: `docker compose down -v` and re-run compose
8. Kubernetes deploy: `kubectl apply -f infra/k8s/`
9. Rollout status: `kubectl rollout status deployment/appointment-deployment`
10. Rollback: `kubectl rollout undo deployment/appointment-deployment`

# Release Checklist
1. `dotnet test` passes (unit + integration).
2. `npm run build` passes in `frontend/`.
3. CI green on `main` (build, format, tests).
4. Docker images built and pushed with `latest` and commit tags.
5. Secrets updated in cluster (no placeholder values).
6. `kubectl apply -f infra/k8s/` applied successfully.
7. Health checks return `200` on live and ready.
8. `/metrics` is scraped and dashboards render.

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
5. Extend the frontend routes and hook up future services (Auth, Doctors, Payments).

# License
MIT (placeholder)
