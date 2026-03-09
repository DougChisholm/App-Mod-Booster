#!/usr/bin/env bash
# =============================================================================
# deploy-with-chat.sh
# Deploys the Expense Management application to Azure WITH GenAI services
# (Azure OpenAI GPT-4o + Azure AI Search) for the AI chat assistant.
#
# Prerequisites:
#   - Azure CLI installed and logged in (az login)
#   - .NET 8 SDK installed
#   - Python 3 with pip
#   - ODBC Driver 18 for SQL Server installed
#   - Azure subscription with quota for Azure OpenAI in swedencentral
#
# Usage (one-liner from terminal):
#   ADMIN_OBJECT_ID="<your-entra-object-id>" ADMIN_LOGIN="<your-email@domain.com>" ./deploy-with-chat.sh
# =============================================================================

set -e

# ── Configuration ─────────────────────────────────────────────────────────────
RESOURCE_GROUP="${RESOURCE_GROUP:-rg-expensemgmt-demo}"
LOCATION="${LOCATION:-uksouth}"
RESOURCE_PREFIX="${RESOURCE_PREFIX:-expensemgmt}"
ADMIN_OBJECT_ID="${ADMIN_OBJECT_ID:-}"
ADMIN_LOGIN="${ADMIN_LOGIN:-}"
# ──────────────────────────────────────────────────────────────────────────────

echo ""
echo "═══════════════════════════════════════════════════════════════"
echo "  Expense Management System – Deploy WITH GenAI Chat"
echo "═══════════════════════════════════════════════════════════════"
echo ""

if [ -z "$ADMIN_OBJECT_ID" ] || [ -z "$ADMIN_LOGIN" ]; then
    echo "❌ Error: ADMIN_OBJECT_ID and ADMIN_LOGIN must be set."
    exit 1
fi

# ── Step 1: Create resource group ─────────────────────────────────────────────
echo "📦 Step 1: Creating resource group '$RESOURCE_GROUP' in $LOCATION..."
az group create \
    --name     "$RESOURCE_GROUP" \
    --location "$LOCATION" \
    --output   none
echo "   ✓ Resource group ready"

# ── Step 2: Deploy Bicep infrastructure (INCLUDING GenAI) ─────────────────────
echo ""
echo "🏗️  Step 2: Deploying Azure infrastructure (App Service + SQL + GenAI)..."
echo "   ⚠️  Azure OpenAI will be deployed to swedencentral for GPT-4o quota"
DEPLOYMENT_OUTPUT=$(az deployment group create \
    --resource-group   "$RESOURCE_GROUP" \
    --template-file    "infrastructure/main.bicep" \
    --parameters       resourcePrefix="$RESOURCE_PREFIX" \
                       adminObjectId="$ADMIN_OBJECT_ID" \
                       adminLogin="$ADMIN_LOGIN" \
                       deployGenAI=true \
    --query            "properties.outputs" \
    --output           json)

echo "   ✓ Infrastructure deployed (including GenAI resources)"

# Extract outputs
APP_SERVICE_NAME=$(echo "$DEPLOYMENT_OUTPUT" | python3 -c "import sys,json; print(json.load(sys.stdin)['appServiceName']['value'])")
APP_SERVICE_URL=$(echo "$DEPLOYMENT_OUTPUT"  | python3 -c "import sys,json; print(json.load(sys.stdin)['appServiceUrl']['value'])")
MANAGED_IDENTITY_CLIENT_ID=$(echo "$DEPLOYMENT_OUTPUT" | python3 -c "import sys,json; print(json.load(sys.stdin)['managedIdentityClientId']['value'])")
MANAGED_IDENTITY_NAME=$(echo "$DEPLOYMENT_OUTPUT" | python3 -c "import sys,json; print(json.load(sys.stdin)['managedIdentityName']['value'])")
SQL_SERVER_FQDN=$(echo "$DEPLOYMENT_OUTPUT"  | python3 -c "import sys,json; print(json.load(sys.stdin)['sqlServerFqdn']['value'])")
SQL_SERVER_NAME=$(echo "$SQL_SERVER_FQDN"    | cut -d'.' -f1)
DATABASE_NAME=$(echo "$DEPLOYMENT_OUTPUT"    | python3 -c "import sys,json; print(json.load(sys.stdin)['databaseName']['value'])")
OPENAI_ENDPOINT=$(echo "$DEPLOYMENT_OUTPUT"  | python3 -c "import sys,json; print(json.load(sys.stdin)['openAIEndpoint']['value'])")
OPENAI_MODEL_NAME=$(echo "$DEPLOYMENT_OUTPUT"| python3 -c "import sys,json; print(json.load(sys.stdin)['openAIModelName']['value'])")
SEARCH_ENDPOINT=$(echo "$DEPLOYMENT_OUTPUT"  | python3 -c "import sys,json; print(json.load(sys.stdin)['searchEndpoint']['value'])")

