using Microsoft.Azure.WebJobs.Host.Config;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace AzureMediaServicesDemo.Functions.Injection
{
    public class InjectConfiguration : IExtensionConfigProvider
    {
        private static readonly object SyncLock = new object();
        private static IServiceProvider _provider;

        private readonly ILoggerFactory factory;

        public InjectConfiguration(ILoggerFactory _factory)
        {
            factory = _factory;
        }


        public void Initialize(ExtensionConfigContext context)
        {
            Configure(context);

            context
                .AddBindingRule<InjectAttribute>()
                .BindToInput<dynamic>(i => _provider.GetRequiredService(i.Type));
        }

        private void Configure(ExtensionConfigContext context)
        {
            if (_provider != null)
            {
                return;
            }

            lock (SyncLock)
            {
                if (_provider != null)
                {
                    return;
                }


                _provider = ServiceProviderConfiguration.Configure(context, factory);
            }
        }
    }

}
