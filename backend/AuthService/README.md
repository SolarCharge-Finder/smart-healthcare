# 🔐 AuthService (Clean Architecture)

This service handles user authentication, registration, and role management for the Smart Healthcare system.

---

# 🧠 Overview

AuthService is built using **Clean Architecture** and follows a **layered microservice design**.

It is responsible for:
- User registration
- User login
- Role assignment (Patient, Doctor, Admin)
- Authentication logic (JWT integration upcoming)

---

# 🏗️ Architecture

This service follows **Clean Architecture principles**:

Controller (API)
   ↓
Application (Business Logic)
   ↓
Domain (Core Entities)
   ↓
Infrastructure (Database, EF Core)

---

# 📁 Project Structure

AuthService/
├── Auth.API                # Entry point (controllers, Program.cs)
│   ├── Controllers/
│   └── Program.cs
│
├── Auth.Application        # Business logic layer
│   ├── DTOs/
│   ├── Interfaces/
│   └── Services/
│
├── Auth.Domain             # Core domain models (no dependencies)
│   ├── Entities/
│   └── Enums/
│
├── Auth.Infrastructure     # External concerns (DB, repositories)
│   ├── Data/
│   └── Repositories/
│
└── Dockerfile              # Containerization setup

---

# 🔑 Key Concepts

## 🟦 API Layer (Auth.API)
- Handles HTTP requests
- Calls Application layer via interfaces
- No business logic here

---

## 🟩 Application Layer (Auth.Application)
- Contains business logic
- Uses interfaces (IAuthService, IUserRepository)
- Does NOT depend on Infrastructure

---

## 🟨 Domain Layer (Auth.Domain)
- Core models (User, UserRole)
- No external dependencies
- Pure C#

---

## 🟥 Infrastructure Layer (Auth.Infrastructure)
- Handles database (EF Core)
- Implements repositories
- Connects to PostgreSQL

---

# 🔄 Request Flow

Client → Controller → AuthService → UserRepository → Database

---

# ⚙️ Technologies Used

- .NET 8
- ASP.NET Core Web API
- Entity Framework Core
- PostgreSQL
- Docker
- Kubernetes

---

# 🐳 Docker Build

From project root:

docker build -t backend-auth-service:latest -f ./backend/AuthService/Dockerfile ./backend

---

# Migrations

dotnet ef migrations add InitialCreate --output-dir Data/Migrations --project backend/AuthService/Auth.Infrastructure --startup-project backend/AuthService/Auth.API

dotnet ef database update --project backend/AuthService/Auth.Infrastructure --startup-project backend/AuthService/Auth.API

dotnet ef database drop --project backend/AuthService/Auth.Infrastructure --startup-project backend/AuthService/Auth.API

---

# User Secrets - view  
dotnet user-secrets list --project backend/AuthService/Auth.API

---

# ☸️ Kubernetes Deployment

Restart service after rebuild:

kubectl rollout restart deployment auth-service

---

# 🔍 Debugging

Check pods:
kubectl get pods

View logs:
kubectl logs <pod-name>

Port forward (if needed):
kubectl port-forward svc/auth-service 5000:80

---

# 🌐 Access Swagger

http://localhost:30001/swagger/index.html

---

# 🛠️ Setup (Local Development)

1. Restore dependencies
dotnet restore

2. Build project
dotnet build

3. Run API
dotnet run --project backend/AuthService/Auth.API

---

# ⚠️ Important Notes

- Each layer only depends on allowed layers (Clean Architecture rule)
- Infrastructure should NEVER be referenced in Application
- API should only depend on Application

---

# 🚀 Future Improvements

- JWT token generation
- Role-based authorization
- Refresh tokens
- Integration with DoctorService & PatientService

---

# 🎯 Summary

This service demonstrates:
- Proper Clean Architecture implementation
- Microservice separation
- Containerized deployment with Docker
- Kubernetes orchestration
