# 🏥 AdminService - Smart Healthcare System

## 📌 Overview

**AdminService** is a microservice responsible for managing administrator accounts and moderation workflows within the Smart Healthcare System.

It handles:

* Admin registration (role request)
* Admin approval/rejection
* Viewing all admins
* Viewing pending admins
* Approving doctors (via DoctorService)
* Fetching pending doctors for approval

---

## 🏗️ Architecture Overview

```
AdminService
│
├── API Layer (Controllers)
│        ↓
├── Application Layer (Services, Interfaces, DTOs)
│        ↓
├── Domain Layer (Entities)
│        ↓
├── Infrastructure Layer (DB, Repositories, External Services)
│        ↓
└── PostgreSQL Database
```

---

## 📂 Folder Breakdown

### 📁 AdminService.API

* Controllers
* Middleware
* Dependency Injection setup

#### Responsibilities

* Handle HTTP requests
* Extract user identity (JWT)
* Route requests to Application layer
* Return HTTP responses

---

### 📁 AdminService.Application

* Interfaces
* Services
* DTOs

#### Responsibilities

* Business logic
* Validation and rules
* Communication with repositories
* Communication with external services via interfaces

---

### 📁 AdminService.Domain

* Entities (Admin)

#### Responsibilities:

* Core business models
* No dependencies on other layers

---

### 📁 AdminService.Infrastructure

* DbContext (AdminDbContext)
* Repositories
* Migrations
* External service clients (DoctorServiceClient)
* HTTP handlers (AuthHeaderHandler)

#### Responsibilities:

* Database access (EF Core)
* External HTTP communication
* Implementation of interfaces

---

## 🔄 Request Flow

```
Client → Controller → Service → Repository → Database
```

For external calls:

```
AdminService → DoctorService (via HttpClient)
```

---

## ⚙️ Features

* Create Admin (request role)
* Get all admins
* Get pending admins
* Approve admin
* Reject admin
* Get pending doctors (external service)
* Approve doctor (external service)
* JWT-based authentication & authorization
* Role-based access control

---

## 🐳 Docker

Build image:

```
docker build -t backend-admin-service:latest ./backend/AdminService
```

Run locally (optional):

```
docker run -p 8080:8080 backend-admin-service
```

---

## ☸️ Kubernetes

Deploy:

```
kubectl apply -f admin-deployment.yaml
kubectl apply -f admin-service.yaml
```

Restart:

```
kubectl rollout restart deployment admin-deployment
```

Check pods:

```
kubectl get pods
```

---

## 🌐 Access

Swagger UI:

```
http://localhost:30003/swagger
```

Health check:

```
http://localhost:30003/health
```

---

## 🗄️ Database

### ⚠️ Note

Migrations are automatically applied on startup using:

```
db.Database.Migrate();
```

---

### (Optional) Manual access

Port forward PostgreSQL:

```
kubectl port-forward svc/postgres 5432:5432
```

Run migrations manually:

```
dotnet ef database update \
--project backend/AdminService/AdminService.Infrastructure \
--startup-project backend/AdminService/AdminService.API
```

---

## 🧪 Testing

Run tests:

```
dotnet test
```

### Test Types

* Integration Tests (InMemory DB)
* PostgreSQL Integration Tests (Testcontainers)
* Mocked external services (DoctorService)

---

## 📌 Design Decisions

* Clean Architecture (layered separation)
* Repository Pattern for data access
* Service Layer for business logic
* DTOs for API communication
* HttpClient abstraction for inter-service communication
* JWT-based authentication
* Dependency Injection for testability

---

## 🚀 Summary

AdminService is a scalable, testable microservice built with:

* .NET 8
* Entity Framework Core
* PostgreSQL
* Docker
* Kubernetes
* Testcontainers
* Clean Architecture principles

It is designed to integrate seamlessly within a distributed microservices ecosystem.
