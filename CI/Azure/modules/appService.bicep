@description('Prefix applied to all resource names.')
param namePrefix string

@description('Region token used in resource names (e.g. ue for East US).')
param regionToken string

@description('Short environment token used in resource names (e.g. d for dev).')
param environment string

@description('Azure region for the web app. Must match the App Service plan\'s region.')
param location string

@description('Resource ID of the (shared) App Service plan to host this web app on.')
param appServicePlanId string

@description('Operating system of the target plan. Drives the runtime stack configuration.')
@allowed([
  'Windows'
  'Linux'
])
param os string = 'Windows'

@description('Application Insights connection string for telemetry.')
param appInsightsConnectionString string

@description('ASP.NET Core environment name (e.g. Development, Production).')
param aspNetCoreEnvironment string = 'Production'

@description('Tags to apply to the web app.')
param tags object = {}

var appName = '${namePrefix}-api-${regionToken}-${environment}'
var isLinux = os == 'Linux'

var baseAppSettings = [
  {
    name: 'ASPNETCORE_ENVIRONMENT'
    value: aspNetCoreEnvironment
  }
  {
    // Config key the API reads (ConfigurationKeys.ApplicationInsightsConnectionString).
    // Telemetry is wired in code via UseAzureMonitor, so the auto-instrumentation agent
    // (ApplicationInsightsAgent_EXTENSION_VERSION) is intentionally NOT enabled to avoid
    // double-instrumentation. Name must match the flat config key exactly.
    name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
    value: appInsightsConnectionString
  }
]

resource webApp 'Microsoft.Web/sites@2024-11-01' = {
  name: appName
  location: location
  tags: tags
  kind: isLinux ? 'app,linux' : 'app'
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: appServicePlanId
    httpsOnly: true
    siteConfig: {
      // Linux uses linuxFxVersion; Windows uses netFrameworkVersion + CURRENT_STACK metadata.
      linuxFxVersion: isLinux ? 'DOTNETCORE|10.0' : null
      netFrameworkVersion: isLinux ? null : 'v10.0'
      metadata: isLinux
        ? null
        : [
            {
              name: 'CURRENT_STACK'
              value: 'dotnet'
            }
          ]
      http20Enabled: true
      minTlsVersion: '1.2'
      ftpsState: 'Disabled'
      appSettings: baseAppSettings
    }
  }
}

output webAppName string = webApp.name
output webAppDefaultHostName string = webApp.properties.defaultHostName
// Used by rbac modules to assign roles to this identity.
output principalId string = webApp.identity.principalId
// All possible outbound IPs; fed to the VM NSG rule so the app can reach PostgreSQL.
// These change only when the plan's pricing tier changes.
output possibleOutboundIps array = split(webApp.properties.possibleOutboundIpAddresses, ',')
