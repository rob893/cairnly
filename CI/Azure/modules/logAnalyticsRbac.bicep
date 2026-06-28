@description('Principal ID of the Web App system-assigned managed identity.')
param webAppPrincipalId string

@description('Name of the existing shared Log Analytics workspace backing App Insights. Lives in this module\'s target (shared) resource group.')
param logAnalyticsWorkspaceName string

// Monitoring Metrics Publisher: 3913510d-42f4-4e42-8a64-420c390055eb.
var metricsPublisherRoleId = subscriptionResourceId(
  'Microsoft.Authorization/roleDefinitions',
  '3913510d-42f4-4e42-8a64-420c390055eb'
)

resource workspace 'Microsoft.OperationalInsights/workspaces@2025-02-01' existing = {
  name: logAnalyticsWorkspaceName
}

// Cross-RG assignment: the web app MI publishes telemetry/metrics to the shared workspace
// that backs the (local-auth-disabled) App Insights component.
resource metricsPublisherAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(workspace.id, webAppPrincipalId, metricsPublisherRoleId)
  scope: workspace
  properties: {
    roleDefinitionId: metricsPublisherRoleId
    principalId: webAppPrincipalId
    principalType: 'ServicePrincipal'
  }
}
