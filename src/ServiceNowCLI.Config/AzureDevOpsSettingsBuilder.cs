using System;
using ServiceNowCLI.Config.Dtos;
using System.Configuration;

namespace ServiceNowCLI.Config
{
    public class AzureDevOpsSettingsBuilder(IDorcConfigProvider dorcConfigProvider) : IAzureDevOpsSettingsBuilder
    {
        public AzureDevOpsSettings GetSettings(bool useDefaultCredentials)
        {
            var dorcPropertyName = ConfigurationManager.AppSettings["DorcPropertyNameForClientSecret"];

            var clientSecret = dorcConfigProvider.GetDorcPropertyValue(dorcPropertyName);

            if (string.IsNullOrEmpty(clientSecret)) throw new ArgumentException($"Failed to get AzureDevOps Client Secret from Dorc - DorcPropertyName={dorcPropertyName}");

            var settings = new AzureDevOpsSettings
            {
                ClientId = ConfigurationManager.AppSettings["AadClientId"],
                Scope = ConfigurationManager.AppSettings["AadScope"],
                TenantId = ConfigurationManager.AppSettings["AadTenantId"],
                OrganizationUrl = ConfigurationManager.AppSettings["AdoOrganizationUrl"],
                ClientSecret = clientSecret,
                UseDefaultCredentials = useDefaultCredentials,
            };

            return settings;
        }
    }
}
