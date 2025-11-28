![Header image](https://github.com/DougChisholm/App-Mod-Booster/blob/main/repo-header-booster.png)

# App-Mod-Booster - Expense Management System

A project to show how GitHub coding agent can turn screenshots of a legacy app into a working proof-of-concept for a cloud native Azure replacement if the legacy database schema is also provided.

## Generated Application

This repository contains a modernized **Expense Management System** with:

- **ASP.NET 8.0 Razor Pages** web application
- **REST API** with Swagger documentation
- **Azure SQL Database** with Entra ID (Azure AD) authentication only
- **User Assigned Managed Identity** for secure, passwordless connections
- **AI Chat Assistant** powered by Azure OpenAI (optional)
- **Modern responsive UI** with Bootstrap 5

### Features

| Feature | Description |
|---------|-------------|
| 📋 **View Expenses** | List all expenses with filtering and search |
| ➕ **Add Expense** | Create new expense claims with amount, date, category, and description |
| ✏️ **Edit Expense** | Update draft expenses before submission |
| ✅ **Submit for Approval** | Submit expenses for manager review |
| ✔️ **Approve/Reject** | Managers can approve or reject pending expenses |
| 💬 **AI Chat** | Natural language interface to query and manage expenses |
| 📊 **API Access** | RESTful API with Swagger documentation at `/swagger` |

## Quick Start

### Prerequisites

- [Azure CLI](https://docs.microsoft.com/cli/azure/install-azure-cli) installed
- [Python 3](https://www.python.org/downloads/) installed
- An Azure subscription with permissions to create resources
- VS Code or terminal access

### Deployment Steps

1. **Fork or clone this repository**

2. **Login to Azure**
   ```bash
   az login
   ```

3. **Deploy the infrastructure and application**

   **Basic deployment (without AI chat):**
   ```bash
   chmod +x deploy.sh
   ./deploy.sh
   ```

   **Full deployment (with Azure OpenAI for AI chat):**
   ```bash
   chmod +x deploy-with-chat.sh
   ./deploy-with-chat.sh
   ```

4. **Access the application**
   
   The deployment script will output the application URL. Navigate to:
   ```
   https://<app-name>.azurewebsites.net/Index
   ```

### Running Locally

1. **Update the connection string** in `src/ExpenseManagement/appsettings.json`:
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Server=tcp:<your-server>.database.windows.net,1433;Database=Northwind;Authentication=Active Directory Default;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"
     }
   }
   ```

2. **Login with Azure CLI**:
   ```bash
   az login
   ```

3. **Run the application**:
   ```bash
   cd src/ExpenseManagement
   dotnet run
   ```

## Architecture

See [ARCHITECTURE.md](ARCHITECTURE.md) for a detailed diagram of the Azure services and their connections.

### Azure Resources Created

| Resource | Description |
|----------|-------------|
| **Resource Group** | Container for all resources |
| **App Service (Linux)** | Hosts the ASP.NET 8.0 application |
| **App Service Plan (S1)** | Standard tier to avoid cold starts |
| **User Assigned Managed Identity** | Passwordless authentication to Azure services |
| **Azure SQL Server** | Entra ID-only authentication |
| **Azure SQL Database** | Northwind database for expense data |
| **Azure OpenAI** *(optional)* | GPT-4o model for AI chat |
| **Azure AI Search** *(optional)* | For future RAG capabilities |

## API Documentation

The application includes a full REST API with Swagger documentation:

- **Swagger UI**: `https://<app-name>.azurewebsites.net/swagger`

### Available Endpoints

| Endpoint | Method | Description |
|----------|--------|-------------|
| `/api/expenses` | GET | Get all expenses |
| `/api/expenses/{id}` | GET | Get expense by ID |
| `/api/expenses` | POST | Create new expense |
| `/api/expenses/{id}` | PUT | Update expense |
| `/api/expenses/{id}/submit` | POST | Submit for approval |
| `/api/expenses/{id}/approve` | POST | Approve expense |
| `/api/expenses/{id}/reject` | POST | Reject expense |
| `/api/expenses/pending` | GET | Get pending expenses |
| `/api/expenses/search` | GET | Search expenses |
| `/api/categories` | GET | Get all categories |
| `/api/users` | GET | Get all users |
| `/api/chat` | POST | Send message to AI assistant |

## File Structure

```
├── infrastructure/          # Bicep IaC templates
│   ├── main.bicep          # Main deployment template
│   ├── app-service.bicep   # App Service configuration
│   ├── azure-sql.bicep     # SQL Server & Database
│   ├── managed-identity.bicep  # User Assigned Identity
│   └── genai.bicep         # Azure OpenAI & AI Search
├── scripts/                 # Python deployment scripts
│   ├── run-sql.py          # Import database schema
│   ├── run-sql-dbrole.py   # Configure identity permissions
│   ├── run-sql-stored-procs.py  # Create stored procedures
│   └── stored-procedures.sql    # SQL stored procedures
├── src/ExpenseManagement/   # ASP.NET application
│   ├── Controllers/        # API controllers
│   ├── Models/            # Data models
│   ├── Pages/             # Razor pages
│   ├── Services/          # Business logic
│   └── wwwroot/           # Static files
├── Database-Schema/         # SQL schema files
├── Legacy-Screenshots/      # Original app screenshots
├── deploy.sh               # Basic deployment script
├── deploy-with-chat.sh     # Full deployment with AI
└── app.zip                 # Pre-built application package
```

## Security

This application follows Azure best practices for security:

- **No passwords stored** - Uses Managed Identity for all Azure service connections
- **Entra ID-only authentication** - SQL Server requires Azure AD authentication
- **HTTPS only** - All traffic is encrypted
- **Minimal permissions** - Each identity has only the permissions it needs

## Modernization Process

This project demonstrates how to modernize a legacy application:

1. **Original screenshots** in `Legacy-Screenshots/` show the legacy Windows Forms app
2. **Database schema** in `Database-Schema/` defines the data structure
3. **GitHub Copilot coding agent** generates the modernized application
4. **Infrastructure as Code** (Bicep) provisions Azure resources
5. **Deployment scripts** automate the entire setup

## Contributing

To test changes to the prompts:

1. Fork this repository
2. Modify the prompts in the `prompts/` folder
3. Run the GitHub coding agent with "modernise my app"
4. Review the generated code

See `Guiding-Principles/` for more information about contributing.

## Support

For Microsoft employees, see the supporting slides:
[Internal SharePoint Link](https://microsofteur-my.sharepoint.com/:p:/g/personal/dchisholm_microsoft_com/IQAY41LQ12fjSIfFz3ha4hfFAZc7JQQuWaOrF7ObgxRK6f4?e=p6arJs)
