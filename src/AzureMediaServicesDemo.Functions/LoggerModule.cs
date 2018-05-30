using Autofac;
using Autofac.Core;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Linq;

namespace AzureMediaServicesDemo
{
    public class LoggerModule : Module
    {
        private static readonly ConcurrentDictionary<Type, object> _logCache = new ConcurrentDictionary<Type, object>();

        private interface ILoggerWrapper
        {
            object Create(ILoggerFactory factory);
        }

        protected override void AttachToComponentRegistration(
            IComponentRegistry componentRegistry,
            IComponentRegistration registration)
        {
            // Handle constructor parameters.
            registration.Preparing += OnComponentPreparing;
        }

        private static object GetLogger(IComponentContext context, Type declaringType)
        {
            return _logCache.GetOrAdd(
                declaringType,
                x =>
                {
                    var factory = context.Resolve<ILoggerFactory>();
                    var loggerName = "Function." + declaringType.FullName + ".User";

                    return factory.CreateLogger(loggerName);
                });
        }

        private static void OnComponentPreparing(object sender, PreparingEventArgs e)
        {
            var t = e.Component.Activator.LimitType;

            if (t.FullName.EndsWith("[]", StringComparison.OrdinalIgnoreCase))
            {
                // Ignore IEnumerable types
                return;
            }

            e.Parameters = e.Parameters.Union(
                new[]
                {
                    new ResolvedParameter((p, i) => p.ParameterType == typeof(ILogger), (p, i) => GetLogger(i, t))
                });
        }
    }
}
