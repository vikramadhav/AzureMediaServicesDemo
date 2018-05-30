using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AzureMediaServicesDemo.Shared
{
    /// <summary>
    /// Wrapper for Environment Variables
    /// </summary>
    /// <remarks>
    /// This class represents a Strongly-Typed instance of Environment Variables, set in Azure Portal
    /// </remarks>
    public class ConfigWrapper
    {

        public string StorageConnectionString
        {
            get { return Environment.GetEnvironmentVariable("StorageConnectionString"); }
        }
        public string SubscriptionId
        {
            get { return Environment.GetEnvironmentVariable("SubscriptionId"); }
        }

        public string ResourceGroup
        {
            get { return Environment.GetEnvironmentVariable("ResourceGroup"); }
        }

        public string AccountName
        {
            get { return Environment.GetEnvironmentVariable("AccountName"); }
        }

        public string AadTenantId
        {
            get { return Environment.GetEnvironmentVariable("AadTenantId"); }
        }

        public string AadClientId
        {
            get { return Environment.GetEnvironmentVariable("AadClientId"); }
        }

        public string AadSecret
        {
            get { return Environment.GetEnvironmentVariable("AadSecret"); }
        }

        public Uri ArmAadAudience
        {
            get { return new Uri(Environment.GetEnvironmentVariable("ArmAadAudience")); }
        }

        public Uri AadEndpoint
        {
            get { return new Uri(Environment.GetEnvironmentVariable("AadEndpoint")); }
        }

        public Uri ArmEndpoint
        {
            get { return new Uri(Environment.GetEnvironmentVariable("ArmEndpoint")); }
        }

        public string Region
        {
            get { return Environment.GetEnvironmentVariable("Region"); }
        }
    }
}
