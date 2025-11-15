@description('Name of the Static Web App')
var webAppName string = 'notifyme-ui'

@description('Name of the Function App')
var functionAppName string = 'notifyme-api'

@description('Cosmos DB account name (must be globally unique)')
var cosmosAccountName string = 'notifymecosmos'

@description('Cosmos DB database name')
var cosmosDbName string = 'notifyme-db'

@description('Cosmos DB container name')
var cosmosContainerName string = 'notifications'

@description('General region for resources')
var location string = resourceGroup().location


resource staticWebApp 'Microsoft.Web/staticSites@2022-09-01' = {
  name: webAppName
  location: location
  sku: {
    name: 'Free'
    tier: 'Free'
  }
}


resource storageAccount 'Microsoft.Storage/storageAccounts@2023-01-01' = {
#disable-next-line BCP334
  name: toLower('${functionAppName}sa')
  location: location
  sku: {
    name: 'Standard_LRS'
  }
  kind: 'StorageV2'
}

resource cosmosAccount 'Microsoft.DocumentDB/databaseAccounts@2023-11-15' = {
  name: cosmosAccountName
  location: location
  kind: 'GlobalDocumentDB'
  properties: {
    enableFreeTier: true
    databaseAccountOfferType: 'Standard'
    locations: [
      {
        locationName: location
        failoverPriority: 0
      }
    ]
  }
}



resource cosmosDatabase 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases@2023-11-15' = {
  parent: cosmosAccount
  name: cosmosDbName
  properties: {
    options: {
      throughput: 400
    }
    resource: {
      id: cosmosDbName
    }
  }
}

resource cosmosContainer 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers@2023-11-15' = {
  parent: cosmosDatabase
  name: cosmosContainerName
  properties: {
    resource: {
      id: cosmosContainerName
      partitionKey: {
        paths: ['/pk']
        kind: 'Hash'
      }
    }
    options: {}
  }
}

resource functionPlan 'Microsoft.Web/serverfarms@2023-12-01' = {
  name: '${functionAppName}-plan'
  location: location
  sku: {
    name: 'Y1'
    tier: 'Dynamic'
  }
}

resource functionApp 'Microsoft.Web/sites@2023-12-01' = {
  name: functionAppName
  location: location
  kind: 'functionapp'
  properties: {
    httpsOnly: true
    serverFarmId: functionPlan.id
    siteConfig: {
      appSettings: [
        {
          name: 'AzureWebJobsStorage'
          value: storageAccount.listKeys().keys[0].value
        }
        {
          name: 'FUNCTIONS_EXTENSION_VERSION'
          value: '~4'
        }
        {
          name: 'WEBSITE_RUN_FROM_PACKAGE'
          value: '1'
        }
        {
          name: 'COSMOS_DB_ENDPOINT'
          value: 'https://${cosmosAccountName}.documents.azure.com:443/'
        }
        {
          name: 'COSMOS_DB_KEY'
          value: cosmosAccount.listKeys().primaryMasterKey
        }
        {
          name: 'COSMOS_DB_DATABASE'
          value: cosmosDbName
        }
        {
          name: 'COSMOS_DB_CONTAINER'
          value: cosmosContainerName
        }
      ]
    }
  }
}

//
// OUTPUTS
//
output staticWebUrl string = staticWebApp.properties.defaultHostname
output functionApiUrl string = functionApp.properties.defaultHostName
output cosmosEndpoint string = cosmosAccount.properties.documentEndpoint
