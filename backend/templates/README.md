# Smart Healthcare API Template

This template provides a **ready-to-use microservice architecture** for building backend services in the Smart Healthcare system. It includes API setup, layered architecture, JWT authentication, database configuration, and testing support — all preconfigured.

---

# What This Template Generates

When you run:

```bash
dotnet new smartapi -n YourServiceName
```

It generates:

```plaintext
YourServiceName/
  YourServiceName.API/
  YourServiceName.Application/
  YourServiceName.Domain/
  YourServiceName.Infrastructure/

YourServiceName.Tests/
```

---

# Architecture Overview

The template follows a **Clean Architecture / Layered Architecture** pattern:

### API Layer (`*.API`)

* Controllers
* Middleware
* Dependency injection setup
* Swagger + JWT config
* Entry point (`Program.cs`)

---

### Application Layer (`*.Application`)

* Business logic
* Interfaces
* DTOs
* Services

---

### Domain Layer (`*.Domain`)

* Core entities
* Business rules (if any)

---

### Infrastructure Layer (`*.Infrastructure`)

* Database context (EF Core)
* Repositories
* External integrations

---

### Test Layer (`*.Tests`)

* Unit tests
* Integration tests
* Auth bypass setup (TestAuthHandler)
* TestingFactory for test server

---

# Built-in Features

✔ JWT Authentication setup
✔ Role-based authorization support
✔ Swagger with JWT support
✔ PostgreSQL (via EF Core)
✔ Auto database migrations on startup
✔ Prometheus metrics integration
✔ Docker-ready structure
✔ Health check endpoint (`/health`)
✔ Test setup with authentication mocking

---

# How It Works

The template uses:

```json
"sourceName": "SmartService"
```

This means:

```plaintext
SmartService → replaced with your service name
```

Example:

```bash
dotnet new smartapi -n UserService
```

Becomes:

```plaintext
SmartService.API → UserService.API
SmartService.Application → UserService.Application
```

---

# How to Use

### 1. Install Template

```bash
dotnet new install ./backend/templates/smart-api
```

---

### 2. Generate a New Service

```bash
dotnet new smartapi -n UserService
```

---

### 3. Build & Run

```bash
cd UserService
dotnet build
dotnet run --project UserService.API
```

---

### 4. Test API

Open:

```plaintext
http://localhost:5000/swagger
http://localhost:5000/health
```

---

# Running Tests

```bash
dotnet test
```

Tests include:

* API tests
* Authenticated endpoints (mocked auth)
* Integration test support

---

# Docker Usage

Each service includes a Dockerfile.

Build:

```bash
docker build -t your-service-name ./YourServiceName
```

---

# Template Maintenance

Before reinstalling template after changes:

```bash
dotnet new --debug:reinit
dotnet new install ./backend/templates/smart-api --force
```

---

# Important Notes

* Do NOT include `bin/` or `obj/` folders in template
* Always use `SmartService` (not `ServiceName`) in code
* JWT config must match across services
* TestAuthHandler bypasses authentication in tests (by design)

---

# Recommended Workflow

```plaintext
1. Generate service using template
2. Implement domain-specific logic
3. Add endpoints
4. Write tests
5. Dockerize
6. Deploy to Kubernetes
```

---

# Future Improvements


---
