// App Service Bicep template
// Deploys App Service with Standard S1 SKU to avoid cold start issues

@description('The location for the app service')
param location string

@description('The name of the app service')
param appServiceName string

@description('The resource ID of the user-assigned managed identity')
param managedIdentityId string

@description('The client ID of the user-assigned managed identity')
param managedIdentityClientId string

// App Service Plan
resource appServicePlan 'Microsoft.Web/serverfarms@2022-09-01' = {
  name: 'asp-${appServiceName}'
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
    reserved: true // Required for Linux
  }
}

// App Service
resource appService 'Microsoft.Web/sites@2022-09-01' = {
  name: appServiceName
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
    siteConfig: {
      linuxFxVersion: 'DOTNETCORE|8.0'
      alwaysOn: true
      ftpsState: 'Disabled'
      minTlsVersion: '1.2'
      http20Enabled: true
      appSettings: [
        {
          name: 'AZURE_CLIENT_ID'
          value: managedIdentityClientId
        }
        {
          name: 'ManagedIdentityClientId'
          value: managedIdentityClientId
        }
      ]
    }
    httpsOnly: true
  }
}

// Outputs
output appServiceName string = appService.name
output defaultHostName string = appService.properties.defaultHostName
output managedIdentityPrincipalId string = managedIdentityClientId
