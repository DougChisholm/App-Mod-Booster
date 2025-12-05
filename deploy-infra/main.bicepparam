using './main.bicep'

// Azure region for deployment
param location = 'uksouth'

// Base name prefix for all resources
param baseName = 'expensemgmt'

// Azure AD Object ID of the SQL administrator - replace with your value
param adminObjectId = 'YOUR_AZURE_AD_OBJECT_ID'

// Azure AD login (UPN) of the SQL administrator - replace with your value
param adminLogin = 'your.email@domain.com'

// Principal type: User for interactive, Application for Service Principal/CI-CD
param adminPrincipalType = 'User'

// Set to true to deploy Azure OpenAI and AI Search
param deployGenAI = false
