using AzureMediaServicesDemo.Functions;
using AzureMediaServicesDemo.Shared;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Hosting;
using Microsoft.Azure.WebJobs.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using Willezone.Azure.WebJobs.Extensions.DependencyInjection;

//[assembly: WebJobsStartup(typeof(Startup), "A Web Jobs Extension Sample")]

//namespace AzureMediaServicesDemo.Functions
//{
//    public class Startup
//    {
//        private readonly ILogger<Startup> _logger;

//        public Startup(ILogger<Startup> logger)
//        {
//            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
//        }
//        public void ConfigureServices(IServiceCollection services)
//        {
//            services.AddSingleton(typeof(ConfigWrapper));
//            services.AddTransient<IStorageHelper, StorageHelper>();
//            services.AddTransient<IAzureMediaServicesHelper, AzureMediaServicesHelper>();
//            services.AddTransient<IVideoService, VideoService>();
//        }

//        public void Configure(IConfigurationBuilder app)
//        {
//            var executingAssembly = new FileInfo(Assembly.GetExecutingAssembly().Location);
//            _logger.LogInformation($"Using \"{executingAssembly.Directory.FullName}\" as base path to load configuration files.");
//            app
//                .SetBasePath(executingAssembly.Directory.FullName)
//                .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
//                .AddEnvironmentVariables();
//        }

//    }

//}
[assembly: WebJobsStartup(typeof(Startup))]
namespace AzureMediaServicesDemo.Functions
{
    internal class Startup : IWebJobsStartup
    {
        public void Configure(IWebJobsBuilder builder) =>
            builder.AddDependencyInjection<ServiceProviderBuilder>();
    }

    internal class ServiceProviderBuilder : IServiceProviderBuilder
    {
        private readonly ILoggerFactory _loggerFactory;

        public ServiceProviderBuilder(ILoggerFactory loggerFactory) =>
            _loggerFactory = loggerFactory;

        public IServiceProvider Build()
        {
            var services = new ServiceCollection();
            services.AddSingleton(typeof(ConfigWrapper));

            services.AddSingleton<ILogger>(_ => _loggerFactory.CreateLogger(LogCategories.CreateFunctionUserCategory("Common")));

            services.AddTransient<IStorageHelper, StorageHelper>();
            services.AddTransient<IAzureMediaServicesHelper, AzureMediaServicesHelper>();
            services.AddTransient<IVideoService, VideoService>();
            // Important: We need to call CreateFunctionUserCategory, otherwise our log entries might be filtered out.

            return services.BuildServiceProvider();
        }
    }
}


