namespace ServiceNowCLI.Config.Dtos
{
    public class AzureDevOpsSettings
    {
        public string BaseUrl { get; set; }

        public string CollectionUrlCloudIndicator { get; set; }

        public string OrgName { get; set; }

        public string TenantId { get; set; }

        public string ClientId { get; set; }

        public string Scope { get; set; }

        public string ClientSecret { get; set; }

        public string OrganizationUrl => $"{BaseUrl}/{OrgName}/";

        public string ServiceNowApiSubscriptionId { get; set; }
    }
}
