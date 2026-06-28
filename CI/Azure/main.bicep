targetScope = 'subscription'

@description('Name of the Cairnly resource group to create and deploy the app into.')
param cairnlyResourceGroupName string = 'rherber-cairnly-rg-uc-d'

@description('Azure region for the Cairnly resource group and its resources. Must match the shared App Service plan\'s region (Central US) since an app and its plan must be co-located.')
param location string = 'centralus'

@description('Short prefix applied to every resource name (e.g. "rherber-cairnly").')
@maxLength(20)
param namePrefix string = 'rherber-cairnly'

@description('Region token used in resource names (e.g. uc for Central US).')
param regionToken string = 'uc'

@description('Short deployment environment token used in names (e.g. d for dev).')
param environment string = 'd'

@description('Operating system of the existing shared App Service plan. Must match the plan so the web app stack is configured correctly. Windows is the default for richer in-portal .NET diagnostics (Profiler, Snapshot Debugger).')
@allowed([
  'Windows'
  'Linux'
])
param appServiceOs string = 'Windows'

// ── Shared resources (live in the shared resource group, all pre-created) ───
@description('Name of the shared resource group that holds the App Service plan, Key Vault, Log Analytics workspace, and the database VM/NSG.')
param sharedResourceGroupName string = 'rherber-shared-rg-ue-d'

@description('Name of the existing shared App Service plan to host the web app on. Located in Central US — the Cairnly web app must be deployed in the same region.')
param appServicePlanName string = 'rherber-shared-asp-uc-d'

@description('Name of the existing shared Key Vault the web app reads secrets from.')
param sharedKeyVaultName string = 'rherber-kv-ue-d'

@description('Name of the existing shared Azure Communication Services resource the web app sends email through.')
param sharedCommunicationServiceName string = 'rherber-acs-g-d'

@description('Name of the existing shared Log Analytics workspace to link App Insights to.')
param sharedLogAnalyticsWorkspaceName string = 'rherber-logworkspace-uw-lws-d'

// ── PostgreSQL-on-VM connectivity ───────────────────────────────────────────
@description('Name of the existing VM network security group to open PostgreSQL on.')
param vmNsgName string = 'rherber-vm-ue-d-nsg'

@description('PostgreSQL TCP port on the VM.')
param postgresPort string = '5432'

@description('Application Insights data retention in days.')
@allowed([30, 60, 90, 120, 180, 270, 365, 550, 730])
param appInsightsRetentionInDays int = 90

@description('Additional resource tags merged over the defaults (environment, project, managedBy).')
param tags object = {}

// Merge caller-supplied tags over opinionated defaults.
var resolvedTags = union(
  {
    environment: environment
    project: 'cairnly'
  },
  tags
)

// ── Cairnly resource group (created by this deployment) ─────────────────────
resource cairnlyResourceGroup 'Microsoft.Resources/resourceGroups@2024-03-01' = {
  name: cairnlyResourceGroupName
  location: location
  tags: resolvedTags
}

// ── Existing shared references (cross-RG) ───────────────────────────────────
resource sharedLogAnalytics 'Microsoft.OperationalInsights/workspaces@2025-02-01' existing = {
  name: sharedLogAnalyticsWorkspaceName
  scope: resourceGroup(sharedResourceGroupName)
}

// ── Existing shared App Service plan (pre-created in the shared resource group)
resource sharedAppServicePlan 'Microsoft.Web/serverfarms@2024-11-01' existing = {
  name: appServicePlanName
  scope: resourceGroup(sharedResourceGroupName)
}

// ── Application Insights (workspace-based, linked to shared Log Analytics) ───
module appInsights 'modules/appInsights.bicep' = {
  name: 'appInsights'
  scope: resourceGroup(cairnlyResourceGroup.name)
  params: {
    namePrefix: namePrefix
    regionToken: regionToken
    environment: environment
    location: location
    logAnalyticsWorkspaceId: sharedLogAnalytics.id
    retentionInDays: appInsightsRetentionInDays
    tags: resolvedTags
  }
}

// ── Web App (system-assigned MI, .NET 10, HTTPS-only) on the shared plan ────
module appService 'modules/appService.bicep' = {
  name: 'appService'
  scope: resourceGroup(cairnlyResourceGroup.name)
  params: {
    namePrefix: namePrefix
    regionToken: regionToken
    environment: environment
    location: location
    appServicePlanId: sharedAppServicePlan.id
    os: appServiceOs
    appInsightsConnectionString: appInsights.outputs.connectionString
    tags: resolvedTags
  }
}

// ── RBAC: Web App MI → App Insights metrics (Cairnly RG) + shared Key Vault secrets (shared RG)
module aiRbac 'modules/rbac.bicep' = {
  name: 'aiRbac'
  scope: resourceGroup(cairnlyResourceGroup.name)
  params: {
    webAppPrincipalId: appService.outputs.principalId
    appInsightsName: appInsights.outputs.appInsightsName
  }
}

module kvRbac 'modules/keyVaultRbac.bicep' = {
  name: 'kvRbac'
  scope: resourceGroup(sharedResourceGroupName)
  params: {
    webAppPrincipalId: appService.outputs.principalId
    keyVaultName: sharedKeyVaultName
  }
}

// ── RBAC: Web App MI → send email via the shared ACS resource (shared RG) ───
module acsRbac 'modules/acsEmailRbac.bicep' = {
  name: 'acsRbac'
  scope: resourceGroup(sharedResourceGroupName)
  params: {
    webAppPrincipalId: appService.outputs.principalId
    communicationServiceName: sharedCommunicationServiceName
  }
}

// ── RBAC: Web App MI → publish telemetry to the shared Log Analytics workspace
// (App Insights has local auth disabled, so ingestion requires Entra auth). ───
module logAnalyticsRbac 'modules/logAnalyticsRbac.bicep' = {
  name: 'logAnalyticsRbac'
  scope: resourceGroup(sharedResourceGroupName)
  params: {
    webAppPrincipalId: appService.outputs.principalId
    logAnalyticsWorkspaceName: sharedLogAnalyticsWorkspaceName
  }
}

// ── Open PostgreSQL on the VM NSG to the web app outbound IPs (shared RG) ────
module postgresNsgRule 'modules/nsgPostgresRule.bicep' = {
  name: 'postgresNsgRule'
  scope: resourceGroup(sharedResourceGroupName)
  params: {
    nsgName: vmNsgName
    port: postgresPort
    sourceAddressPrefixes: appService.outputs.possibleOutboundIps
  }
}

// ── Outputs for downstream use (pipeline variables, post-deploy config) ──────
output cairnlyResourceGroupName string = cairnlyResourceGroup.name
output webAppName string = appService.outputs.webAppName
output webAppDefaultHostName string = appService.outputs.webAppDefaultHostName
output appServicePlanId string = sharedAppServicePlan.id
output appInsightsConnectionString string = appInsights.outputs.connectionString
output webAppOutboundIps array = appService.outputs.possibleOutboundIps
