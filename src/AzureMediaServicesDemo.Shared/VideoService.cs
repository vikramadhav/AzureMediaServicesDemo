using Microsoft.WindowsAzure.Storage.Table;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Management.Media;
using Microsoft.Azure.Management.Media.Models;

namespace AzureMediaServicesDemo.Shared
{
    public interface IVideoService
    {
        Task<IQueryable<Video>> GetVideos();
        Task AddVideo(string fileName);
    }

    /// <summary>
    /// Service to Handle Interactions with Azure Media Services and Azure Storage
    /// </summary>
    /// <remarks>
    /// This class allows the facilitation of adding and retrieving videos from the Video Repo. Video
    /// metadata is stored in Azure Table Storage. Information stored is Video Name and Streaming
    /// endpoint in Azure Media Services.
    /// </remarks>
    public class VideoService : IVideoService
    {
        private readonly ILogger<VideoService> _log;
        private readonly ConfigWrapper _config;

        private readonly IAzureMediaServicesHelper _amsHelpers;
        private readonly IStorageHelper _storageHelpers;

        /// <summary>
        /// Initializes a new instance of the <see cref="VideoService"/> class.
        /// </summary>
        /// <param name="log">Instance of ILogger resolved via DI.</param>
        /// <param name="config">Instance of ConfigWrapper which is Env Variables.</param>
        /// <param name="amsHelper">Instance of IAzureMediaServicesHelpers service to interact with AMS</param>
        /// <param name="storageHelper">Instance of IStorageHelper service to interact with Azure Storage</param>
        public VideoService(ILogger<VideoService> log, ConfigWrapper config, IAzureMediaServicesHelper amsHelper, IStorageHelper storageHelper)
        {
            _log = log;
            _config = config;
            _amsHelpers = amsHelper;
            _storageHelpers = storageHelper;
        }

        /// <summary>
        /// Accepts filename of video to process with Azure Media Services
        /// </summary>
        /// <param name="fileName">Filename of Video to process in AMS</param>
        /// <returns>
        /// Task that will complete when processing has completed.
        /// </returns>
        public async Task AddVideo(string fileName)
        {
            try
            {
                IAzureMediaServicesClient client = _amsHelpers.CreateMediaServicesClient();

                // Set the polling interval for long running operations to 2 seconds.
                // The default value is 30 seconds for the .NET client SDK
                client.LongRunningOperationRetryTimeout = 2;

                var videos = await GetVideos();

                if (!videos.Any(a => a.Name == fileName))
                {
                    await _amsHelpers.ProcessVideo(client, fileName);
                }
                else
                {
                    await _storageHelpers.DeleteBlobAsync(fileName);
                    _log.LogInformation($"Video: {fileName} has already been processed.");
                }
            }
            catch (ApiErrorException ex)
            {
                _log.LogError(ex.Message);
                _log.LogError("ERROR:API call failed with error code: {0} and message: {1}",
                    ex.Body.Error.Code, ex.Body.Error.Message);
            }
        }

        /// <summary>
        /// Gets list of videos from Azure Table Storage
        /// </summary>
        /// <returns>
        /// Task that will complete when processing has completed.
        /// </returns>
        public async Task<IQueryable<Video>> GetVideos()
        {
            TableContinuationToken token = null;
            var entities = new List<Video>();
            do
            {
                var queryResult = await _storageHelpers.GetTable().ExecuteQuerySegmentedAsync(new TableQuery<Video>(), token);
                entities.AddRange(queryResult.Results);
                token = queryResult.ContinuationToken;

            } while (token != null);

            return entities.AsQueryable();
        }
    }
}