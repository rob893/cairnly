@description('Principal ID of the Web App system-assigned managed identity.')
param webAppPrincipalId string

@description('Name of the existing shared Key Vault to grant the Secrets User role on. Lives in this module\'s target (shared) resource group.')
param keyVaultName string

// Key Vault Secrets User: 4633458b-17de-408a-b874-0445c86b69e6
var kvSecretsUserRoleId = subscriptionResourceId(
  'Microsoft.Authorization/roleDefinitions',
  '4633458b-17de-408a-b874-0445c86b69e6'
)

resource vault 'Microsoft.KeyVault/vaults@2024-11-01' existing = {
  name: keyVaultName
}

// Cross-RG assignment: the web app (Cairnly RG) reads secrets from the shared vault via DefaultAzureCredential.
resource kvSecretsUserAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(vault.id, webAppPrincipalId, kvSecretsUserRoleId)
  scope: vault
  properties: {
    roleDefinitionId: kvSecretsUserRoleId
    principalId: webAppPrincipalId
    principalType: 'ServicePrincipal'
  }
}
