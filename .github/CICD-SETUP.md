# GitHub Actions CI/CD Setup Guide

This guide explains how to configure GitHub Actions for fully automated deployment of the Expense Management System to Azure using OIDC (OpenID Connect) federation - no secrets required.

## What's Manual vs Automatic?

| Component | Who Creates It | When |
|-----------|---------------|------|
| **Service Principal** | Manual (you) | One-time setup per Azure subscription |
| **Azure Role Assignments** | Manual (you) | One-time setup per subscription |
| **Federated Credentials** | Manual (you) | One-time setup per repository/branch |
| **GitHub Repository Variables** | Manual (you) | One-time setup per repository |
| **GitHub Environment** | Manual (you) | One-time setup per repository |
| `.github/workflows/deploy.yml` | Agent (rebuild) | Generated from prompt-028 |
| `deploy-infra/deploy.ps1` | Agent (rebuild) | Generated with CI/CD detection logic |
| `deploy-infra/modules/*.bicep` | Agent (rebuild) | Generated with adminPrincipalType parameter |
| `.github/CICD-SETUP.md` | Agent (rebuild) | This documentation file |

### Summary
- **You do once:** Create Service Principal, assign roles, configure GitHub repo
- **Agent generates:** All workflow files, scripts, and Bicep templates with CI/CD support

## Prerequisites

- Azure subscription with Owner or Contributor + User Access Administrator permissions
- GitHub repository with Actions enabled
- Azure CLI installed locally for initial setup

## Step 1: Create Service Principal with OIDC Federation

Run these commands from a **PowerShell** terminal:

```powershell
# Set variables
$GITHUB_ORG = "your-github-username-or-org"
$GITHUB_REPO = "App-Mod-Boost"
$SUBSCRIPTION_ID = "your-azure-subscription-id"
$APP_NAME = "sp-expense-mgmt-cicd"

# Login to Azure
az login

# Create Service Principal (without secret - we'll use OIDC)
az ad app create --display-name $APP_NAME

# Get the Application (Client) ID
$APP_ID = az ad app list --display-name $APP_NAME --query "[0].appId" -o tsv

# Create Service Principal for the application
az ad sp create --id $APP_ID

# Get the Service Principal Object ID (needed for SQL admin)
$SP_OBJECT_ID = az ad sp show --id $APP_ID --query "id" -o tsv

# Assign Contributor role to the subscription
az role assignment create `
    --assignee $APP_ID `
    --role "Contributor" `
    --scope "/subscriptions/$SUBSCRIPTION_ID"

# Assign User Access Administrator role (needed to create role assignments for Managed Identity)
az role assignment create `
    --assignee $APP_ID `
    --role "User Access Administrator" `
    --scope "/subscriptions/$SUBSCRIPTION_ID"

# Create federated credential for GitHub Actions - main branch
az ad app federated-credential create --id $APP_ID --parameters '{\"name\":\"github-actions-main\",\"issuer\":\"https://token.actions.githubusercontent.com\",\"subject\":\"repo:chrisdoofer/App-Mod-Boost:ref:refs/heads/main\",\"audiences\":[\"api://AzureADTokenExchange\"]}'

# Create federated credential for GitHub Actions - production environment
az ad app federated-credential create --id $APP_ID --parameters '{\"name\":\"github-actions-production\",\"issuer\":\"https://token.actions.githubusercontent.com\",\"subject\":\"repo:chrisdoofer/App-Mod-Boost:environment:production\",\"audiences\":[\"api://AzureADTokenExchange\"]}'

# Get Tenant ID
$TENANT_ID = az account show --query tenantId -o tsv

# Output the values you need for GitHub
Write-Host ""
Write-Host "============================================" -ForegroundColor Green
Write-Host "Add these as GitHub Repository Variables:" -ForegroundColor Green
Write-Host "============================================" -ForegroundColor Green
Write-Host "AZURE_CLIENT_ID:       $APP_ID"
Write-Host "AZURE_TENANT_ID:       $TENANT_ID"
Write-Host "AZURE_SUBSCRIPTION_ID: $SUBSCRIPTION_ID"
Write-Host ""
```

## Step 2: Configure GitHub Repository

### Add Repository Variables

1. Go to your GitHub repository
2. Navigate to **Settings** → **Secrets and variables** → **Actions**
3. Click the **Variables** tab
4. Add these repository variables:

