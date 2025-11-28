# Azure Services Architecture

This diagram shows how the Azure services connect to each other when deployed.

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                              Azure Resource Group                            │
│                             (rg-expensemgmt-demo)                           │
│                                                                              │
│  ┌─────────────────────────────────────────────────────────────────────┐    │
│  │                     User Assigned Managed Identity                   │    │
│  │                      (mid-expensemgmt-xxxxx)                        │    │
│  │                                                                      │    │
│  │  • Assigned to App Service                                          │    │
│  │  • db_datareader, db_datawriter, EXECUTE on SQL Database            │    │
│  │  • Cognitive Services OpenAI User role on Azure OpenAI              │    │
│  │  • Search Index Data Contributor on AI Search                       │    │
│  └─────────────────────────────────────────────────────────────────────┘    │
│                                    │                                         │
│                                    │ Authenticates                          │
│                                    ▼                                         │
│  ┌─────────────────────────────────────────────────────────────────────┐    │
│  │                         App Service (Linux)                          │    │
│  │                      (app-expensemgmt-xxxxx)                        │    │
│  │                                                                      │    │
│  │  • ASP.NET 8.0 Razor Pages Application                              │    │
│  │  • Standard S1 SKU (always on, no cold start)                       │    │
│  │  • Expense Management UI                                            │    │
│  │  • REST API with Swagger                                            │    │
│  │  • Chat UI with AI Integration                                      │    │
│  └──────────────────┬───────────────────────────┬──────────────────────┘    │
│                     │                           │                            │
│                     │ Managed Identity          │ Managed Identity           │
│                     │ Connection                │ (optional)                 │
│                     ▼                           ▼                            │
│  ┌──────────────────────────────┐   ┌──────────────────────────────────┐   │
│  │      Azure SQL Database      │   │    Azure OpenAI (Sweden Central) │   │
│  │    (sql-expensemgmt-xxxxx)   │   │     (aoai-expensemgmt-xxxxx)     │   │
│  │                              │   │                                   │   │
│  │  • Database: Northwind       │   │  • GPT-4o Model Deployment       │   │
│  │  • Entra ID Auth Only        │   │  • S0 SKU                        │   │
│  │  • Basic Tier                │   │  • Function calling enabled      │   │
│  │  • Stored Procedures         │   │                                   │   │
│  │  • Expense Schema            │   │                                   │   │
│  └──────────────────────────────┘   └──────────────────────────────────┘   │
│                                                       │                      │
│                                                       │ RAG (future)        │
│                                                       ▼                      │
│                                     ┌──────────────────────────────────┐    │
│                                     │     Azure AI Search              │    │
│                                     │   (search-expensemgmt-xxxxx)     │    │
│                                     │                                   │    │
│                                     │  • Basic SKU                     │    │
│                                     │  • Document indexing             │    │
│                                     │                                   │    │
│                                     └──────────────────────────────────┘    │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘

                              │
                              │ HTTPS (Port 443)
                              │
                              ▼
                    ┌─────────────────────┐
                    │     End Users       │
                    │   (Web Browser)     │
                    └─────────────────────┘
```

## Data Flow

1. **User Access**: Users access the application via HTTPS through the App Service
2. **Authentication**: App Service uses its User Assigned Managed Identity to authenticate
3. **Database Operations**: The app calls stored procedures in Azure SQL using Managed Identity
4. **AI Chat** (optional): When GenAI is deployed, the chat uses Azure OpenAI for natural language processing
5. **Function Calling**: GPT-4o can execute functions to query/update expense data through the API

## Security Features

- **No Passwords**: All connections use Managed Identity (Azure AD authentication)
- **Entra ID Only**: SQL Server has Azure AD-only authentication enabled
- **HTTPS Only**: All traffic is encrypted
- **Minimal Permissions**: Each service has only the permissions it needs

## Deployment Options

1. **Basic Deployment** (`deploy.sh`):
   - App Service, SQL Database, Managed Identity
   - Chat UI shows demo responses

2. **Full Deployment** (`deploy-with-chat.sh`):
   - All of the above plus Azure OpenAI and AI Search
   - Chat UI uses GPT-4o with function calling
