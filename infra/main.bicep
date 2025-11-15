@secure()
param sqlAdminPassword string

var location string = resourceGroup().location
var appServiceName string = 'notifyme-api'
var storageName string = 'notifymestorage001'
var sqlServerName string = 'notifyme-sqlserver'
var sqlAdminLogin string = 'notifyadmin'
var sqlDbName string = 'NotifyMeDB'
var staticWebAppName = 'notifyme-api-web'

//
// App Service Plan (Free Tier)
//
resource plan 'Microsoft.Web/serverfarms@2023-12-01' = {
  name: '${appServiceName}-plan'
  location: location
  sku: {
    name: 'F1'
    tier: 'Free'
  }
}

//
// Web API App Service
//
resource apiApp 'Microsoft.Web/sites@2023-12-01' = {
  name: appServiceName
  location: location
  kind: 'app'
  properties: {
    httpsOnly: true
    serverFarmId: plan.id
  }
}

resource uiApp 'Microsoft.Web/sites@2023-12-01' = {
  name: 'notifyme-ui'
  location: location
  kind: 'app'
  properties: {
    httpsOnly: true
    serverFarmId: plan.id
  }
}

//
// Storage Account
//
resource storage 'Microsoft.Storage/storageAccounts@2023-04-01' = {
  name: storageName
  location: location
  sku: {
    name: 'Standard_LRS'
  }
  kind: 'StorageV2'
}

//
// SQL Server
//
resource sqlServer 'Microsoft.Sql/servers@2022-05-01-preview' = {
  name: sqlServerName
  location: location
  properties: {
    administratorLogin: sqlAdminLogin
    administratorLoginPassword: sqlAdminPassword
    version: '12.0'
  }
}

//
// SQL Database
//
resource sqlDb 'Microsoft.Sql/servers/databases@2022-05-01-preview' = {
  parent: sqlServer
  name: sqlDbName
  location: location
  sku: {
    name: 'Basic'
  }
}

output apiAppUrl string = apiApp.properties.defaultHostName
output uiAppUrl string = uiApp.properties.defaultHostName
output sqlServerFullName string = sqlServer.properties.fullyQualifiedDomainName
