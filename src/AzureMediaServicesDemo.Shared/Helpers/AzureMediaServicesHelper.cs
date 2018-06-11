using Microsoft.Azure.Management.Media;
using Microsoft.Azure.Management.Media.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AzureMediaServicesDemo.Shared
{
    public interface IAzureMediaServicesHelper
    {
        Task ProcessVideo(IAzureMediaServicesClient client, string InputMP4FileName);

        IAzureMediaServicesClient CreateMediaServicesClient();
    }

    /// <summary>
    /// Service to Handle Interactions with Azure Media Services
    /// </summary>
    /// <remarks>
    /// This class allows the facilitation of adding and retrieving videos from the Video Repo.
    /// Video processing is handled by v3 of Azure Media Services SDK
    /// </remarks>
    public class AzureMediaServicesHelper : IAzureMediaServicesHelper
    {
        private const string AdaptiveStreamingTransformName = "MyTransformWithAdaptiveStreamingPreset";
        private readonly IStorageHelper _storageHelpers;
        private readonly ILogger<AzureMediaServicesHelper> _log;
        private readonly ConfigWrapper _config;

        /// <summary>
        /// Initializes a new instance of the <see cref="AzureMediaServicesHelper"/> class.
        /// </summary>
        /// <param name="log">Instance of ILogger resolved via DI.</param>
        /// <param name="config">Instance of ConfigWrapper which is Env Variables.</param>
        /// <param name="storageHelper">Instance of IStorageHelper service to interact with Azure Storage</param>
        public AzureMediaServicesHelper(ConfigWrapper config, ILogger<AzureMediaServicesHelper> log, IStorageHelper storageHelper)
        {
            _config = config;
            _log = log;
            _storageHelpers = storageHelper;
        }


        /// <summary>
        /// If the specified transform exists, get that transform.
        /// If the it does not exist, creates a new transform with the specified output. 
        /// In this case, the output is set to encode a video using one of the built-in encoding presets.
        /// </summary>
        /// <param name="client">The Media Services client.</param>
        /// <param name="resourceGroupName">The name of the resource group within the Azure subscription.</param>
        /// <param name="accountName"> The Media Services account name.</param>
        /// <param name="transformName">The name of the transform.</param>
        /// <returns></returns>
        // <EnsureTransformExists>
        private static async Task<Transform> GetOrCreateTransformAsync(
            IAzureMediaServicesClient client,
            string resourceGroupName,
            string accountName,
            string transformName)
        {
            // Does a Transform already exist with the desired name? Assume that an existing Transform with the desired name
            // also uses the same recipe or Preset for processing content.
            Transform transform = await client.Transforms.GetAsync(resourceGroupName, accountName, transformName);

            if (transform == null)
            {
                // You need to specify what you want it to produce as an output
                TransformOutput[] output = new TransformOutput[]
                {
                    new TransformOutput
                    {
                        // The preset for the Transform is set to one of Media Services built-in sample presets.
                        // You can  customize the encoding settings by changing this to use "StandardEncoderPreset" class.
                        Preset = new BuiltInStandardEncoderPreset()
                        {
                            // This sample uses the built-in encoding preset for Adaptive Bitrate Streaming.
                            PresetName = EncoderNamedPreset.AdaptiveStreaming
                        }
                    }
                };

                // Create the Transform with the output defined above
                transform = await client.Transforms.CreateOrUpdateAsync(resourceGroupName, accountName, transformName, output);
            }

            return transform;
        }

        /// <summary>
        /// Takes input filename and creates, starts, and monitors processing job
        /// When job completes, video is stored in Azure Table Storage
        /// </summary>
        /// <param name="client">The Media Services client.</param>
        /// <param name="fileName">The name of the file to process with Azure Media Services</param>
        /// <returns></returns>
        // <ProcessVideo>
        public async Task ProcessVideo(IAzureMediaServicesClient client, string fileName)
        {
          await  GetOrCreateTransformAsync(client, _config.ResourceGroup, _config.AccountName, AdaptiveStreamingTransformName);

            // Creating a unique suffix so that we don't have name collisions if you run the sample
            // multiple times without cleaning up.
            string uniqueness = Guid.NewGuid().ToString().Substring(0, 13);
            string jobName = "job-" + uniqueness;
            string locatorName = "locator-" + uniqueness;
            string outputAssetName = "output-" + uniqueness;
            string inputAssetName = "input-" + uniqueness;

            // Create a new input Asset and upload the specified local video file into it.
            await CreateInputAssetAsync(client, _config.ResourceGroup, _config.AccountName, inputAssetName, fileName);

            // Use the name of the created input asset to create the job input.
            JobInput jobInput = new JobInputAsset(assetName: inputAssetName);

            // Output from the encoding Job must be written to an Asset, so let's create one
            Asset outputAsset = await CreateOutputAssetAsync(client, _config.ResourceGroup, _config.AccountName, outputAssetName);

            Job job = await SubmitJobAsync(client, _config.ResourceGroup, _config.AccountName, AdaptiveStreamingTransformName, jobName, jobInput, outputAssetName);

            // In this demo code, we will poll for Job status
            // Polling is not a recommended best practice for production applications because of the latency it introduces.
            // Overuse of this API may trigger throttling. Developers should instead use Event Grid.
            job = await WaitForJobToFinishAsync(client, _config.ResourceGroup, _config.AccountName, AdaptiveStreamingTransformName, jobName);

            if (job.State == JobState.Finished)
            {
                StreamingLocator locator = CreateStreamingLocator(client, _config.ResourceGroup, _config.AccountName, outputAsset.Name, locatorName);

                IList<string> urls = GetStreamingURLs(client, _config.ResourceGroup, _config.AccountName, locator.Name);
                _log.LogInformation("Urls Created");
                foreach (var url in urls)
                {
                    _log.LogInformation(url);
                }
                // Get url of manifest file supported for Azure Media Player Playback
                var streamingUrl = urls.Where(a => a.EndsWith("manifest")).FirstOrDefault();

                await _storageHelpers.InsertTableRecord(fileName, streamingUrl);
                _log.LogInformation("Stream Processing Complete");
            }
        }
        // </ProcessVideo>

        /// <summary>
        /// Creates the AzureMediaServicesClient object based on the credentials
        /// supplied in App.config.
        /// </summary>
        /// <param name="config">The parm is of type configWrapper. This class reads values from app.config.</param>
        /// <returns></returns>
        // <CreateMediaServicesClient>
        public IAzureMediaServicesClient CreateMediaServicesClient()
        {
            ArmClientCredentials credentials = new ArmClientCredentials(_config);

            return new AzureMediaServicesClient(_config.ArmEndpoint, credentials)
            {
                SubscriptionId = _config.SubscriptionId,
            };
        }
        // </CreateMediaServicesClient>

        #region Job Methods

        /// <summary>
        /// Submits a request to Media Services to apply the specified Transform to a given input video.
        /// </summary>
        /// <param name="client">The Media Services client.</param>
        /// <param name="resourceGroupName">The name of the resource group within the Azure subscription.</param>
        /// <param name="accountName"> The Media Services account name.</param>
        /// <param name="transformName">The name of the transform.</param>
        /// <param name="jobName">The (unique) name of the job.</param>
        /// <param name="jobInput"></param>
        /// <param name="outputAssetName">The (unique) name of the  output asset that will store the result of the encoding job. </param>
        // <SubmitJob>
        private static async Task<Job> SubmitJobAsync(IAzureMediaServicesClient client,
            string resourceGroupName,
            string accountName,
            string transformName,
            string jobName,
            JobInput jobInput,
            string outputAssetName)
        {
            JobOutput[] jobOutputs =
            {
                new JobOutputAsset(outputAssetName),
            };

            // In this example, we are assuming that the job name is unique.
            //
            // If you already have a job with the desired name, use the Jobs.Get method
            // to get the existing job. In Media Services v3, Get methods on entities returns null 
            // if the entity doesn't exist (a case-insensitive check on the name).
            Job job = await client.Jobs.CreateAsync(
                resourceGroupName,
                accountName,
                transformName,
                jobName,
                new Job
                {
                    Input = jobInput,
                    Outputs = jobOutputs,
                });

            return job;
        }
        // </SubmitJob>

        /// <summary>
        /// Polls Media Services for the status of the Job.
        /// </summary>
        /// <param name="client">The Media Services client.</param>
        /// <param name="resourceGroupName">The name of the resource group within the Azure subscription.</param>
        /// <param name="accountName"> The Media Services account name.</param>
        /// <param name="transformName">The name of the transform.</param>
        /// <param name="jobName">The name of the job you submitted.</param>
        /// <returns></returns>
        // <WaitForJobToFinish>
        private async Task<Job> WaitForJobToFinishAsync(IAzureMediaServicesClient client,
            string resourceGroupName,
            string accountName,
            string transformName,
            string jobName)
        {
            const int SleepIntervalMs = 60 * 1000;

            Job job = null;

            do
            {
                job = await client.Jobs.GetAsync(resourceGroupName, accountName, transformName, jobName);

                Console.WriteLine($"Job is '{job.State}'.");
                for (int i = 0; i < job.Outputs.Count; i++)
                {
                    JobOutput output = job.Outputs[i];
                    Console.Write($"\tJobOutput[{i}] is '{output.State}'.");
                    if (output.State == JobState.Processing)
                    {
                        Console.Write($"  Progress: '{output.Progress}'.");
                    }

                    Console.WriteLine();
                }

                if (job.State != JobState.Finished && job.State != JobState.Error && job.State != JobState.Canceled)
                {
                    await Task.Delay(SleepIntervalMs);
                }
            }
            while (job.State != JobState.Finished && job.State != JobState.Error && job.State != JobState.Canceled);

            return job;
        }
        // </WaitForJobToFinish>

        #endregion

        #region Asset Methods

        /// <summary>
        /// Creates a new input Asset and uploads the specified local video file into it.
        /// </summary>
        /// <param name="client">The Media Services client.</param>
        /// <param name="resourceGroupName">The name of the resource group within the Azure subscription.</param>
        /// <param name="accountName"> The Media Services account name.</param>
        /// <param name="assetName">The asset name.</param>
        /// <param name="fileToUpload">The file you want to upload into the asset.</param>
        /// <returns></returns>
        // <CreateInputAsset>
        private async Task<Asset> CreateInputAssetAsync(
            IAzureMediaServicesClient client,
            string resourceGroupName,
            string accountName,
            string assetName,
            string fileToUpload)
        {
            // In this example, we are assuming that the asset name is unique.
            //
            // If you already have an asset with the desired name, use the Assets.Get method
            // to get the existing asset. In Media Services v3, Get methods on entities returns null 
            // if the entity doesn't exist (a case-insensitive check on the name).

            // Call Media Services API to create an Asset.
            // This method creates a container in storage for the Asset.
            // The files (blobs) associated with the asset will be stored in this container.
            Asset asset = await client.Assets.CreateOrUpdateAsync(resourceGroupName, accountName, assetName, new Asset());

            // Use Media Services API to get back a response that contains
            // SAS URL for the Asset container into which to upload blobs.
            // That is where you would specify read-write permissions 
            // and the exparation time for the SAS URL.
            var response = await client.Assets.ListContainerSasAsync(
                resourceGroupName,
                accountName,
                assetName,
                permissions: AssetContainerPermission.ReadWrite,
                expiryTime: DateTime.UtcNow.AddHours(4).ToUniversalTime());

            var sasUri = new Uri(response.AssetContainerSasUrls.First());

            _log.LogInformation($"Uploading {fileToUpload}");
            // Use Strorage API to upload the file into the container in storage.
            _storageHelpers.CopyBlobAsync(sasUri, fileToUpload).Wait();
            _log.LogInformation("Upload Complete");

            asset = client.Assets.GetAsync(resourceGroupName, accountName, assetName, new System.Threading.CancellationToken()).Result;


            return asset;
        }
        // </CreateInputAsset>

        /// <summary>
        /// Creates an ouput asset. The output from the encoding Job must be written to an Asset.
        /// </summary>
        /// <param name="client">The Media Services client.</param>
        /// <param name="resourceGroupName">The name of the resource group within the Azure subscription.</param>
        /// <param name="accountName"> The Media Services account name.</param>
        /// <param name="assetName">The output asset name.</param>
        /// <returns></returns>
        // <CreateOutputAsset>
        private async Task<Asset> CreateOutputAssetAsync(IAzureMediaServicesClient client, string resourceGroupName, string accountName, string assetName)
        {
            // Check if an Asset already exists
            Asset outputAsset = await client.Assets.GetAsync(resourceGroupName, accountName, assetName);
            Asset asset = new Asset();
            string outputAssetName = assetName;

            if (outputAsset != null)
            {
                // Name collision! In order to get the sample to work, let's just go ahead and create a unique asset name
                // Note that the returned Asset can have a different name than the one specified as an input parameter.
                // You may want to update this part to throw an Exception instead, and handle name collisions differently.
                string uniqueness = $"-{Guid.NewGuid().ToString("N")}";
                outputAssetName += uniqueness;
            }

            return await client.Assets.CreateOrUpdateAsync(resourceGroupName, accountName, outputAssetName, asset);
        }
        // </CreateOutputAsset>
        #endregion Asset Methods

        #region Streaming Methods


        /// <summary>
        /// Checks if the "default" streaming endpoint is in the running state,
        /// if not, starts it.
        /// Then, builds the streaming URLs.
        /// </summary>
        /// <param name="client">The Media Services client.</param>
        /// <param name="resourceGroupName">The name of the resource group within the Azure subscription.</param>
        /// <param name="accountName"> The Media Services account name.</param>
        /// <param name="locatorName">The name of the StreamingLocator that was created.</param>
        /// <returns></returns>
        // <GetStreamingURLs>
        private IList<string> GetStreamingURLs(
            IAzureMediaServicesClient client,
            string resourceGroupName,
            string accountName,
            String locatorName)
        {
            IList<string> streamingURLs = new List<string>();

            string streamingUrlPrefx = "";

            StreamingEndpoint streamingEndpoint = client.StreamingEndpoints.Get(resourceGroupName, accountName, "default");

            if (streamingEndpoint != null)
            {
                streamingUrlPrefx = streamingEndpoint.HostName;

                if (streamingEndpoint.ResourceState != StreamingEndpointResourceState.Running)
                    client.StreamingEndpoints.Start(resourceGroupName, accountName, "default");
            }

            foreach (var path in client.StreamingLocators.ListPaths(resourceGroupName, accountName, locatorName).StreamingPaths)
            {
                if (path.Paths.Count > 0)
                {
                    streamingURLs.Add("https://" + streamingUrlPrefx + path.Paths[0].ToString());
                }
            }

            return streamingURLs;
        }
        // </GetStreamingURLs>


        /// <summary>
        /// Creates a StreamingLocator for the specified asset and with the specified streaming policy name.
        /// Once the StreamingLocator is created the output asset is available to clients for playback.
        /// </summary>
        /// <param name="client">The Media Services client.</param>
        /// <param name="resourceGroupName">The name of the resource group within the Azure subscription.</param>
        /// <param name="accountName"> The Media Services account name.</param>
        /// <param name="assetName">The name of the output asset.</param>
        /// <param name="locatorName">The StreamingLocator name (unique in this case).</param>
        /// <returns></returns>
        // <CreateStreamingLocator>
        private StreamingLocator CreateStreamingLocator(IAzureMediaServicesClient client,
                                                                string resourceGroup,
                                                                string accountName,
                                                                string assetName,
                                                                string locatorName)
        {
            StreamingLocator locator =
                client.StreamingLocators.Create(resourceGroup,
                accountName,
                locatorName,
                new StreamingLocator()
                {
                    AssetName = assetName,
                    StreamingPolicyName = PredefinedStreamingPolicy.ClearStreamingOnly,
                });

            return locator;
        }
        // </CreateStreamingLocator>
        #endregion
    }
}