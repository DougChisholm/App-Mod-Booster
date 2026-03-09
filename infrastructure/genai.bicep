// genai.bicep
// Deploys: Azure OpenAI (GPT-4o, swedencentral) + Azure AI Search (S0)
// Uses user-assigned managed identity passed in from main.bicep
// All names lowercase to satisfy Azure OpenAI custom subdomain requirements

param resourcePrefix string = 'expensemgmt'
param managedIdentityPrincipalId string  // Principal ID for role assignments

// Azure OpenAI MUST be in swedencentral for GPT-4o quota availability
var aoaiLocation    = 'swedencentral'
var searchLocation  = 'uksouth'

var uniqueSuffix    = uniqueString(resourceGroup().id)
// Names must be lowercase
var aoaiName        = 'aoai-${resourcePrefix}-${toLower(uniqueSuffix)}'
var searchName      = 'search-${resourcePrefix}-${toLower(uniqueSuffix)}'
var modelDeploymentName = 'gpt-4o'

// Azure OpenAI resource
resource openAI 'Microsoft.CognitiveServices/accounts@2023-05-01' = {
  name: aoaiName
  location: aoaiLocation
  kind: 'OpenAI'
  sku: {
    name: 'S0'
  }
  properties: {
    customSubDomainName: aoaiName
    publicNetworkAccess: 'Enabled'
  }
}

// GPT-4o model deployment
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
      version: '2024-11-20'
    }
  }
}

// Azure AI Search - S0 for low cost
resource aiSearch 'Microsoft.Search/searchServices@2023-11-01' = {
  name: searchName
  location: searchLocation
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

// Role: Cognitive Services OpenAI User (GUID: 5e0bd9bd-7b93-4f28-af87-19fc36ad61bd)
var cognitiveServicesOpenAIUserRoleId = '5e0bd9bd-7b93-4f28-af87-19fc36ad61bd'

resource openAIRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(openAI.id, managedIdentityPrincipalId, cognitiveServicesOpenAIUserRoleId)
  scope: openAI
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', cognitiveServicesOpenAIUserRoleId)
    principalId: managedIdentityPrincipalId
    principalType: 'ServicePrincipal'
  }
}

// Role: Search Index Data Reader (GUID: 1407120a-92aa-4202-b7e9-c0e197c71c8f)
var searchIndexDataReaderRoleId = '1407120a-92aa-4202-b7e9-c0e197c71c8f'

resource searchReaderRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(aiSearch.id, managedIdentityPrincipalId, searchIndexDataReaderRoleId)
  scope: aiSearch
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', searchIndexDataReaderRoleId)
    principalId: managedIdentityPrincipalId
    principalType: 'ServicePrincipal'
  }
}

output openAIEndpoint string = openAI.properties.endpoint
output openAIModelName string = modelDeploymentName
output openAIName string = openAI.name
output searchEndpoint string = 'https://${aiSearch.name}.search.windows.net'
output searchName string = aiSearch.name
