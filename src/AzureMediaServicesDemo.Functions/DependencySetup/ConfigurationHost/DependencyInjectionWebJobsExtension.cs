using AzureMediaServicesDemo.Injection;
using Microsoft.Azure.WebJobs;

namespace AzureMediaServicesDemo.ConfigurationHost
{
    public static class DependencyInjectionWebJobsBuilderExtensions
    {
        public static IWebJobsBuilder AddDependencyInjection(this IWebJobsBuilder builder)
        {
            builder.AddExtension(typeof(InjectWebJobsExtension));
            return builder;
        }
    }
}