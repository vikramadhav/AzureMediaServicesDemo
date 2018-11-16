using System;
using System.Linq;
using AzureMediaServicesDemo.Injection.Internal;
using Microsoft.Azure.WebJobs.Host.Bindings;
using Microsoft.Azure.WebJobs.Host.Config;
using Microsoft.Azure.WebJobs.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AzureMediaServicesDemo.Injection
{
    internal class InjectWebJobsExtension : IExtensionConfigProvider
    {
        private ILogger _logger;
        public InjectWebJobsExtension(ILoggerFactory loggerFactory)
        {
            _loggerFactory = loggerFactory;
        }
        private readonly ILoggerFactory _loggerFactory;
        public void Initialize(ExtensionConfigContext context)
        {
            _logger = _loggerFactory.CreateLogger(LogCategories.CreateTriggerCategory("Blob"));

            var rule = context.AddBindingRule<InjectAttribute>();

            rule.BindToInput<Anonymous>(attribute => null);

            var dependencyConfig = InitializeContainer(context);

            if (dependencyConfig == null) return;

            var serviceCollection = new ServiceCollection();
            dependencyConfig.ConfigureServices(serviceCollection);

            var container = serviceCollection.BuildServiceProvider();
            rule.AddOpenConverter<Anonymous, OpenType>(typeof(InjectConverter<>), container);
        }

        private static IDependencyConfiguration InitializeContainer(ExtensionConfigContext context)
        {
            var configType = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(x => x.GetTypes())
                .FirstOrDefault(x =>
                    typeof(IDependencyConfiguration).IsAssignableFrom(x) && !x.IsInterface && !x.IsAbstract);

            IDependencyConfiguration configuration = null;

            if (configType == null) return configuration;

            var configInstance = Activator.CreateInstance(configType);
            configuration = (IDependencyConfiguration) configInstance;

            return configuration;
        }
    }
}