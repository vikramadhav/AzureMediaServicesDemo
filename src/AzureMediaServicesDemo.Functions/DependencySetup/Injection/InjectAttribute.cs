using System;
using Microsoft.Azure.WebJobs.Description;

namespace AzureMediaServicesDemo.Injection
{
    [AttributeUsage(AttributeTargets.Parameter)]
    [Binding]
    public class InjectAttribute : Attribute
    {
    }
}