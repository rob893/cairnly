@description('Name of the existing VM network security group to add the rule to.')
param nsgName string

@description('Inbound TCP destination port to open (PostgreSQL).')
param port string = '5432'

@description('Source IP addresses allowed inbound — typically the web app outbound IPs. Each entry is a single IP or CIDR.')
param sourceAddressPrefixes array

@description('Rule priority (100–4096). Must be unique within the NSG and below any catch-all deny.')
@minValue(100)
@maxValue(4096)
param priority int = 1100

@description('Rule name.')
param ruleName string = 'AllowCairnlyAppServiceToPostgres'

// Additive child rule: attaching to the existing NSG by name leaves all other rules untouched.
resource nsg 'Microsoft.Network/networkSecurityGroups@2023-11-01' existing = {
  name: nsgName
}

resource postgresRule 'Microsoft.Network/networkSecurityGroups/securityRules@2023-11-01' = {
  parent: nsg
  name: ruleName
  properties: {
    priority: priority
    direction: 'Inbound'
    access: 'Allow'
    protocol: 'Tcp'
    sourcePortRange: '*'
    sourceAddressPrefixes: sourceAddressPrefixes
    destinationAddressPrefix: '*'
    destinationPortRange: port
    description: 'Allow the Cairnly App Service outbound IPs to reach PostgreSQL on the VM.'
  }
}
