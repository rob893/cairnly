@description('Principal ID of the Web App system-assigned managed identity.')
param webAppPrincipalId string

@description('Name of the Application Insights instance (in this resource group) to grant the Monitoring Metrics Publisher role on.')
param appInsightsName string

// Monitoring Metrics Publisher: 3913510d-42f4-4e42-8a64-420c390055eb
var metricsPublisherRoleId = subscriptionResourceId(
  'Microsoft.Authorization/roleDefinitions',
  '3913510d-42f4-4e42-8a64-420c390055eb'
)

resource appInsightsComp 'Microsoft.Insights/components@2020-02-02' existing = {
  name: appInsightsName
}

// Allows the Web App MI to publish custom metrics to App Insights.
resource metricsPublisherAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(appInsightsComp.id, webAppPrincipalId, metricsPublisherRoleId)
  scope: appInsightsComp
  properties: {
    roleDefinitionId: metricsPublisherRoleId
    principalId: webAppPrincipalId
    principalType: 'ServicePrincipal'
  }
}
