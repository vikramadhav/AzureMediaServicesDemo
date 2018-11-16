using AzureMediaServicesDemo.Shared;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Threading.Tasks;
using Willezone.Azure.WebJobs.Extensions.DependencyInjection;

namespace AzureMediaServicesDemo
{
    public static class ProcessVideo
    {
        [FunctionName("ProcessVideo")]
        public async static Task Run([BlobTrigger("input/{name}", Connection = "AzureWebJobsStorage")]Stream myBlob, string name, [Inject]ILogger log, [Inject] IVideoService videoService)
        {
            log.LogInformation($"C# Blob trigger function Processing blob\n Name:{name} \n Size: {myBlob.Length} Bytes");
            await videoService.AddVideo(name);
            log.LogInformation($"C# Blob trigger function Processed");
        }
    }
}