echo "   App Service  : $APP_SERVICE_NAME"
echo "   App URL      : $APP_SERVICE_URL/Index"
echo "   SQL Server   : $SQL_SERVER_FQDN"
echo "   OpenAI Endpoint: $OPENAI_ENDPOINT"
echo "   OpenAI Model : $OPENAI_MODEL_NAME"
echo "   Search       : $SEARCH_ENDPOINT"

# ── Step 3: Configure App Service settings (incl. OpenAI) ────────────────────
echo ""
echo "⚙️  Step 3: Configuring App Service settings (including OpenAI)..."
CONNECTION_STRING="Server=tcp:${SQL_SERVER_FQDN},1433;Database=${DATABASE_NAME};Authentication=Active Directory Managed Identity;User Id=${MANAGED_IDENTITY_CLIENT_ID};Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"

az webapp config appsettings set \
    --name           "$APP_SERVICE_NAME" \
    --resource-group "$RESOURCE_GROUP" \
    --settings \
        "AZURE_CLIENT_ID=${MANAGED_IDENTITY_CLIENT_ID}" \
        "ManagedIdentityClientId=${MANAGED_IDENTITY_CLIENT_ID}" \
        "ConnectionStrings__DefaultConnection=${CONNECTION_STRING}" \
        "GenAI__Endpoint=${OPENAI_ENDPOINT}" \
        "GenAI__DeploymentName=${OPENAI_MODEL_NAME}" \
        "GenAI__SearchEndpoint=${SEARCH_ENDPOINT}" \
    --output none
echo "   ✓ App Service configured with OpenAI settings"

# ── Step 4: Wait for SQL Server ───────────────────────────────────────────────
echo ""
echo "⏳ Step 4: Waiting 30 seconds for SQL Server to be fully ready..."
sleep 30

