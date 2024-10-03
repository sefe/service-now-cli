using System;
using ServiceNowCLI.Config.Dtos;
using System.Configuration;

namespace ServiceNowCLI.Config
{
    public class AzureDevOpsSettingsBuilder : IAzureDevOpsSettingsBuilder
    {
        private readonly IDorcConfigProvider _dorcConfigProvider;

        public AzureDevOpsSettingsBuilder(IDorcConfigProvider dorcConfigProvider)
        {
            _dorcConfigProvider = dorcConfigProvider;
        }

        public AzureDevOpsSettings GetSettings()
        {
            var dorcPropertyName = ConfigurationManager.AppSettings["DorcPropertyNameForClientSecret"];

            var clientSecret = _dorcConfigProvider.GetDorcPropertyValue(dorcPropertyName);

            if (string.IsNullOrEmpty(clientSecret)) throw new ArgumentException($"Failed to get AzureDevOps Client Secret from Dorc - DorcPropertyName={dorcPropertyName}");

            var settings = new AzureDevOpsSettings
            {
                ClientId = ConfigurationManager.AppSettings["AadClientId"],
                Scope = ConfigurationManager.AppSettings["AadScope"],
                TenantId = ConfigurationManager.AppSettings["AadTenantId"],
                BaseUrl = ConfigurationManager.AppSettings["AdoBaseUrl"],
                CollectionUrlCloudIndicator = ConfigurationManager.AppSettings["AdoCollectionUrlCloudIndicator"],
                OrgName = ConfigurationManager.AppSettings["AdoOrgName"],
                ClientSecret = clientSecret,
                ServiceNowApiSubscriptionId = ConfigurationManager.AppSettings["serviceNowApiSubscriptionId"],
            };

            return settings;
        }
    }
}
