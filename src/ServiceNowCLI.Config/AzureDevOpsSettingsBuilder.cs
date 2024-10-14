using System;
using ServiceNowCLI.Config.Dtos;
using System.Configuration;

namespace ServiceNowCLI.Config
{
    public class AzureDevOpsSettingsBuilder(IDorcConfigProvider dorcConfigProvider) : IAzureDevOpsSettingsBuilder
    {
        public AzureDevOpsSettings GetSettings()
        {
            var dorcPropertyName = ConfigurationManager.AppSettings["DorcPropertyNameForClientSecret"];

            var clientSecret = dorcConfigProvider.GetDorcPropertyValue(dorcPropertyName);

            if (string.IsNullOrEmpty(clientSecret)) throw new ArgumentException($"Failed to get AzureDevOps Client Secret from Dorc - DorcPropertyName={dorcPropertyName}");

            var organizationUrl = ConfigurationManager.AppSettings["AdoOrganizationUrl"];

            if (string.IsNullOrEmpty(organizationUrl)) throw new ArgumentException($"AdoOrganizationUrl is not set in config file");

            bool useDefaultCreds = !organizationUrl.Contains(ConfigurationManager.AppSettings["AdoCollectionUrlCloudIndicator"], StringComparison.OrdinalIgnoreCase);

            var settings = new AzureDevOpsSettings
            {
                ClientId = ConfigurationManager.AppSettings["AadClientId"],
                Scope = ConfigurationManager.AppSettings["AadScope"],
                TenantId = ConfigurationManager.AppSettings["AadTenantId"],
                OrganizationUrl = organizationUrl,
                ClientSecret = clientSecret,
                UseDefaultCredentials = useDefaultCreds,
            };

            return settings;
        }
    }
}
