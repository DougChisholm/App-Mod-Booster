@description('Location for the managed identity')
param location string = resourceGroup().location

@description('Base name for the managed identity')
param baseName string

// Create a unique but deterministic name using resource group id
var uniqueSuffix = uniqueString(resourceGroup().id)
var managedIdentityName = toLower('mid-${baseName}-${uniqueSuffix}')

resource managedIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: managedIdentityName
  location: location
}

@description('The resource ID of the managed identity')
output managedIdentityId string = managedIdentity.id

@description('The name of the managed identity')
output managedIdentityName string = managedIdentity.name

@description('The client ID of the managed identity')
output managedIdentityClientId string = managedIdentity.properties.clientId

@description('The principal ID of the managed identity')
output managedIdentityPrincipalId string = managedIdentity.properties.principalId
