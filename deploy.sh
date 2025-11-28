#!/bin/bash
# Deploy Expense Management System to Azure
# This script deploys the infrastructure and application (without GenAI services)
# For GenAI/Chat functionality, use deploy-with-chat.sh instead

set -e

echo "============================================"
echo "Expense Management System - Deployment Script"
echo "============================================"

# Variables - Update these before running
RESOURCE_GROUP="rg-expensemgmt-demo"
LOCATION="uksouth"

# Get current user's info for SQL Admin
echo ""
echo "Getting current user information..."
ADMIN_OBJECT_ID=$(az ad signed-in-user show --query id -o tsv)
ADMIN_LOGIN=$(az ad signed-in-user show --query userPrincipalName -o tsv)

echo "Admin Object ID: $ADMIN_OBJECT_ID"
echo "Admin Login: $ADMIN_LOGIN"

# Create Resource Group if it doesn't exist
echo ""
echo "Step 1: Creating resource group..."
az group create --name $RESOURCE_GROUP --location $LOCATION --output none
echo "✓ Resource group '$RESOURCE_GROUP' created/verified"

# Deploy infrastructure
echo ""
echo "Step 2: Deploying infrastructure (App Service, Managed Identity, SQL Server)..."
DEPLOYMENT_OUTPUT=$(az deployment group create \
    --resource-group $RESOURCE_GROUP \
    --template-file infrastructure/main.bicep \
    --parameters adminObjectId=$ADMIN_OBJECT_ID adminLogin=$ADMIN_LOGIN deployGenAI=false \
    --query "properties.outputs" \
    --output json)

# Extract values from deployment output
APP_SERVICE_NAME=$(echo $DEPLOYMENT_OUTPUT | jq -r '.appServiceName.value')
APP_SERVICE_HOSTNAME=$(echo $DEPLOYMENT_OUTPUT | jq -r '.appServiceDefaultHostName.value')
SQL_SERVER_NAME=$(echo $DEPLOYMENT_OUTPUT | jq -r '.sqlServerName.value')
SQL_SERVER_FQDN=$(echo $DEPLOYMENT_OUTPUT | jq -r '.sqlServerFqdn.value')
DATABASE_NAME=$(echo $DEPLOYMENT_OUTPUT | jq -r '.databaseName.value')
MANAGED_IDENTITY_NAME=$(echo $DEPLOYMENT_OUTPUT | jq -r '.managedIdentityName.value')
MANAGED_IDENTITY_CLIENT_ID=$(echo $DEPLOYMENT_OUTPUT | jq -r '.managedIdentityClientId.value')

echo "✓ Infrastructure deployed successfully"
echo "  App Service: $APP_SERVICE_NAME"
echo "  SQL Server: $SQL_SERVER_FQDN"
echo "  Database: $DATABASE_NAME"
echo "  Managed Identity: $MANAGED_IDENTITY_NAME"

# Configure App Service settings
echo ""
echo "Step 3: Configuring App Service settings..."
CONNECTION_STRING="Server=tcp:${SQL_SERVER_FQDN},1433;Database=${DATABASE_NAME};Authentication=Active Directory Managed Identity;User Id=${MANAGED_IDENTITY_CLIENT_ID};Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"

az webapp config connection-string set \
    --name $APP_SERVICE_NAME \
    --resource-group $RESOURCE_GROUP \
    --connection-string-type SQLAzure \
    --settings "DefaultConnection=$CONNECTION_STRING" \
    --output none

az webapp config appsettings set \
    --name $APP_SERVICE_NAME \
    --resource-group $RESOURCE_GROUP \
    --settings "ConnectionStrings__DefaultConnection=$CONNECTION_STRING" \
    --output none

echo "✓ App Service configured"

# Wait for SQL Server to be ready
echo ""
echo "Step 4: Waiting 30 seconds for SQL Server to be fully ready..."
sleep 30

# Add current IP to SQL firewall
echo ""
echo "Step 5: Adding current IP to SQL Server firewall..."
CURRENT_IP=$(curl -s https://api.ipify.org)
az sql server firewall-rule create \
    --resource-group $RESOURCE_GROUP \
    --server $SQL_SERVER_NAME \
    --name "DeploymentClient-$(date +%s)" \
    --start-ip-address $CURRENT_IP \
    --end-ip-address $CURRENT_IP \
    --output none
echo "✓ Firewall rule added for IP: $CURRENT_IP"

# Update Python scripts with actual server name
echo ""
echo "Step 6: Updating Python scripts with server details..."
sed -i.bak "s/sql-expensemgmt-REPLACE.database.windows.net/${SQL_SERVER_FQDN}/g" scripts/run-sql.py && rm -f scripts/run-sql.py.bak
sed -i.bak "s/sql-expensemgmt-REPLACE.database.windows.net/${SQL_SERVER_FQDN}/g" scripts/run-sql-dbrole.py && rm -f scripts/run-sql-dbrole.py.bak
sed -i.bak "s/sql-expensemgmt-REPLACE.database.windows.net/${SQL_SERVER_FQDN}/g" scripts/run-sql-stored-procs.py && rm -f scripts/run-sql-stored-procs.py.bak
sed -i.bak "s/MANAGED-IDENTITY-NAME/${MANAGED_IDENTITY_NAME}/g" scripts/script.sql && rm -f scripts/script.sql.bak
echo "✓ Python scripts updated"

# Install Python dependencies
echo ""
echo "Step 7: Installing Python dependencies..."
pip3 install --quiet pyodbc azure-identity

# Import database schema
echo ""
echo "Step 8: Importing database schema..."
python3 scripts/run-sql.py

# Configure database roles for managed identity
echo ""
echo "Step 9: Configuring database roles for managed identity..."
python3 scripts/run-sql-dbrole.py

# Create stored procedures
echo ""
echo "Step 10: Creating stored procedures..."
python3 scripts/run-sql-stored-procs.py

# Deploy application code
echo ""
echo "Step 11: Deploying application code..."
if [ -f "app.zip" ]; then
    az webapp deploy \
        --resource-group $RESOURCE_GROUP \
        --name $APP_SERVICE_NAME \
        --src-path ./app.zip \
        --type zip \
        --output none
    echo "✓ Application deployed"
else
    echo "⚠ app.zip not found. Build the application first:"
    echo "  cd src/ExpenseManagement"
    echo "  dotnet publish -c Release -o ./publish"
    echo "  cd publish && zip -r ../../../app.zip . && cd ../../.."
fi

# Summary
echo ""
echo "============================================"
echo "Deployment Complete!"
echo "============================================"
echo ""
echo "Resources created:"
echo "  Resource Group: $RESOURCE_GROUP"
echo "  App Service: $APP_SERVICE_NAME"
echo "  SQL Server: $SQL_SERVER_FQDN"
echo "  Database: $DATABASE_NAME"
echo "  Managed Identity: $MANAGED_IDENTITY_NAME"
echo ""
echo "Application URL: https://${APP_SERVICE_HOSTNAME}/Index"
echo ""
echo "NOTE: The Chat UI will show dummy responses."
echo "To enable AI-powered chat, run: ./deploy-with-chat.sh"
echo ""
echo "To run locally:"
echo "1. Update appsettings.json with:"
echo "   \"ConnectionStrings\": {"
echo "     \"DefaultConnection\": \"Server=tcp:${SQL_SERVER_FQDN},1433;Database=${DATABASE_NAME};Authentication=Active Directory Default;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;\""
echo "   }"
echo "2. Login with: az login"
echo "3. Run: dotnet run"
