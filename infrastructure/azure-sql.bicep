// Azure SQL Database Bicep template
// Deploys Azure SQL with Entra ID-only authentication (required by MCAPS governance)

@description('The location for the SQL server')
param location string

@description('The name of the SQL server')
param sqlServerName string

@description('The Object ID of the Entra ID administrator')
param adminObjectId string

@description('The User Principal Name of the Entra ID administrator')
param adminLogin string

@description('The Principal ID of the managed identity for database access')
param managedIdentityPrincipalId string

// SQL Server
resource sqlServer 'Microsoft.Sql/servers@2021-11-01' = {
  name: sqlServerName
  location: location
  properties: {
    minimalTlsVersion: '1.2'
    publicNetworkAccess: 'Enabled'
    administrators: {
      administratorType: 'ActiveDirectory'
      principalType: 'User'
      login: adminLogin
      sid: adminObjectId
      tenantId: subscription().tenantId
      azureADOnlyAuthentication: true
    }
  }
}

// SQL Database - Northwind (using Basic tier for development)
resource database 'Microsoft.Sql/servers/databases@2021-11-01' = {
  parent: sqlServer
  name: 'Northwind'
  location: location
  sku: {
    name: 'Basic'
    tier: 'Basic'
  }
  properties: {
    collation: 'SQL_Latin1_General_CP1_CI_AS'
    maxSizeBytes: 2147483648 // 2GB
  }
}

// Firewall rule to allow Azure services
resource firewallRule 'Microsoft.Sql/servers/firewallRules@2021-11-01' = {
  parent: sqlServer
  name: 'AllowAzureServices'
  properties: {
    startIpAddress: '0.0.0.0'
    endIpAddress: '0.0.0.0'
  }
}

// Outputs
output sqlServerName string = sqlServer.name
output sqlServerFqdn string = sqlServer.properties.fullyQualifiedDomainName
output databaseName string = database.name
