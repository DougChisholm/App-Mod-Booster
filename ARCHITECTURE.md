# Azure Services Architecture

This diagram shows the Azure resources created by this solution and how they connect.

## Architecture Diagram

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                           Azure Resource Group                               │
│                          (rg-expensemgmt-demo)                              │
│                                                                             │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │                    User-Assigned Managed Identity                    │   │
│  │                      (mid-expensemgmt-xxx)                          │   │
│  │                                                                      │   │
│  │  Used by all services for secure authentication without passwords    │   │
│  └──────────────────────────────┬───────────────────────────────────────┘   │
│                                 │                                            │
│         ┌───────────────────────┼───────────────────────┐                   │
│         │                       │                       │                   │
│         ▼                       ▼                       ▼                   │
│  ┌─────────────────┐   ┌─────────────────┐   ┌─────────────────┐           │
│  │   App Service   │   │   Azure SQL     │   │  Azure OpenAI   │           │
│  │   (S1 Plan)     │   │   (Basic)       │   │  (S0 SKU)       │           │
│  │                 │   │                 │   │                 │           │
│  │  ┌───────────┐  │   │  ┌───────────┐  │   │  ┌───────────┐  │           │
│  │  │ ASP.NET   │  │──▶│  │ Northwind │  │   │  │  GPT-4o   │  │           │
│  │  │ Razor App │  │   │  │ Database  │  │   │  │  Model    │  │           │
│  │  └───────────┘  │   │  └───────────┘  │   │  └───────────┘  │           │
│  │                 │   │                 │   │                 │           │
│  │  - Dashboard    │   │  - Expenses     │   │  - Chat UI      │           │
│  │  - Expenses     │   │  - Users        │   │  - Function     │           │
│  │  - Add Expense  │   │  - Categories   │   │    Calling      │           │
│  │  - Approve      │   │  - Statuses     │   │                 │           │
│  │  - Chat UI      │   │  - Stored Procs │   │  Location:      │           │
│  │  - REST APIs    │   │                 │   │  Sweden Central │           │
│  │                 │   │  Entra ID Auth  │   │                 │           │
│  │  Location:      │   │  Only           │   │                 │           │
│  │  UK South       │   │                 │   │                 │           │
│  └────────┬────────┘   └─────────────────┘   └────────┬────────┘           │
│           │                                           │                     │
│           │         ┌─────────────────┐               │                     │
│           │         │  Azure AI       │               │                     │
│           └────────▶│  Search         │◀──────────────┘                     │
│                     │  (Basic SKU)    │                                     │
│                     │                 │                                     │
│                     │  RAG Support    │                                     │
│                     │  (Optional)     │                                     │
│                     │                 │                                     │
│                     │  Location:      │                                     │
│                     │  Sweden Central │                                     │
│                     └─────────────────┘                                     │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

## Data Flow

### 1. Web Application Flow
```
User Browser ──▶ App Service ──▶ Azure SQL Database
                     │
                     └──▶ Managed Identity (Authentication)
```

### 2. Chat AI Flow
```
User Browser ──▶ App Service ──▶ Azure OpenAI (GPT-4o)
                     │                │
                     │                ▼
                     │         Function Calling
                     │                │
                     └────────────────┼──▶ Azure SQL (via APIs)
                                      │
                                      └──▶ AI Search (RAG context)
```

## Security Model

- **No passwords stored**: All authentication uses Managed Identity
- **Entra ID Only**: SQL Server configured for Azure AD authentication only
- **RBAC**: Managed Identity granted only necessary database roles
- **TLS 1.2**: Minimum TLS version enforced on all services
- **HTTPS Only**: App Service configured for HTTPS only

## Resource SKUs

| Resource | SKU | Purpose |
|----------|-----|---------|
| App Service Plan | S1 Standard | Avoids cold start, always-on |
| Azure SQL Database | Basic | Development/POC tier |
| Azure OpenAI | S0 | Standard tier with GPT-4o |
| Azure AI Search | Basic | Development/POC tier |

## Deployment Options

1. **Without GenAI** (`deployGenAI=false`):
   - App Service + SQL only
   - Chat UI shows demo mode message

2. **With GenAI** (`deployGenAI=true`):
   - Full functionality including AI chat
   - Requires Azure OpenAI access
