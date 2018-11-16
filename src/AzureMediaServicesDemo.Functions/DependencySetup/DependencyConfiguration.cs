using AzureMediaServicesDemo.Injection;
using AzureMediaServicesDemo.Shared;
using Microsoft.Extensions.DependencyInjection;

namespace AzureMediaServicesDemo
{
    public class DependencyConfiguration : IDependencyConfiguration
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton(typeof(ConfigWrapper));
            services.AddTransient<IStorageHelper, StorageHelper>();
            services.AddTransient<IAzureMediaServicesHelper, AzureMediaServicesHelper>();
            services.AddTransient<IVideoService, VideoService>();

        }
    }
}