# ── Step 5: SQL Firewall ──────────────────────────────────────────────────────
echo ""
echo "🔒 Step 5: Configuring SQL firewall rules..."
MY_IP=$(curl -s https://api.ipify.org 2>/dev/null || echo "0.0.0.0")

az sql server firewall-rule create \
    --resource-group "$RESOURCE_GROUP" \
    --server         "$SQL_SERVER_NAME" \
    --name           "AllowAllAzureIPs" \
    --start-ip-address 0.0.0.0 \
    --end-ip-address   0.0.0.0 \
    --output none

if [ "$MY_IP" != "0.0.0.0" ]; then
    az sql server firewall-rule create \
        --resource-group "$RESOURCE_GROUP" \
        --server         "$SQL_SERVER_NAME" \
        --name           "AllowDeploymentIP" \
        --start-ip-address "$MY_IP" \
        --end-ip-address   "$MY_IP" \
        --output none
fi
echo "   ✓ Firewall rules configured"
echo "   Waiting additional 15 seconds for firewall rules to propagate..."
sleep 15

# ── Step 6: Python dependencies ──────────────────────────────────────────────
echo ""
echo "🐍 Step 6: Installing Python dependencies..."
pip3 install --quiet pyodbc azure-identity
echo "   ✓ Python packages installed"

# ── Step 7: Import database schema ───────────────────────────────────────────
echo ""
echo "📊 Step 7: Importing database schema..."
sed -i.bak "s|SERVER   = \"example.database.windows.net\"|SERVER   = \"${SQL_SERVER_FQDN}\"|g" run-sql.py && rm -f run-sql.py.bak
sed -i.bak "s|DATABASE = \"Northwind\"|DATABASE = \"${DATABASE_NAME}\"|g" run-sql.py && rm -f run-sql.py.bak
python3 run-sql.py
echo "   ✓ Schema imported"

# ── Step 8: Configure managed identity database roles ────────────────────────
echo ""
echo "🔑 Step 8: Configuring database roles for managed identity..."
export MANAGED_IDENTITY_NAME="$MANAGED_IDENTITY_NAME"
sed -i.bak "s|SERVER          = \"example.database.windows.net\"|SERVER          = \"${SQL_SERVER_FQDN}\"|g" run-sql-dbrole.py && rm -f run-sql-dbrole.py.bak
sed -i.bak "s|DATABASE        = \"Northwind\"|DATABASE        = \"${DATABASE_NAME}\"|g" run-sql-dbrole.py && rm -f run-sql-dbrole.py.bak
python3 run-sql-dbrole.py
echo "   ✓ Database roles configured"

# ── Step 9: Deploy stored procedures ─────────────────────────────────────────
echo ""
echo "📝 Step 9: Deploying stored procedures..."
sed -i.bak "s|SERVER          = \"example.database.windows.net\"|SERVER          = \"${SQL_SERVER_FQDN}\"|g" run-sql-stored-procs.py && rm -f run-sql-stored-procs.py.bak
sed -i.bak "s|DATABASE        = \"Northwind\"|DATABASE        = \"${DATABASE_NAME}\"|g" run-sql-stored-procs.py && rm -f run-sql-stored-procs.py.bak
python3 run-sql-stored-procs.py
echo "   ✓ Stored procedures deployed"

# ── Step 10: Build and package ────────────────────────────────────────────────
echo ""
echo "🔨 Step 10: Building and packaging application..."
cd app/ExpenseManagement

python3 -c "
import json, re
with open('appsettings.json','r') as f: content = f.read()
content = re.sub(r'REPLACE_SQL_SERVER', '${SQL_SERVER_NAME}', content)
content = re.sub(r'REPLACE_MANAGED_IDENTITY_CLIENT_ID', '${MANAGED_IDENTITY_CLIENT_ID}', content)
with open('appsettings.json','w') as f: f.write(content)
print('   appsettings.json updated')
"

dotnet publish -c Release -o ./publish --nologo
cd publish
zip -r ../../app.zip . -x "*.pdb"
cd ../..
echo "   ✓ Application packaged"

# ── Step 11: Deploy to App Service ───────────────────────────────────────────
echo ""
echo "🚀 Step 11: Deploying application to Azure App Service..."
az webapp deploy \
    --resource-group "$RESOURCE_GROUP" \
    --name           "$APP_SERVICE_NAME" \
    --src-path       ./app.zip \
    --type           zip \
    --output         none
echo "   ✓ Application deployed"

# ── Summary ───────────────────────────────────────────────────────────────────
echo ""
echo "═══════════════════════════════════════════════════════════════"
echo "  ✅ Deployment Complete (with GenAI Chat)!"
echo "═══════════════════════════════════════════════════════════════"
echo ""
echo "  🌐 Application URL : ${APP_SERVICE_URL}/Index"
echo "  📖 Swagger API Docs: ${APP_SERVICE_URL}/swagger"
echo "  💬 AI Chat         : ${APP_SERVICE_URL}/Chat"
echo "  🤖 OpenAI Endpoint : ${OPENAI_ENDPOINT}"
echo ""
echo "  ⚠️  NOTE: Navigate to /Index (not the root URL) to view the app."
echo ""
