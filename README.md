# Shadow - Modern .NET Portfolio Application

[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4)](https://dotnet.microsoft.com/)
[![Blazor](https://img.shields.io/badge/Blazor-WebAssembly-512BD4)](https://dotnet.microsoft.com/apps/aspnet/web-apps/blazor)
[![Azure Functions](https://img.shields.io/badge/Azure-Functions-0078D4)](https://azure.microsoft.com/services/functions/)
[![License](https://img.shields.io/badge/license-Portfolio-blue)](LICENSE)

> **Note**: This is a **work in progress** portfolio project showcasing modern .NET development practices, cloud integration, and full-stack web application architecture.

## 🎯 Overview

Shadow is a comprehensive full-stack .NET solution demonstrating enterprise-grade application development with modern technologies. This project serves as a technical portfolio showcasing expertise in:

- Modern web application architecture (BFF pattern)
- Blazor WebAssembly with interactive UI
- Serverless computing with Azure Functions
- Identity management and authentication
- State management with Fluxor (Redux pattern)
- Cloud integration (Azure Storage, SendGrid)
- RESTful API development

---

## 🏗️ Solution Architecture

```
Shadow/
├── Shadow.BlazorSpa.Client/      # Blazor WebAssembly frontend
├── Shadow.BlazorSpa.Bff/         # Backend-for-Frontend (BFF) API
├── Shadow.Identity/              # Identity and authentication services
├── Shadow.FastEndpoints/         # FastEndpoints API implementation
└── Shadow.FunkyGibbon/           # Azure Functions serverless backend
```

### Project Breakdown

#### 🎨 Shadow.BlazorSpa (Blazor WebAssembly + BFF)

**Client Project** - Interactive web application using Blazor WebAssembly
- **Tech Stack**: Blazor WebAssembly, C# 14, .NET 10
- **Features**:
  - Interactive weather forecasting UI
  - State management with **Fluxor** (Redux pattern)
  - Persistent state management across sessions
  - Responsive design with modern UI patterns
  - Real-time Azure Function integration

**BFF Project** - Backend-for-Frontend API layer
- **Pattern**: Backend-for-Frontend (BFF) architecture
- **Purpose**: Provides secure, optimized API gateway for the Blazor client
- **Tech Stack**: ASP.NET Core, Duende.Bff
- **Security**: Authentication state management, secure API proxying

#### 🔐 Shadow.Identity

Identity and authentication infrastructure
- User authentication and authorization
- Identity management services
- ASP.NET Core Identity integration

#### ⚡ Shadow.FastEndpoints

RESTful API implementation using FastEndpoints
- **Tech Stack**: FastEndpoints, ASP.NET Core
- **Pattern**: REPR (Request-Endpoint-Response) pattern
- **Benefits**: Performance-optimized, minimal API boilerplate

Note on APIs and databases:
- The solution exposes APIs in multiple places:
  - `Shadow.BlazorSpa.Bff` contains BFF-specific endpoints and proxy controllers (for example the `NotesProxyController`) intended to provide a secure, frontend-focused API surface and to host server-side rendering features. The BFF proxies requests to downstream APIs and is the recommended entry point for the Blazor client when authentication is required.
  - `Shadow.FastEndpoints` contains the primary REST API surface (for example `NotesEndpoint`) and uses Orleans grains for state management in the notes sample. This project is intended as the main backend API that can be consumed by the BFF or other services.
- The solution uses two database technologies by design:
  - `Shadow.Identity` (Identity) uses a SQL-compatible EF Core store (configure via `ConnectionStrings:DefaultConnection` in that project). This typically maps to a SQL Server instance for ASP.NET Identity data.
  - `Shadow.FastEndpoints` uses PostgreSQL for application data (configure via `ConnectionStrings:Postgres`). The FastEndpoints `ApplicationDbContext` is registered to use Npgsql when the Postgres connection string is present.

When running locally, ensure both connection strings are configured (or adjust projects to use a single DB) and that the Postgres server is reachable when running the FastEndpoints project. The BFF typically proxies authenticated calls to the FastEndpoints API rather than querying databases directly.

#### ☁️ Shadow.FunkyGibbon (Azure Functions)

Serverless backend for asynchronous operations
- **Tech Stack**: Azure Functions (.NET 10 Isolated)
- **Features**:
  - HTTP-triggered functions for event logging
  - Azure Blob Storage integration
  - SendGrid email notifications
  - Resilient error handling and logging
- **Use Case**: Weather forecast tracking with email notifications and blob persistence

---

## 🚀 Technologies & Patterns

### Frontend
- **Blazor WebAssembly** - C# in the browser, no JavaScript
- **Fluxor** - Redux-style state management for .NET
- **Persistent State** - Cross-session data persistence
- **Dependency Injection** - Clean, testable architecture

### Backend
- **ASP.NET Core** - Modern, cross-platform web framework
- **FastEndpoints** - Performance-focused API framework
- **Azure Functions** - Serverless compute for event-driven architecture
- **BFF Pattern** - Security and optimization layer for SPA

### Cloud & Infrastructure
- **Azure Functions** - Serverless compute
- **Azure Blob Storage** - Cloud storage for data persistence
- **SendGrid** - Email delivery service
- **Azure Pipelines** - Automated CI/CD with multi-stage deployments
- **GitHub Integration** - Webhook-triggered builds on every commit
- **CORS** - Secure cross-origin resource sharing

### Architectural Patterns
- **Backend-for-Frontend (BFF)** - Dedicated API gateway for frontend
- **REPR (Request-Endpoint-Response)** - Minimal API pattern
- **Redux/Flux** - Predictable state management
- **Repository Pattern** - Data access abstraction
- **Dependency Injection** - Loose coupling and testability

---

## 🛠️ Setup & Development

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Visual Studio 2025](https://visualstudio.microsoft.com/) or [VS Code](https://code.visualstudio.com/)
- [Azure Functions Core Tools](https://docs.microsoft.com/azure/azure-functions/functions-run-local)
- Azure subscription (for cloud deployment)

### Local Development

1. **Clone the repository**
   ```bash
   git clone https://github.com/markyoxall/Shadow.git
   cd Shadow
   ```

2. **Restore dependencies**
   ```bash
   dotnet restore
   ```

3. **Run the Blazor application**
   ```bash
   cd Shadow.BlazorSpa/Shadow.BlazorSpa.Bff
   dotnet run
   ```
   Navigate to: `https://localhost:7035`

4. **Run Azure Functions locally** (optional)
   ```bash
   cd Shadow.FunkyGibbon
   func start
   ```

### Configuration

#### Blazor Client Configuration
Client-side settings are in `Shadow.BlazorSpa.Client/wwwroot/appsettings.json`:
```json
{
  "AzureFunction": {
    "WalkthroughUrl": "https://your-function-app.azurewebsites.net/api/walkthrough"
  }
}
```

#### Azure Function Configuration (local)
Create `local.settings.json` in `Shadow.FunkyGibbon/` (not committed to Git):
```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "your-storage-connection-string",
    "BLOB_CONTAINER": "walkthrough",
    "SENDGRID_API_KEY": "your-sendgrid-api-key",
    "EMAIL_TO": "recipient@example.com",
    "EMAIL_FROM": "sender@example.com"
  }
}
```

Note: local vs deployed configuration
- For deployment to Azure you must set `AzureWebJobsStorage` (the full storage account connection string) and any other secrets in the Function App's Application settings (Azure Portal → Function App → Configuration → Application settings). These values will be used by the Function at runtime in Azure.
- For local development you have two common options:
  1. Use Azurite (recommended) — a local storage emulator. Set `AzureWebJobsStorage` to `UseDevelopmentStorage=true` (or use an explicit Azurite connection string) in `local.settings.json` or `appsettings.local.json`.
  2. Point to a real Azure Storage account by placing its connection string in `local.settings.json` under `AzureWebJobsStorage` (useful when you need to test against real storage).

Examples and tips
- Using Azurite with the shorthand connection string (recommended):
  - Start Azurite (choose one):
    - npm: `npm i -g azurite` then run `azurite` in a terminal.
    - Docker: `docker run -p 10000:10000 -p 10001:10001 -p 10002:10002 mcr.microsoft.com/azure-storage/azurite`
  - In `local.settings.json` or `appsettings.local.json` set:
    - `"AzureWebJobsStorage": "UseDevelopmentStorage=true"`
  - The SDK will route blob operations to the local emulator ports (10000+).

- Using an explicit Azurite connection string (optional):
  - When required by tooling, use the explicit connection string below (Azurite defaults):
    - `DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;AccountKey=Eby8vdM02xNOcqFlqUwJ...;BlobEndpoint=http://127.0.0.1:10000/devstoreaccount1;`
  - Paste that into `AzureWebJobsStorage` in your local settings.

- Using a real Azure Storage account for local testing:
  - In Azure Portal → Storage Account → Access keys → copy the "Connection string" value and paste it into `local.settings.json` under `AzureWebJobsStorage`.

Where to put local settings
- `local.settings.json` (Functions Core Tools format) is the simplest choice for running locally with `func start`. This file is already ignored by `.gitignore`.
- Alternatively you can create `Shadow.FunkyGibbon/appsettings.local.json` with a `Walkthrough` section. This project reads `Walkthrough:StorageConnection` and falls back to `AzureWebJobsStorage` if the Walkthrough section is missing.

Verification
- Start Azurite, run the function (`func start`), invoke the walkthrough endpoint and inspect the Azurite UI or connect with Azure Storage Explorer to confirm blobs are being uploaded.


---

## 📦 Azure Deployment

### Azure Functions Deployment

1. **Create Azure Function App**
   ```bash
   az functionapp create --resource-group YourResourceGroup \
     --consumption-plan-location ukwest \
     --runtime dotnet-isolated \
     --runtime-version 10 \
     --functions-version 4 \
     --name your-function-app \
     --storage-account yourstorageaccount
   ```

2. **Configure environment variables** in Azure Portal:
   - `AzureWebJobsStorage`
   - `SENDGRID_API_KEY`
   - `EMAIL_TO`, `EMAIL_FROM`
   - `BLOB_CONTAINER`

3. **Deploy from Visual Studio**:
   - Right-click `Shadow.FunkyGibbon` → Publish
   - Select Azure Function App
   - Publish

4. **Enable CORS** in Azure Portal:
   - Azure Function App → CORS
   - Add allowed origins (e.g., `https://localhost:7035`, production URL)

---

## 🎓 Key Learning Demonstrations

This project demonstrates proficiency in:

### Architecture & Design
- ✅ **BFF Pattern** - Secure, optimized API gateway for SPAs
- ✅ **Serverless Architecture** - Event-driven design with Azure Functions
- ✅ **State Management** - Redux/Flux pattern implementation
- ✅ **Separation of Concerns** - Clean architecture principles

### Cloud & DevOps
- ✅ **Azure Integration** - Functions, Storage, SendGrid
- ✅ **CORS Configuration** - Secure cross-origin communication
- ✅ **Environment Management** - Local vs. production configuration
- ✅ **Secret Management** - Azure environment variables, .gitignore patterns

### .NET Expertise
- ✅ **.NET 10** - Latest framework features
- ✅ **C# 14** - Modern language features (primary constructors, file-scoped namespaces)
- ✅ **Async/Await** - Fire-and-forget patterns, non-blocking operations
- ✅ **Dependency Injection** - Constructor injection, service registration
- ✅ **Logging & Monitoring** - Structured logging with ILogger

### Best Practices
- ✅ **Error Handling** - Try-catch, resilient operations
- ✅ **Logging** - Application Insights integration
- ✅ **Security** - No secrets in source control, CORS policies
- ✅ **Code Organization** - Clean, maintainable project structure

---

## 📈 Roadmap

### Current Features
- ✅ Blazor WebAssembly SPA with interactive UI
- ✅ Weather forecast functionality
- ✅ Azure Functions integration for event tracking
- ✅ Blob storage persistence
- ✅ Email notifications via SendGrid
- ✅ State management with Fluxor

### Planned Features
- 🔄 User authentication and authorization
- 🔄 Additional API endpoints
- 🔄 Enhanced UI/UX design
- 🔄 Unit and integration tests
- 🔄 CI/CD pipeline
- 🔄 Production deployment to Azure
- 🔄 Performance monitoring and analytics

---

## 📞 Contact

**Developer**: Mark Yoxall  
**Repository**: [GitHub - Shadow](https://github.com/markyoxall/Shadow)  
**Email**: mark.yoxall65@gmail.com

---

## 📄 License

This project is for portfolio demonstration purposes.

---

## 🙏 Acknowledgments

This project uses the following technologies and frameworks:
- [.NET](https://dotnet.microsoft.com/)
- [Blazor](https://dotnet.microsoft.com/apps/aspnet/web-apps/blazor)
- [Azure Functions](https://azure.microsoft.com/services/functions/)
- [Fluxor](https://github.com/mrpmorris/Fluxor)
- [FastEndpoints](https://fast-endpoints.com/)
- [Duende.Bff](https://docs.duendesoftware.com/identityserver/v6/bff/)
- [SendGrid](https://sendgrid.com/)

---

*Last Updated: February 2026*
