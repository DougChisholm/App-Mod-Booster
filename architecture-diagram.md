# Azure Services Architecture Diagram

This diagram shows the resources created by running `deploy-with-chat.sh` and how they connect.

```
┌─────────────────────────────────────────────────────────────────────────────────┐
│                           Azure Resource Group (uksouth)                        │
│                                                                                 │
│   ┌──────────────────────────────────────────────────────┐                     │
│   │              App Service (S1, UKSOUTH)               │                     │
│   │          app-expensemgmt-<unique>                    │                     │
│   │                                                      │                     │
│   │  ┌────────────────────────────────────────────────┐  │                     │
│   │  │  ASP.NET Core 8 – Expense Management App       │  │                     │
│   │  │  • Razor Pages UI (Dashboard, Add, Approve)    │  │                     │
│   │  │  • REST API (/api/expenses, /api/chat, …)      │  │                     │
│   │  │  • Swagger UI (/swagger)                       │  │                     │
│   │  │  • Chat UI (/Chat)                             │  │                     │
│   │  └────────────────────────────────────────────────┘  │                     │
│   │                  │ Uses Managed Identity             │                     │
│   └──────────────────┼───────────────────────────────────┘                     │
│                      │                                                          │
│   ┌──────────────────▼─────────────────┐                                       │
│   │  User-Assigned Managed Identity    │                                       │
│   │  mid-AppModAssist-09-14-31         │                                       │
│   │  (no passwords – secure by default)│                                       │
│   └───────┬──────────────────┬─────────┘                                       │
│           │                  │                                                  │
│           ▼                  ▼                                                  │
│   ┌───────────────┐  ┌────────────────────────────────────────────────────┐    │
│   │ Azure SQL DB  │  │          Azure OpenAI (swedencentral)              │    │
│   │ (uksouth)     │  │          aoai-expensemgmt-<unique>                 │    │
│   │               │  │                                                    │    │
│   │ Server:       │  │  Model: GPT-4o (gpt-4o deployment)                 │    │
│   │ sql-expense…  │  │  SKU: S0, Capacity: 8                              │    │
│   │               │  │  Auth: Managed Identity (no API keys)              │    │
│   │ Database:     │  │  Role: Cognitive Services OpenAI User              │    │
│   │ Northwind     │  └────────────────────────────────────────────────────┘    │
│   │               │                                                             │
│   │ Auth: Entra   │  ┌────────────────────────────────────────────────────┐    │
│   │ ID only (no   │  │          Azure AI Search (uksouth)                 │    │
│   │ SQL auth)     │  │          search-expensemgmt-<unique>               │    │
│   │               │  │                                                    │    │
│   │ Roles:        │  │  SKU: Basic (low cost)                             │    │
│   │ db_datareader │  │  Auth: Managed Identity                            │    │
│   │ db_datawriter │  │  Role: Search Index Data Reader                    │    │
│   │ EXECUTE       │  └────────────────────────────────────────────────────┘    │
│   └───────────────┘                                                             │
└─────────────────────────────────────────────────────────────────────────────────┘

       ▲
       │  HTTPS
       │
  ┌────┴──────┐
  │   User    │  Browser → https://app-expensemgmt-<unique>.azurewebsites.net/Index
  │ (Browser) │
  └───────────┘
```

## Data Flow

### Web UI → Database
```
Browser → App Service (Razor Pages) → ExpenseService → SQL Stored Procedures → Azure SQL
```

### Web UI → AI Chat
```
Browser → App Service (/Chat page) → ChatController → ChatService
    → Azure OpenAI (GPT-4o) [function calling]
    → ExpenseService → Azure SQL
    → Azure OpenAI (final response)
    → Browser
```

### Authentication Flow
```
App Service uses User-Assigned Managed Identity (mid-AppModAssist-09-14-31)
    → Azure SQL:   Active Directory Managed Identity connection string
    → Azure OpenAI: ManagedIdentityCredential with explicit client ID
    → No passwords, connection strings with secrets, or API keys stored anywhere
```

## Deployment Order

When running `deploy.sh` or `deploy-with-chat.sh`:

1. **Resource Group** – created in uksouth
2. **App Service Plan** (S1) + **App Service** + **Managed Identity** – deployed together
3. **Azure SQL Server + Northwind DB** – Entra ID only, MI granted ##MS_DatabaseManager##
4. **Azure OpenAI** (swedencentral) + **AI Search** (uksouth) – only in `deploy-with-chat.sh`
5. **App Service settings** – connection string + OpenAI endpoint configured post-deployment
6. **SQL Firewall** – current IP + Azure services allowed
7. **Python: database schema import** – `run-sql.py`
8. **Python: managed identity DB roles** – `run-sql-dbrole.py`
9. **Python: stored procedures** – `run-sql-stored-procs.py`
10. **App zip deploy** – `az webapp deploy` with flat zip (no intermediate folder)

## Resource Naming

All resources use `uniqueString(resourceGroup().id)` for uniqueness (no timestamps):

| Resource            | Name Pattern                              | Region        |
|---------------------|-------------------------------------------|---------------|
| App Service Plan    | `asp-expensemgmt-<unique>`                | uksouth       |
| App Service         | `app-expensemgmt-<unique>`                | uksouth       |
| Managed Identity    | `mid-AppModAssist-09-14-31`               | uksouth       |
| SQL Server          | `sql-expensemgmt-<unique>`                | uksouth       |
| SQL Database        | `Northwind`                               | uksouth       |
| Azure OpenAI        | `aoai-expensemgmt-<unique>` (lowercase)   | swedencentral |
| AI Search           | `search-expensemgmt-<unique>` (lowercase) | uksouth       |
