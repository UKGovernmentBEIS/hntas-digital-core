# HNTAS Digital Core API (`HNTAS.DIGITAL.CORE`)

## Overview
Backend API responsible for business logic, validation, and data persistence for the HNTAS platform.

---

## ⚙️ Technology Stack

| Layer | Technology |
| :--- | :--- |
| **Runtime** | .NET 10, C# |
| **API Framework** | ASP.NET Core Web API |
| **Database** | DocumentDB (MongoDB-compatible) |

## Running Locally

### Prerequisites
- .NET 10 SDK
- Docker Desktop installed


## Getting Started

Clone the repository and restore dependencies from the root directory:


```bash
git clone <repository-url>
cd hntas-digital-core/HNTAS.Digital.Core
dotnet restore
```

### Install Docker
Download and install Docker Desktop from:  
https://www.docker.com/products/docker-desktop/

Ensure Docker is running before continuing.

---

#### Pull MongoDB Image
Pull the official MongoDB image from Docker Hub:

```bash
docker pull mongo:latest
```


#### Start MongoDB Container
If you use Docker, start a local MongoDB container:

```bash
docker run --name hntas-mongo -p 27017:27017 -d mongo:latest
```

### Run API
```bash
cd HNTAS.Core.Api
dotnet run --launch-profile https
```

---

## API Endpoint
- Example: `https://localhost:7117`


