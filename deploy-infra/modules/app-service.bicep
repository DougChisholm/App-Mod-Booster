@description('Location for the App Service')
param location string = resourceGroup().location

@description('Base name for the App Service')
param baseName string

@description('Resource ID of the user-assigned managed identity')
param managedIdentityId string

@description('Client ID of the user-assigned managed identity')
param managedIdentityClientId string

// Create unique but deterministic names using resource group id
var uniqueSuffix = uniqueString(resourceGroup().id)
var appServicePlanName = toLower('asp-${baseName}-${uniqueSuffix}')
var webAppName = toLower('app-${baseName}-${uniqueSuffix}')

resource appServicePlan 'Microsoft.Web/serverfarms@2023-01-01' = {
  name: appServicePlanName
  location: location
  sku: {
    name: 'S1'
    tier: 'Standard'
    size: 'S1'
    family: 'S'
    capacity: 1
  }
  kind: 'linux'
  properties: {
    reserved: true
  }
}

resource webApp 'Microsoft.Web/sites@2023-01-01' = {
  name: webAppName
  location: location
  kind: 'app,linux'
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${managedIdentityId}': {}
    }
  }
  properties: {
    serverFarmId: appServicePlan.id
    httpsOnly: true
    siteConfig: {
      linuxFxVersion: 'DOTNETCORE|8.0'
      alwaysOn: true
      ftpsState: 'Disabled'
      minTlsVersion: '1.2'
      appSettings: [
        {
          name: 'AZURE_CLIENT_ID'
          value: managedIdentityClientId
        }
      ]
    }
  }
}

@description('The name of the App Service')
output webAppName string = webApp.name

@description('The default hostname of the App Service')
output webAppHostName string = webApp.properties.defaultHostName

@description('The resource ID of the App Service')
output webAppId string = webApp.id
