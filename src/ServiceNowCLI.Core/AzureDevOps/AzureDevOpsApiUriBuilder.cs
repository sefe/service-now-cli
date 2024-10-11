using ServiceNowCLI.Config.Dtos;

namespace ServiceNowCLI.Core.AzureDevOps
{
    public static class AzureDevOpsApiUriBuilder
    {
        public static string GetResourceAreasUri(AzureDevOpsSettings adoSettings, string resourceGuid)
        {
            return $"{adoSettings.OrganizationUrl}/_apis/resourceAreas/{resourceGuid}?api-preview=5.0-preview.1";
        }

        public static string GetUriForReleaseId(string locationUrl, string teamProjectName, string releaseId)
        {
            return $"{locationUrl}/{teamProjectName}/_apis/release/releases/{releaseId}?api-version=5.1";
        }

        public static string GetUriForBuildId(string locationUrl, string teamProjectName, string buildId)
        {
            var endOfUri = $"{teamProjectName}/_apis/build/builds/{buildId}?api-version=6.0";

            return $"{locationUrl}/{endOfUri}";
        }

        public static string GetUriForWorkItemId(string locationUrl, string teamProjectName, string workItemId)
        {
            var endOfUri = $"{teamProjectName}/_apis/wit/workitems/{workItemId}?api-version=5.1";

            return $"{locationUrl}/{endOfUri}";
        }
    }
}
