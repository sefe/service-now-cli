using Newtonsoft.Json;
using System.Collections.Generic;

namespace ServiceNowCLI.Core.SecurityValidator
{
    public class AutoCrScanRequest
    {
        [JsonProperty("teamProjectName")]
        public string TeamProjectName { get; set; }

        [JsonProperty("buildId")]
        public int? BuildId { get; set; }

        [JsonProperty("buildNumber")]
        public string BuildNumber { get; set; }

        [JsonProperty("repositoryId")]
        public string RepositoryId { get; set; }

        [JsonProperty("repositoryName")]
        public string RepositoryName { get; set; }

        [JsonProperty("sourceBranch")]
        public string SourceBranch { get; set; }

        [JsonProperty("issuePathFilter")]
        public string IssuePathFilter { get; set; }

        [JsonProperty("environment")]
        public string Environment { get; set; }

        [JsonProperty("workItemIds")]
        public List<int> WorkItemIds { get; set; } = new List<int>();

        [JsonProperty("shortDescription")]
        public string ShortDescription { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("justification")]
        public string Justification { get; set; }

        [JsonProperty("implementationPlan")]
        public string ImplementationPlan { get; set; }

        [JsonProperty("configurationItem")]
        public string ConfigurationItem { get; set; }

        [JsonProperty("category")]
        public string Category { get; set; }

        [JsonProperty("requestedBy")]
        public string RequestedBy { get; set; }
    }

    public class AutoCrScanFinding
    {
        [JsonProperty("ruleCode")]
        public string RuleCode { get; set; }

        [JsonProperty("severity")]
        public string Severity { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("details")]
        public string Details { get; set; }

        [JsonProperty("actionRequired")]
        public string ActionRequired { get; set; }
    }

    public class AutoCrRepoAuditSummary
    {
        [JsonProperty("aikidoRepoId")]
        public int AikidoRepoId { get; set; }

        [JsonProperty("repoName")]
        public string RepoName { get; set; }

        [JsonProperty("branch")]
        public string Branch { get; set; }

        [JsonProperty("dashboardUrl")]
        public string DashboardUrl { get; set; }

        [JsonProperty("openCriticalCount")]
        public int OpenCriticalCount { get; set; }

        [JsonProperty("openHighCount")]
        public int OpenHighCount { get; set; }

        [JsonProperty("openMediumCount")]
        public int OpenMediumCount { get; set; }

        [JsonProperty("openLowCount")]
        public int OpenLowCount { get; set; }
    }

    public class AutoCrScanResponse
    {
        [JsonProperty("passed")]
        public bool Passed { get; set; }

        [JsonProperty("outcomeHash")]
        public string OutcomeHash { get; set; }

        [JsonProperty("formattedServiceNowCommentary")]
        public string FormattedServiceNowCommentary { get; set; }

        [JsonProperty("findings")]
        public List<AutoCrScanFinding> Findings { get; set; } = new List<AutoCrScanFinding>();

        [JsonProperty("repoAudits")]
        public List<AutoCrRepoAuditSummary> RepoAudits { get; set; } = new List<AutoCrRepoAuditSummary>();
    }
}
