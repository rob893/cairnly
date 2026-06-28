@description('Principal ID of the Web App system-assigned managed identity.')
param webAppPrincipalId string

@description('Name of the existing shared Azure Communication Services resource the web app sends email through. Lives in this module\'s target (shared) resource group.')
param communicationServiceName string

// Communication and Email Service Owner: 09976791-48a7-449e-bb21-39d1a415f350.
// Grants the Entra data-plane access ACS requires to send email via DefaultAzureCredential.
var acsSenderRoleId = subscriptionResourceId(
  'Microsoft.Authorization/roleDefinitions',
  '09976791-48a7-449e-bb21-39d1a415f350'
)

resource communicationService 'Microsoft.Communication/communicationServices@2023-04-01' existing = {
  name: communicationServiceName
}

// Cross-RG assignment: the web app (Cairnly RG) sends email through the shared ACS resource.
resource acsSenderAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(communicationService.id, webAppPrincipalId, acsSenderRoleId)
  scope: communicationService
  properties: {
    roleDefinitionId: acsSenderRoleId
    principalId: webAppPrincipalId
    principalType: 'ServicePrincipal'
  }
}
