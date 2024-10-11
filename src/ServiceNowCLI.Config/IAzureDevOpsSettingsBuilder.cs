using ServiceNowCLI.Config.Dtos;

namespace ServiceNowCLI.Config
{
    public interface IAzureDevOpsSettingsBuilder
    {
        AzureDevOpsSettings GetSettings(bool useDefaultCredentials);
    }
}
