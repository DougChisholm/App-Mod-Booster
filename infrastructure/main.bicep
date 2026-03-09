// main.bicep
// Orchestrates deployment of all resources:
//   - App Service + Managed Identity (app-service.bicep)
//   - Azure SQL Database (azure-sql.bicep)
//   - Azure OpenAI + AI Search - optional (genai.bicep)
//
// NOTE: OpenAI__Endpoint and OpenAI__DeploymentName are set by the deployment scripts
//       AFTER this Bicep completes, to avoid circular dependency.

param location string = 'uksouth'
param resourcePrefix string = 'expensemgmt'
param adminObjectId string        // Entra ID admin Object ID
param adminLogin string           // Entra ID admin UPN/email
param deployGenAI bool = false    // Set true in deploy-with-chat.sh

// App Service + Managed Identity
module appService 'app-service.bicep' = {
  name: 'deploy-app-service'
  params: {
    location: location
    resourcePrefix: resourcePrefix
  }
}

// Azure SQL Database
module azureSql 'azure-sql.bicep' = {
  name: 'deploy-azure-sql'
  params: {
    location: location
    resourcePrefix: resourcePrefix
    adminObjectId: adminObjectId
    adminLogin: adminLogin
    managedIdentityPrincipalId: appService.outputs.managedIdentityPrincipalId
  }
}

// Azure OpenAI + AI Search (conditional - only when deployGenAI=true)
module genAI 'genai.bicep' = if (deployGenAI) {
  name: 'deploy-genai'
  params: {
    resourcePrefix: resourcePrefix
    managedIdentityPrincipalId: appService.outputs.managedIdentityPrincipalId
  }
}

// Outputs consumed by deployment scripts
output appServiceName string = appService.outputs.appServiceName
output appServiceUrl string = appService.outputs.appServiceUrl
output managedIdentityClientId string = appService.outputs.managedIdentityClientId
output managedIdentityName string = appService.outputs.managedIdentityName
output sqlServerFqdn string = azureSql.outputs.sqlServerFqdn
output sqlServerName string = azureSql.outputs.sqlServerName
output databaseName string = azureSql.outputs.databaseName

// GenAI outputs - null-safe for when deployGenAI=false
output openAIEndpoint string = deployGenAI ? genAI.outputs!.openAIEndpoint : ''
output openAIModelName string = deployGenAI ? genAI.outputs!.openAIModelName : ''
output searchEndpoint string = deployGenAI ? genAI.outputs!.searchEndpoint : ''
