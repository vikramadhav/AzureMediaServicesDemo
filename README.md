# Azure Media Services Demo
Azure Function and ASP.NET Core Web App showing Azure Media Services

This repo is a port of the [Media Services v3 Sample](https://github.com/Azure-Samples/media-services-v3-dotnet-quickstarts) to an Azure Function. Here is the [blog post](https://docs.microsoft.com/en-us/azure/media-services/latest/stream-files-dotnet-quickstart) on this. 

## Prerequisites
To run samples in this repository, you need:
* Visual Studio 2017.
* Latest Azure Functions Workloads
* An Azure Media Services account. See the steps described in [Create a Media Services account](https://docs.microsoft.com/azure/media-services/latest/create-account-cli-quickstart).

## Local Setup
* Clone Repo
* Add new File in Functions Project local.settings.json

File should include information obtained from Azure Media Services

```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "",
    "AzureWebJobsDashboard": "",
    "SubscriptionId": "",
    "Region": "",
    "ResourceGroup": "",
    "AccountName": "",
    "AadTenantId": "",
    "AadClientId": "",
    "AadSecret": "",
    "ArmAadAudience": "",
    "AadEndpoint": "",
    "ArmEndpoint": "",
    "StorageConnectionString": "",
  }
}
````

After adding these settings, you should be able to test the function locally. The function is configured to listen on the `input` container of the Storage Account specified in `StorageConnectionString` 
(you can use same storage account as Azure Functiojn uses). You will need to create the container in whatever blob storage account you use.

I use [Azure Storage Explorer](https://azure.microsoft.com/features/storage-explorer/) to communicate with my Storage Accounts, which makes it quite easy. Once you have the Function running and listening for a blob to be dropped in that container, you can drop one using Storage Explorer. At that point in time, the trigger will fire the funtction and Azure Media Services will process the Video.

Here is the flow of what the function does

1. Picks up blob file from input container
2. Copies blob file to input-asset location for Azure Media Services to pickup (files are called assets in the Azure Media Services world)
3. Runs transform (in this example standard encoder) against asset to generate output-assets
4. Publishes video with a streaming endpoint
5. Inserts record into Azure Table Storage with Name and Endpoint (this is used to get the list of videos for the player page in the Web Project)


On the Web App side, all you need is to add the StorageConnectionString setting to appsettings.json


## Deployment
Easiest way to deploy the function and web app to Azure is to Right-Click --> Publish (not sustainable, don't do this in the real world). Once you deploy, you will need to add settings from Above to Application Settings for the function and web app
