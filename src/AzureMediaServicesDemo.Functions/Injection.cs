using Autofac;
using AzureMediaServicesDemo.Shared;
using Microsoft.Azure.WebJobs.Description;
using Microsoft.Azure.WebJobs.Host.Config;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Reflection;

namespace AzureMediaServicesDemo
{
    [Binding]
    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
    public class InjectAttribute : Attribute
    {
        public InjectAttribute(Type type)
        {
            Type = type;
        }

        public Type Type { get; }
    }

    public class InjectConfiguration : IExtensionConfigProvider
    {
        private static readonly object _syncLock = new object();
        private static IContainer _container;

        public void Initialize(ExtensionConfigContext context)
        {
            InitializeContainer(context);

            context
                .AddBindingRule<InjectAttribute>()
                .BindToInput<dynamic>(i => _container.Resolve(i.Type));
        }

        private void InitializeContainer(ExtensionConfigContext context)
        {
            if (_container != null)
            {
                return;
            }

            lock (_syncLock)
            {
                if (_container != null)
                {
                    return;
                }

                _container = ContainerConfig.BuildContainer(context.Config.LoggerFactory);
            }
        }
    }

    public static class ContainerConfig
    {
        public static IContainer BuildContainer(ILoggerFactory factory)
        {
            var builder = new ContainerBuilder();

            var assemblyTypes = Assembly.GetExecutingAssembly().GetTypes().Where(x => x.GetInterfaces().Any()).ToArray();
            builder.RegisterTypes(assemblyTypes).AsImplementedInterfaces();

            builder.RegisterType<StorageHelper>().As<IStorageHelper>();
            builder.RegisterType<AzureMediaServicesHelper>().As<IAzureMediaServicesHelper>();
            builder.RegisterType<VideoService>().As<IVideoService>();

            builder.RegisterInstance(new ConfigWrapper()).As<ConfigWrapper>();
            builder.RegisterInstance(factory).As<ILoggerFactory>();
            builder.RegisterModule<LoggerModule>();

            return builder.Build();
        }
    }
}
