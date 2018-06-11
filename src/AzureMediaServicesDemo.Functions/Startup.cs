using AzureMediaServicesDemo.Shared;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

namespace AzureMediaServicesDemo.Functions
{
    public class Startup
    {
        private readonly ILogger<Startup> _logger;

        public Startup(ILogger<Startup> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton(typeof(ConfigWrapper));
            services.AddTransient<IStorageHelper, StorageHelper>();
            services.AddTransient<IAzureMediaServicesHelper, AzureMediaServicesHelper>();
            services.AddTransient<IVideoService, VideoService>();
        }

        public void Configure(IConfigurationBuilder app)
        {
            var executingAssembly = new FileInfo(Assembly.GetExecutingAssembly().Location);
            _logger.LogInformation($"Using \"{executingAssembly.Directory.FullName}\" as base path to load configuration files.");
            app
                .SetBasePath(executingAssembly.Directory.FullName)
                .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables();
        }

    }

}
