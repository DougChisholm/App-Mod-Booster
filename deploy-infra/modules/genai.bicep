@description('Base name for the GenAI resources')
param baseName string

@description('Principal ID of the managed identity for role assignments')
param managedIdentityPrincipalId string

// GenAI resources are deployed to Sweden Central for model availability
var genaiLocation = 'swedencentral'
var uniqueSuffix = uniqueString(resourceGroup().id)
var openAIName = toLower('oai-${baseName}-${uniqueSuffix}')
var searchName = toLower('srch-${baseName}-${uniqueSuffix}')
var modelName = 'gpt-4o'
var modelDeploymentName = 'gpt-4o'

// Azure OpenAI Service
resource openAI 'Microsoft.CognitiveServices/accounts@2023-10-01-preview' = {
  name: openAIName
  location: genaiLocation
  kind: 'OpenAI'
  sku: {
    name: 'S0'
  }
  properties: {
    customSubDomainName: openAIName
    publicNetworkAccess: 'Enabled'
    networkAcls: {
      defaultAction: 'Allow'
    }
  }
}

// Deploy GPT-4o model
resource gpt4oDeployment 'Microsoft.CognitiveServices/accounts/deployments@2023-10-01-preview' = {
  parent: openAI
  name: modelDeploymentName
  sku: {
    name: 'Standard'
    capacity: 8
  }
  properties: {
    model: {
      format: 'OpenAI'
      name: modelName
      version: '2024-08-06'
    }
  }
}

// Azure AI Search
resource search 'Microsoft.Search/searchServices@2023-11-01' = {
  name: searchName
  location: genaiLocation
  sku: {
    name: 'basic'
  }
  properties: {
    hostingMode: 'default'
    publicNetworkAccess: 'enabled'
    partitionCount: 1
    replicaCount: 1
  }
}

// Role assignment: Cognitive Services OpenAI User for managed identity
var cognitiveServicesOpenAIUserRole = subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '5e0bd9bd-7b93-4f28-af87-19fc36ad61bd')

resource openAIRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(openAI.id, managedIdentityPrincipalId, cognitiveServicesOpenAIUserRole)
  scope: openAI
  properties: {
    principalId: managedIdentityPrincipalId
    roleDefinitionId: cognitiveServicesOpenAIUserRole
    principalType: 'ServicePrincipal'
  }
}

// Role assignment: Search Index Data Contributor for managed identity
var searchIndexDataContributorRole = subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '8ebe5a00-799e-43f5-93ac-243d3dce84a7')

resource searchRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(search.id, managedIdentityPrincipalId, searchIndexDataContributorRole)
  scope: search
  properties: {
    principalId: managedIdentityPrincipalId
    roleDefinitionId: searchIndexDataContributorRole
    principalType: 'ServicePrincipal'
  }
}

@description('The endpoint URL of Azure OpenAI')
output openAIEndpoint string = openAI.properties.endpoint

@description('The name of the OpenAI deployment')
output openAIModelName string = modelDeploymentName

@description('The name of the OpenAI resource')
output openAIName string = openAI.name

@description('The endpoint URL of Azure AI Search')
output searchEndpoint string = 'https://${search.name}.search.windows.net'

@description('The name of the Azure AI Search resource')
output searchName string = search.name
