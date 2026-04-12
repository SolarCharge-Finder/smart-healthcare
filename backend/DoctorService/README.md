# DoctorService - Smart Healthcare System

## 📌 Overview
DoctorService is a microservice responsible for managing doctor profiles within the Smart Healthcare system.

It handles:
- Doctor registration
- Approval workflow (admin-controlled)
- Retrieval of doctor data

---

## 🏗️ Architecture Overview

```
DoctorService/
├── Doctor.API           → Entry point (Controllers, Middleware)
├── Doctor.Application   → Business logic (Services, Interfaces, DTOs)
├── Doctor.Domain        → Core entities (pure business models)
├── Doctor.Infrastructure→ Database + external implementations
```

---

## 📂 Folder Breakdown

### 🔹 Doctor.API
- Controllers (API endpoints)
- Middleware
- Extensions (service registration)
- Program.cs (application entry point)

#### Responsibilities
- Accept requests
- Return responses
- Delegate logic to application layer

### 🔹 Doctor.Application
- Interfaces
- Services
- DTOs

#### Responsibilities
- Enforce business rules
- Orchestrate operations
- Prevent invalid actions (e.g., duplicate doctor creation)

### 🔹 Doctor.Domain
- Entities (Doctor)

#### Responsibilities:

- Define structure of data
- No dependencies on other layers

### 🔹 Doctor.Infrastructure
handles external concerns like db access
- DbContext
- Repositories
- Migrations

#### Responsibilities:

- Communicate with PostgreSQL
- Implement interfaces from Application layer

---

## 🔄 Request Flow

```
Client → Controller → Service → Repository → Database
```

---

## ⚙️ Features

- Create doctor profile
- Get all doctors
- Get pending doctors
- Approve doctor

---
## Solution File Change

dotnet sln add backend/DoctorService/DoctorService.API/DoctorService.API.csproj
dotnet sln add backend/DoctorService/DoctorService.Application/DoctorService.Application.csproj
dotnet sln add backend/DoctorService/DoctorService.Domain/DoctorService.Domain.csproj
dotnet sln add backend/DoctorService/DoctorService.Infrastructure/DoctorService.Infrastructure.csproj

---

## 🐳 Docker

Build image:
```
docker build -t backend-doctor-service:latest ./backend/DoctorService
```

---

## ☸️ Kubernetes

Deploy:
```
kubectl apply -f doctor-deployment.yaml
kubectl apply -f doctor-service.yaml
```

Restart:
```
kubectl rollout restart deployment doctor-service
```

Check pods:
```
kubectl get pods
```

---

## 🌐 Access

```
http://localhost:30002/swagger
```

---

## 🗄️ Database

Port forward:
```
kubectl port-forward svc/postgres 5432:5432
```
## Migrations

```
dotnet ef migrations add InitialCreate --output-dir Data/Migrations --project backend/DoctorService/Doctor.Infrastructure --startup-project backend/DoctorService/Doctor.API

dotnet ef database update --project backend/DoctorService/Doctor.Infrastructure --startup-project backend/DoctorService/Doctor.API

dotnet ef database drop --project backend/DoctorService/Doctor.Infrastructure --startup-project backend/DoctorService/Doctor.API
```
---

## 📌 Design Decisions

- Clean Architecture
- Repository Pattern
- Service Layer
- DTO usage

---

## 🚀 Summary

DoctorService is a scalable microservice using:
- .NET 8
- PostgreSQL
- Docker
- Kubernetes

---