| Variable Name | Value |
|--------------|-------|
| `AZURE_CLIENT_ID` | The Application (Client) ID from Step 1 |
| `AZURE_TENANT_ID` | Your Azure Tenant ID |
| `AZURE_SUBSCRIPTION_ID` | Your Azure Subscription ID |

### Create Environment (Optional but Recommended)

1. Go to **Settings** → **Environments**
2. Click **New environment**
3. Name it `production`
4. Optionally add:
   - Required reviewers (for manual approval before deployment)
   - Environment protection rules

## Step 3: Run the Workflow

1. Go to **Actions** tab in your repository
2. Select **Deploy to Azure** workflow
3. Click **Run workflow**
4. Fill in the parameters:
   - **Resource Group**: e.g., `rg-expensemgmt-demo-20250125`
   - **Location**: Select your preferred Azure region
   - **Deploy GenAI**: Check if you want Azure OpenAI and AI Search
   - **Environment**: Select `production`
5. Click **Run workflow**

## How OIDC Authentication Works

Instead of storing Azure credentials as GitHub secrets, OIDC federation:

1. GitHub Actions requests a token from GitHub's OIDC provider
2. The token is exchanged with Azure AD using the federated credential
3. Azure validates the token's claims (repository, branch, environment)
4. If valid, Azure issues an access token for the Service Principal
5. The deployment proceeds with the Service Principal's permissions

Benefits:
- **No secrets to rotate** - tokens are short-lived and generated per-run
- **No secret exposure risk** - no credentials stored in GitHub
- **Fine-grained access** - different credentials per branch/environment

## Troubleshooting

### "AADSTS70021: No matching federated identity record found"

The federated credential subject doesn't match. Check:
- Repository name is correct (case-sensitive)
- Branch name matches (main vs master)
- Environment name matches if using environment deployment

### "AZURE_CLIENT_ID environment variable not set"

Ensure the repository variables are configured correctly and the workflow is referencing them properly.

### SQL Server authentication fails

The Service Principal needs to be set as the SQL Server Entra ID admin. This happens automatically during deployment, but verify:
- The Service Principal has the correct Object ID
- The `adminPrincipalType` is set to `Application` (not `User`)

### sqlcmd "Identity not found" or "incomplete environment variable configuration"

This happens when using `ActiveDirectoryDefault` in OIDC because it tries `EnvironmentCredential` first.

**Solution:** Use `ActiveDirectoryAzCli` authentication in CI/CD:
```powershell
$authMethod = if ($IsCI) { "ActiveDirectoryAzCli" } else { "ActiveDirectoryDefault" }
sqlcmd -S $serverFqdn -d "Northwind" "--authentication-method=$authMethod" -Q "SELECT 1"
```

### "Principal could not be resolved" when creating database user

This error occurs when using `CREATE USER ... FROM EXTERNAL PROVIDER` because it requires the SQL Server to have a system-assigned managed identity with Directory Reader permissions in Entra ID.

**Solution:** Use SID-based user creation instead:
```powershell
# Convert Client ID to SID
$guidBytes = [System.Guid]::Parse($managedIdentityClientId).ToByteArray()
$sidHex = "0x" + [System.BitConverter]::ToString($guidBytes).Replace("-", "")

# Create user with SID (no Directory Reader required)
sqlcmd -S $serverFqdn -d "Northwind" "--authentication-method=$authMethod" `
    -Q "CREATE USER [$managedIdentityName] WITH SID = $sidHex, TYPE = E;"
```

### App deployment fails with "SCM container restart"

This happens when deploying the app immediately after infrastructure deployment. The app settings update causes the SCM (deployment) container to restart.

**Solution:** Add a 60-second delay before app deployment in the workflow:
```yaml
- name: Wait for App Service to stabilize
  run: |
    echo "Waiting 60 seconds for App Service settings to apply..."
    sleep 60
```

### Role assignment failures in Bicep

If Bicep includes role assignments (e.g., giving Managed Identity access to Azure OpenAI), you'll see:
> "The client does not have permission to perform action 'Microsoft.Authorization/roleAssignments/write'"

**Solution:** Ensure the Service Principal has both **Contributor** AND **User Access Administrator** roles at the subscription level.

## Local Development

For local development, continue using the deployment scripts directly:

```powershell
# Infrastructure deployment (uses your signed-in Azure CLI credentials)
.\deploy-infra\deploy.ps1 -ResourceGroup "rg-expensemgmt-dev" -Location "uksouth"

# Application deployment
.\deploy-app\deploy.ps1
```

The scripts automatically detect whether they're running in CI/CD or locally and adjust the authentication method accordingly.
