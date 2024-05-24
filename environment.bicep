param location string = 'australiaeast'

var resourceNamePrefix = 'maqrcg'

// Define the storage account
resource storageAccount 'Microsoft.Storage/storageAccounts@2021-08-01' = {
  name: '${resourceNamePrefix}stg001'
  location: location
  sku: {
    name: 'Standard_LRS'
  }
  kind: 'StorageV2'
}

// Define the storage container for QR codes
resource qrCodesContainer 'Microsoft.Storage/storageAccounts/blobServices/containers@2021-08-01' = {
  name: '${storageAccount.name}/default/qr-codes'
  dependsOn: [
    storageAccount
  ]
  properties: {
    publicAccess: 'None'
  }
}

// Define the function app hosting plan (Consumption Plan)
resource functionAppPlan 'Microsoft.Web/serverfarms@2021-02-01' = {
  name: '${resourceNamePrefix}-app-plan-001'
  location: location
  sku: {
    name: 'Y1'
    tier: 'Dynamic'
  }
}

// Define the function app
resource functionApp 'Microsoft.Web/sites@2023-12-01' = {
  name: '${resourceNamePrefix}-func-app-001'
  location: location
  kind: 'functionapp'
  properties: {
    serverFarmId: functionAppPlan.id
    siteConfig: {
      appSettings: [
        {
          name: 'AzureWebJobsStorage'
          value: storageAccount.properties.primaryEndpoints.blob
        }
        {
          name: 'WEBSITE_RUN_FROM_PACKAGE'
          value: '1'
        }
      ]
    }
    httpsOnly: true
  }
}
