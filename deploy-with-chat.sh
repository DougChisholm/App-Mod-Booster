#!/bin/bash
# Deploy infrastructure with GenAI resources and configure App Service
# This script deploys Azure OpenAI and AI Search in addition to base infrastructure

set -e

echo "=============================================="
echo "Expense Management - Deploy with GenAI Chat"
echo "=============================================="

# Variables - Update these before running
RESOURCE_GROUP="${RESOURCE_GROUP:-rg-expensemgmt-demo}"
LOCATION="${LOCATION:-uksouth}"
BASE_NAME="${BASE_NAME:-expensemgmt}"

# Get admin info from Azure CLI
ADMIN_OBJECT_ID=$(az ad signed-in-user show --query id -o tsv)
ADMIN_LOGIN=$(az ad signed-in-user show --query userPrincipalName -o tsv)

echo ""
echo "Configuration:"
echo "  Resource Group: $RESOURCE_GROUP"
echo "  Location: $LOCATION"
echo "  Base Name: $BASE_NAME"
echo "  Admin: $ADMIN_LOGIN"
echo ""

# Step 1: Create Resource Group
echo "Step 1: Creating resource group..."
az group create --name "$RESOURCE_GROUP" --location "$LOCATION" --output none
echo "  ✓ Resource group created"

# Step 2: Deploy Infrastructure with GenAI
echo ""
echo "Step 2: Deploying infrastructure with GenAI resources..."
DEPLOYMENT_OUTPUT=$(az deployment group create \
    --resource-group "$RESOURCE_GROUP" \
    --template-file ./deploy-infra/main.bicep \
    --parameters location="$LOCATION" \
    --parameters baseName="$BASE_NAME" \
    --parameters adminObjectId="$ADMIN_OBJECT_ID" \
    --parameters adminLogin="$ADMIN_LOGIN" \
    --parameters deployGenAI=true \
    --query properties.outputs -o json)

echo "  ✓ Infrastructure deployed"

# Extract outputs
WEB_APP_NAME=$(echo "$DEPLOYMENT_OUTPUT" | jq -r '.webAppName.value')
WEB_APP_HOST=$(echo "$DEPLOYMENT_OUTPUT" | jq -r '.webAppHostName.value')
SQL_SERVER_NAME=$(echo "$DEPLOYMENT_OUTPUT" | jq -r '.sqlServerName.value')
SQL_SERVER_FQDN=$(echo "$DEPLOYMENT_OUTPUT" | jq -r '.sqlServerFqdn.value')
DATABASE_NAME=$(echo "$DEPLOYMENT_OUTPUT" | jq -r '.databaseName.value')
MANAGED_IDENTITY_NAME=$(echo "$DEPLOYMENT_OUTPUT" | jq -r '.managedIdentityName.value')
MANAGED_IDENTITY_CLIENT_ID=$(echo "$DEPLOYMENT_OUTPUT" | jq -r '.managedIdentityClientId.value')
OPENAI_ENDPOINT=$(echo "$DEPLOYMENT_OUTPUT" | jq -r '.openAIEndpoint.value')
OPENAI_MODEL_NAME=$(echo "$DEPLOYMENT_OUTPUT" | jq -r '.openAIModelName.value')
SEARCH_ENDPOINT=$(echo "$DEPLOYMENT_OUTPUT" | jq -r '.searchEndpoint.value')

echo ""
echo "Deployed resources:"
echo "  Web App: $WEB_APP_NAME"
echo "  SQL Server: $SQL_SERVER_NAME"
echo "  Managed Identity: $MANAGED_IDENTITY_NAME"
echo "  OpenAI Endpoint: $OPENAI_ENDPOINT"
echo "  Search Endpoint: $SEARCH_ENDPOINT"

# Step 3: Wait for resources to be ready
echo ""
echo "Step 3: Waiting for resources to be ready..."
sleep 30
echo "  ✓ Wait complete"

