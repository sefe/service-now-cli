namespace ServiceNowCLI.Config.Dtos
{
    public class AzureDevOpsSettings
    {
        public string TenantId { get; set; }

        public string ClientId { get; set; }

        public string Scope { get; set; }

        public string ClientSecret { get; set; }

        public string OrganizationUrl { get; set; }

        public bool UseDefaultCredentials { get; set; }
    }
}
