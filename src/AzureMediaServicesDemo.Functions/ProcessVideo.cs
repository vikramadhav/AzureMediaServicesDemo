using AzureMediaServicesDemo.Shared;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Threading.Tasks;

namespace AzureMediaServicesDemo
{
    public static class ProcessVideo
    {
        [FunctionName("ProcessVideo")]
        public async static Task Run([BlobTrigger("input/{name}", Connection = "AzureWebJobsStorage")]Stream myBlob, string name, [Inject(typeof(IVideoService))]IVideoService something,
            ILogger log)
        {
            log.LogInformation($"C# Blob trigger function Processing blob\n Name:{name} \n Size: {myBlob.Length} Bytes");
            await something.AddVideo(name);
            log.LogInformation($"C# Blob trigger function Processed");
        }
    }
}