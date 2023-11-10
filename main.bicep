param location string = resourceGroup().location
param storageAccountName string = 'crudustorageacc'
param functionAppName string = 'crudumyfunctionapp'
param firstQueueName string = 'jobstartqueue'
param secondQueueName string = 'imagequeue'
param blobContainerName string = 'myblobcontainer'
param tableName string = 'mytable'
param existingServerFarm string = 'ASP-sspassfunctionsgroup-9169'

resource storageAccount 'Microsoft.Storage/storageAccounts@2023-01-01' = {
  name: storageAccountName
  location: location
  sku: {
    name: 'Standard_LRS'
  }
  kind: 'StorageV2'
  properties: {
    supportsHttpsTrafficOnly: true
  }
}

resource firstQueue 'Microsoft.Storage/storageAccounts/queueServices/queues@2023-01-01' = {
  name: '${storageAccount.name}/default/${firstQueueName}'
  dependsOn: [
    storageAccount
  ]
}

resource secondQueue 'Microsoft.Storage/storageAccounts/queueServices/queues@2023-01-01' = {
  name: '${storageAccount.name}/default/${secondQueueName}'
  dependsOn: [
    storageAccount
  ]
}

resource blobContainer 'Microsoft.Storage/storageAccounts/blobServices/containers@2023-01-01' = {
  name: '${storageAccount.name}/default/${blobContainerName}'
  dependsOn: [
    storageAccount
  ]
}

resource table 'Microsoft.Storage/storageAccounts/tableServices/tables@2023-01-01' = {
  name: '${storageAccount.name}/default/${tableName}'
  dependsOn: [
    storageAccount
  ]
}

// Function app
resource functionApp 'Microsoft.Web/sites@2023-01-01' = {
  name: functionAppName
  location: location
  kind: 'functionapp'
  properties: {
    serverFarmId: '/subscriptions/${subscription().subscriptionId}/resourceGroups/${resourceGroup().name}/providers/Microsoft.Web/serverfarms/${existingServerFarm}'
    siteConfig: {
      appSettings: [
        {
          name: 'FUNCTIONS_WORKER_RUNTIME'
          value: 'dotnet-isolated'
        }
        {
          name: 'AzureWebJobsStorage'
          value: 'DefaultEndpointsProtocol=https;AccountName=${storageAccountName};AccountKey=${listKeys(storageAccount.id, storageAccount.apiVersion).keys[0].value};EndpointSuffix=core.windows.net'
        }
        {
          name: 'WEBSITE_RUN_FROM_PACKAGE'
          value: '1'
        }
        {
          name: 'image-queue'
          value: firstQueueName
        }
        {
          name: 'blob-container-name'
          value: blobContainerName
        }
        {
          name: 'jobid-queue-name'
          value: secondQueueName
        }
        {
          name: 'table-name'
          value: tableName
        }
        {
          name: 'StorageAccountKey'
          value: listKeys(storageAccount.id, storageAccount.apiVersion).keys[0].value
        }
        {
          name: 'StorageAccountConnectionString'
          value: 'DefaultEndpointsProtocol=https;AccountName=${storageAccountName};AccountKey=${listKeys(storageAccount.id, storageAccount.apiVersion).keys[0].value};EndpointSuffix=core.windows.net'
        }
      ]
    }
  }
  dependsOn: [
    storageAccount
  ]
}

// Hosting plan for the function app
resource hostingPlan 'Microsoft.Web/serverfarms@2023-01-01' = {
  name: '${existingServerFarm}'
  location: location
  properties: {
    isXenon: false
  }
  sku: {
    name: 'Y1' 
  }
}
