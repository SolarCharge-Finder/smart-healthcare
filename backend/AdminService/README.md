# AdminService - Smart Healthcare System

## 📌 Overview
.

It handles:

---

## 🏗️ Architecture Overview

```


---

## 📂 Folder Breakdown

### .API


#### Responsibilities


### .Application
- Interfaces
- Services
- DTOs

#### Responsibilities


### .Domain
- Entities 

#### Responsibilities:


### .Infrastructure
handles external concerns like db access
- DbContext
- Repositories
- Migrations

#### Responsibilities:

---

## 🔄 Request Flow

```
Client → Controller → Service → Repository → Database
```

---

## ⚙️ Features


---

## 🐳 Docker

Build image:
```
docker build -t 
```

---

## ☸️ Kubernetes

Deploy:
```
kubectl apply -f 
kubectl apply -f 
```

Restart:
```
kubectl rollout restart deployment
```

Check pods:
```
kubectl get pods
```

---

## 🌐 Access

```
http://localhost:3000...
```

---

## 🗄️ Database

Port forward:
```
kubectl port-forward svc/postgres 5432:5432
```

Run migrations:
```
dotnet ef database update --project 
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