# Step 4: Configure SQL Firewall
echo ""
echo "Step 4: Configuring SQL Server firewall..."
MY_IP=$(curl -s https://api.ipify.org)
az sql server firewall-rule create \
    --resource-group "$RESOURCE_GROUP" \
    --server "$SQL_SERVER_NAME" \
    --name "DeploymentMachine" \
    --start-ip-address "$MY_IP" \
    --end-ip-address "$MY_IP" \
    --output none 2>/dev/null || true
echo "  ✓ Firewall configured"

# Step 5: Configure App Service with GenAI settings
echo ""
echo "Step 5: Configuring App Service with GenAI settings..."
az webapp config appsettings set \
    --resource-group "$RESOURCE_GROUP" \
    --name "$WEB_APP_NAME" \
    --settings \
        "AZURE_CLIENT_ID=$MANAGED_IDENTITY_CLIENT_ID" \
        "ManagedIdentityClientId=$MANAGED_IDENTITY_CLIENT_ID" \
        "OpenAI__Endpoint=$OPENAI_ENDPOINT" \
        "OpenAI__DeploymentName=$OPENAI_MODEL_NAME" \
        "Search__Endpoint=$SEARCH_ENDPOINT" \
        "ConnectionStrings__DefaultConnection=Server=tcp:$SQL_SERVER_FQDN,1433;Initial Catalog=$DATABASE_NAME;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;Authentication=Active Directory Managed Identity;User Id=$MANAGED_IDENTITY_CLIENT_ID;" \
    --output none
echo "  ✓ App settings configured"

# Step 6: Update Python scripts with actual values
echo ""
echo "Step 6: Updating Python scripts with actual values..."

# Update run-sql.py
sed -i.bak "s/sql-expensemgmt-UNIQUE.database.windows.net/$SQL_SERVER_FQDN/g" run-sql.py && rm -f run-sql.py.bak

# Update run-sql-dbrole.py
sed -i.bak "s/sql-expensemgmt-UNIQUE.database.windows.net/$SQL_SERVER_FQDN/g" run-sql-dbrole.py && rm -f run-sql-dbrole.py.bak

# Update run-sql-stored-procs.py
sed -i.bak "s/sql-expensemgmt-UNIQUE.database.windows.net/$SQL_SERVER_FQDN/g" run-sql-stored-procs.py && rm -f run-sql-stored-procs.py.bak

# Update script.sql with managed identity name
sed -i.bak "s/MANAGED-IDENTITY-NAME/$MANAGED_IDENTITY_NAME/g" script.sql && rm -f script.sql.bak

echo "  ✓ Scripts updated"

# Step 7: Import database schema
echo ""
echo "Step 7: Importing database schema..."
pip3 install --quiet pyodbc azure-identity
python3 run-sql.py
echo "  ✓ Schema imported"

# Step 8: Configure managed identity database roles
echo ""
echo "Step 8: Configuring managed identity database roles..."
python3 run-sql-dbrole.py
echo "  ✓ Database roles configured"

# Step 9: Create stored procedures
echo ""
echo "Step 9: Creating stored procedures..."
python3 run-sql-stored-procs.py
echo "  ✓ Stored procedures created"

# Step 10: Build and deploy application
echo ""
echo "Step 10: Building and deploying application..."
cd src/ExpenseManagement
dotnet publish -c Release -o ./publish --nologo -v q
cd ./publish
zip -r ../../../app.zip . > /dev/null
cd ../../..
az webapp deploy --resource-group "$RESOURCE_GROUP" --name "$WEB_APP_NAME" --src-path ./app.zip --output none
echo "  ✓ Application deployed"

# Complete
echo ""
echo "=============================================="
echo "Deployment Complete!"
echo "=============================================="
echo ""
echo "Application URL: https://$WEB_APP_HOST/Index"
echo "Swagger API:     https://$WEB_APP_HOST/swagger"
echo "AI Chat:         https://$WEB_APP_HOST/Chat"
echo ""
echo "GenAI Features: ENABLED"
echo "  - Azure OpenAI: $OPENAI_ENDPOINT"
echo "  - AI Search: $SEARCH_ENDPOINT"
echo ""
