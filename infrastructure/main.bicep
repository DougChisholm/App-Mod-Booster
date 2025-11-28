// Main Bicep deployment template for Expense Management System
// Deploys: Resource Group resources, App Service, Managed Identity, Azure SQL

@description('The location for all resources')
param location string = 'uksouth'

@description('Enable GenAI resources deployment')
param deployGenAI bool = false

@description('Admin Object ID for SQL Server Entra ID authentication')
param adminObjectId string

@description('Admin login (User Principal Name) for SQL Server Entra ID authentication')
param adminLogin string

// Generate unique suffix for resource names
var uniqueSuffix = toLower(uniqueString(resourceGroup().id))
var appServiceName = 'app-expensemgmt-${uniqueSuffix}'
var sqlServerName = 'sql-expensemgmt-${uniqueSuffix}'
var managedIdentityName = 'mid-expensemgmt-${uniqueSuffix}'

// Deploy Managed Identity
module managedIdentity 'managed-identity.bicep' = {
  name: 'managedIdentityDeployment'
  params: {
    location: location
    managedIdentityName: managedIdentityName
  }
}

// Deploy App Service
module appService 'app-service.bicep' = {
  name: 'appServiceDeployment'
  params: {
    location: location
    appServiceName: appServiceName
    managedIdentityId: managedIdentity.outputs.managedIdentityId
    managedIdentityClientId: managedIdentity.outputs.managedIdentityClientId
  }
}

// Deploy Azure SQL
module azureSql 'azure-sql.bicep' = {
  name: 'azureSqlDeployment'
  params: {
    location: location
    sqlServerName: sqlServerName
    adminObjectId: adminObjectId
    adminLogin: adminLogin
    managedIdentityPrincipalId: managedIdentity.outputs.managedIdentityPrincipalId
  }
}

// Deploy GenAI resources (optional)
module genai 'genai.bicep' = if (deployGenAI) {
  name: 'genaiDeployment'
  params: {
    location: 'swedencentral' // Azure OpenAI available region
    managedIdentityPrincipalId: managedIdentity.outputs.managedIdentityPrincipalId
    uniqueSuffix: uniqueSuffix
  }
}

// Outputs
output appServiceName string = appService.outputs.appServiceName
output appServiceDefaultHostName string = appService.outputs.defaultHostName
output sqlServerName string = azureSql.outputs.sqlServerName
output sqlServerFqdn string = azureSql.outputs.sqlServerFqdn
output databaseName string = azureSql.outputs.databaseName
output managedIdentityName string = managedIdentity.outputs.managedIdentityName
output managedIdentityClientId string = managedIdentity.outputs.managedIdentityClientId
output managedIdentityPrincipalId string = managedIdentity.outputs.managedIdentityPrincipalId

// GenAI outputs (null-safe for when not deployed)
output openAIEndpoint string = deployGenAI ? genai.outputs.openAIEndpoint : ''
output openAIModelName string = deployGenAI ? genai.outputs.openAIModelName : ''
output openAIName string = deployGenAI ? genai.outputs.openAIName : ''
output searchEndpoint string = deployGenAI ? genai.outputs.searchEndpoint : ''
