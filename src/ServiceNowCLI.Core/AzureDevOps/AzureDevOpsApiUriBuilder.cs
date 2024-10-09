using ServiceNowCLI.Config.Dtos;
using System;

namespace ServiceNowCLI.Core.AzureDevOps
{
    public static class AzureDevOpsApiUriBuilder
    {
        public static string GetUriForReleaseId(string collectionUri, string teamProjectName, string releaseId)
        {
            // collectionUri is usually correct for both on-prem and cloud AzureDevOps. For cloud it is e.g. vsrm.dev.azure.com, which is the correct REST API for release definitions
            return $"{collectionUri}{teamProjectName}/_apis/release/releases/{releaseId}?api-version=5.1";
        }

        public static string GetUriForBuildId(string collectionUri, AzureDevOpsSettings adoSettings, string teamProjectName, string buildId)
        {
            var endOfUri = $"{teamProjectName}/_apis/build/builds/{buildId}?api-version=6.0";

            // collectionUri is usually only correct for on-prem AzureDevOps, for cloud it is e.g. vsrm.dev.azure.com, but this is only correct for release definitions
            if (collectionUri.Contains(adoSettings.CollectionUrlCloudIndicator, StringComparison.OrdinalIgnoreCase))
            {
                return $"{adoSettings.OrganizationUrl}/{endOfUri}";
            }

            return $"{collectionUri}{endOfUri}";
        }

        public static string GetUriForWorkItemId(string collectionUri, AzureDevOpsSettings adoSettings, string teamProjectName, string workItemId)
        {
            var endOfUri = $"{teamProjectName}/_apis/wit/workitems/{workItemId}?api-version=5.1";

            // collectionUri is usually only correct for on-prem AzureDevOps, for cloud it is e.g. vsrm.dev.azure.com, but this is only correct for release definitions
            if (collectionUri.Contains(adoSettings.CollectionUrlCloudIndicator, StringComparison.OrdinalIgnoreCase))
            {
                return $"{adoSettings.OrganizationUrl}/{endOfUri}";
            }

            return $"{collectionUri}{endOfUri}";
        }
    }
}
