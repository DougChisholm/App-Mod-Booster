// azure-sql.bicep
// Deploys: Azure SQL Server + Northwind Database
// Security: Entra ID (Azure AD) only authentication - no SQL auth (MCAPS SFI-ID4.2.2 compliance)
// Uses stable API version 2021-11-01 throughout

param location string = 'uksouth'
param resourcePrefix string = 'expensemgmt'
param adminObjectId string         // Object ID of the Entra ID admin (person deploying)
param adminLogin string            // UPN / email of the Entra ID admin
param managedIdentityPrincipalId string  // Principal ID of the user-assigned managed identity

var uniqueSuffix = uniqueString(resourceGroup().id)
var sqlServerName = 'sql-${resourcePrefix}-${uniqueSuffix}'
var databaseName  = 'Northwind'

// SQL Server
resource sqlServer 'Microsoft.Sql/servers@2021-11-01' = {
  name: sqlServerName
  location: location
  properties: {
    // Disable SQL authentication - Entra ID only
    administrators: {
      administratorType: 'ActiveDirectory'
      azureADOnlyAuthentication: true
      login: adminLogin
      sid: adminObjectId
      tenantId: subscription().tenantId
      principalType: 'User'
    }
  }
}

// Northwind database - Basic tier for development
resource database 'Microsoft.Sql/servers/databases@2021-11-01' = {
  parent: sqlServer
  name: databaseName
  location: location
  sku: {
    name: 'Basic'
    tier: 'Basic'
  }
  properties: {
    collation: 'SQL_Latin1_General_CP1_CI_AS'
    maxSizeBytes: 2147483648
  }
}

// Firewall rule - allow all Azure services (0.0.0.0 - 0.0.0.0)
resource firewallAzureServices 'Microsoft.Sql/servers/firewallRules@2021-11-01' = {
  parent: sqlServer
  name: 'AllowAllAzureIPs'
  properties: {
    startIpAddress: '0.0.0.0'
    endIpAddress: '0.0.0.0'
  }
}

// Azure AD-only authentication setting
resource aadOnlyAuth 'Microsoft.Sql/servers/azureADOnlyAuthentications@2021-11-01' = {
  parent: sqlServer
  name: 'Default'
  properties: {
    azureADOnlyAuthentication: true
  }
}

output sqlServerFqdn string = sqlServer.properties.fullyQualifiedDomainName
output sqlServerName string = sqlServer.name
output databaseName string = databaseName
