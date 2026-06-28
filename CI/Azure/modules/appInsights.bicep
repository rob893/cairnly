@description('Prefix applied to all resource names. Max 16 characters.')
param namePrefix string

@description('Short environment token used in resource names (e.g. d for dev).')
param environment string

@description('Region token used in resource names (e.g. ue for East US).')
param regionToken string

@description('Azure region for Application Insights.')
param location string

@description('Resource ID of the Log Analytics workspace to link (workspace-based App Insights).')
param logAnalyticsWorkspaceId string

@description('Data retention in days.')
@allowed([
  30
  60
  90
  120
  180
  270
  365
  550
  730
])
param retentionInDays int = 90

@description('Disable local (instrumentation-key/connection-string) auth so telemetry ingestion requires Microsoft Entra auth. The app authenticates via DefaultAzureCredential, so its managed identity needs the Monitoring Metrics Publisher role on this component.')
param disableLocalAuth bool = true

@description('Tags to apply to the resource.')
param tags object = {}

var aiName = '${namePrefix}-ai-${regionToken}-${environment}'

resource appInsightsComp 'Microsoft.Insights/components@2020-02-02' = {
  name: aiName
  location: location
  kind: 'web'
  tags: tags
  properties: {
    Application_Type: 'web'
    WorkspaceResourceId: logAnalyticsWorkspaceId
    RetentionInDays: retentionInDays
    IngestionMode: 'LogAnalytics'
    DisableLocalAuth: disableLocalAuth
    publicNetworkAccessForIngestion: 'Enabled'
    publicNetworkAccessForQuery: 'Enabled'
  }
}

output appInsightsName string = appInsightsComp.name
output connectionString string = appInsightsComp.properties.ConnectionString
