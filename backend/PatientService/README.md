# PatientService - Smart Healthcare System

## 📌 Overview

PatientService is responsible for managing patient-related data within the Smart Healthcare System.

It handles:
- Patient profile creation and management
- Linking patients with authenticated users
- Retrieving patient information
- Soft deletion (deactivation) of patients
- Integration with AuthService to assign roles

---

## 🏗️ Architecture Overview

```
Client → Patient API → Application Layer → Domain → Infrastructure → Database
↓
Auth Service (role assignment)
```
---

## 📂 Folder Breakdown

### .API
- Controllers
- Middleware / Config

#### Responsibilities
- Expose REST endpoints
- Handle HTTP requests/responses
- Validate input and call application services
- Enforce authorization

---

### .Application
- Interfaces
- Services
- DTOs

#### Responsibilities
- Business logic implementation
- Coordinate between repositories and external services
- Handle use cases (create, update, deactivate patient)
- Call AuthService to assign Patient role

---

### .Domain
- Entities 

#### Responsibilities:
- Core business models (Patient)
- Domain-level properties and rules
- No external dependencies

---

### .Infrastructure
handles external concerns like db access
- DbContext
- Repositories
- Migrations
- External service clients (AuthService client)

#### Responsibilities:
- Database access using EF Core
- Repository implementations
- Communication with AuthService
- Persisting and retrieving data

---

## 🔄 Request Flow

```
Client → Controller → Service → Repository → Database
↓
Auth Service
```

---

## ⚙️ Features

- Create patient profile linked to userId
- Prevent duplicate patient creation for same user
- Assign Patient role via AuthService
- Get patient by ID or userId
- Update patient details
- Soft delete (deactivate patient)
- Hard delete patient

---

## Solution file
dotnet sln add backend/DoctorService/DoctorService.API/DoctorService.API.csproj
dotnet sln add backend/DoctorService/DoctorService.Application/DoctorService.Application.csproj
dotnet sln add backend/DoctorService/DoctorService.Domain/DoctorService.Domain.csproj
dotnet sln add backend/DoctorService/DoctorService.Infrastructure/DoctorService.Infrastructure.csproj

---

## 🐳 Docker

Build image:
```
docker build -t backend-patient-service:latest -f ./backend/PatientService/Dockerfile ./backend
```

---

## ☸️ Kubernetes

Deploy:
```
kubectl apply -f patient-deployment.yaml
kubectl apply -f patient-service.yaml
```

Restart:
```
kubectl rollout restart deployment patient-service
```

Check pods:
```
kubectl get pods
```

---

## 🌐 Access

```
http://localhost:30004/swagger
```

---

## 🗄️ Database

Port forward:
```
kubectl port-forward svc/postgres 5432:5432
```

## Migratinos
```
dotnet ef migrations add InitialCreate --output-dir Data/Migrations --project backend/PatientService/PatientService.Infrastructure --startup-project backend/PatientService/PatientService.API

dotnet ef database update --project backend/PatientService/PatientService.Infrastructure --startup-project backend/PatientService/PatientService.API

dotnet ef database drop --project backend/PatientService/PatientService.Infrastructure --startup-project backend/PatientService/PatientService.API
```

---

## 📌 Design Decisions

- Clean Architecture
- Repository Pattern
- Service Layer
- DTO usage
- External service communication via contracts (AuthService)

---

## 🚀 Summary

PatientService is a scalable microservice using:
- .NET 8
- PostgreSQL
- Docker
- Kubernetes
- Inter-service communication with AuthService

---
