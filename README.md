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
   git clone https://dev.azure.com/markyoxall65/Shadowland/_git/Shadow
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
**Repository**: [Azure DevOps - Shadowland](https://dev.azure.com/markyoxall65/Shadowland/_git/Shadow)  
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
