using Microsoft.Extensions.DependencyInjection;

namespace AzureMediaServicesDemo.Injection
{
    public interface IDependencyConfiguration
    {
        void ConfigureServices(IServiceCollection services);
    }
}