// GenAI Resources Bicep template
// Deploys Azure OpenAI and AI Search for chat functionality

@description('The location for GenAI resources (should be swedencentral for GPT-4o)')
param location string = 'swedencentral'

@description('The Principal ID of the managed identity for role assignments')
param managedIdentityPrincipalId string

@description('Unique suffix for resource names')
param uniqueSuffix string

// Use lowercase names to avoid Azure OpenAI naming issues
var openAIName = 'aoai-expensemgmt-${uniqueSuffix}'
var searchName = 'search-expensemgmt-${uniqueSuffix}'
var modelDeploymentName = 'gpt-4o'

// Azure OpenAI Account
resource openAI 'Microsoft.CognitiveServices/accounts@2023-05-01' = {
  name: openAIName
  location: location
  kind: 'OpenAI'
  sku: {
    name: 'S0'
  }
  properties: {
    customSubDomainName: openAIName
    publicNetworkAccess: 'Enabled'
  }
}

// GPT-4o Model Deployment
resource gpt4oDeployment 'Microsoft.CognitiveServices/accounts/deployments@2023-05-01' = {
  parent: openAI
  name: modelDeploymentName
  sku: {
    name: 'Standard'
    capacity: 8
  }
  properties: {
    model: {
      format: 'OpenAI'
      name: 'gpt-4o'
      version: '2024-05-13'
    }
  }
}

// AI Search Service
resource searchService 'Microsoft.Search/searchServices@2023-11-01' = {
  name: searchName
  location: location
  sku: {
    name: 'basic'
  }
  properties: {
    replicaCount: 1
    partitionCount: 1
    hostingMode: 'default'
    publicNetworkAccess: 'enabled'
  }
}

// Role Assignment - Cognitive Services OpenAI User for Managed Identity
resource openAIRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(openAI.id, managedIdentityPrincipalId, 'Cognitive Services OpenAI User')
  scope: openAI
  properties: {
    principalId: managedIdentityPrincipalId
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '5e0bd9bd-7b93-4f28-af87-19fc36ad61bd') // Cognitive Services OpenAI User
    principalType: 'ServicePrincipal'
  }
}

// Role Assignment - Search Index Data Contributor for Managed Identity
resource searchRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(searchService.id, managedIdentityPrincipalId, 'Search Index Data Contributor')
  scope: searchService
  properties: {
    principalId: managedIdentityPrincipalId
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '8ebe5a00-799e-43f5-93ac-243d3dce84a7') // Search Index Data Contributor
    principalType: 'ServicePrincipal'
  }
}

// Outputs
output openAIEndpoint string = openAI.properties.endpoint
output openAIModelName string = modelDeploymentName
output openAIName string = openAI.name
output searchEndpoint string = 'https://${searchService.name}.search.windows.net'
output searchName string = searchService.name
