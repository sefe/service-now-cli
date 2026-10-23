using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Azure.Pipelines.WebApi;
using Microsoft.TeamFoundation.Build.WebApi;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using Microsoft.VisualStudio.Services.ReleaseManagement.WebApi;
using Microsoft.VisualStudio.Services.WebApi;
using Newtonsoft.Json;
using RestSharp;
using ServiceNowCLI.Config.Dtos;

namespace ServiceNowCLI.Core.AzureDevOps
{
    public class BuildLogic : BaseLogic
    {
        private readonly BuildHttpClient _buildsClient;
        private readonly string teamProjectName;
        private readonly VssConnection vssConnection;

        public BuildLogic(
            string teamProjectName,
            AzureDevOpsSettings adoSettings,
            IAzureDevOpsTokenHandler tokenHandler,
            VssConnection vssConnection) : base(adoSettings, tokenHandler)
        {
            this.teamProjectName = teamProjectName;
            this.vssConnection = vssConnection;

            _buildsClient = vssConnection.GetClient<BuildHttpClient>();
        }

        public IEnumerable<Build> GetRecentBuilds(int howManyMonthsAgo, string buildNumber=null)
        {
            Console.WriteLine($"Getting recent builds... starting getting last {howManyMonthsAgo} months");

            string continuationToken = null;
            List<BuildDefinitionReference> buildDefinitions = [];

            do
            {
                IPagedList<BuildDefinitionReference> buildsPage = _buildsClient.GetDefinitionsAsync2(
                    project: teamProjectName,
                    builtAfter: DateTime.Today.AddMonths(howManyMonthsAgo * -1),
                    continuationToken: continuationToken).Result;

                buildDefinitions.AddRange(buildsPage);

                continuationToken = buildsPage.ContinuationToken;

            } while (!string.IsNullOrEmpty(continuationToken));

            var builds = new List<Build>();

            var definitionIds = buildDefinitions.Select(d => d.Id).ToArray();
            do
            {
                IPagedList<Build> buildsPage = _buildsClient.GetBuildsAsync2(
                    buildNumber: buildNumber,
                    project: teamProjectName,
                    definitions: definitionIds,
                    continuationToken: continuationToken).Result;

                builds.AddRange(buildsPage);

                continuationToken = buildsPage.ContinuationToken;

            } while (!string.IsNullOrEmpty(continuationToken));
            

            Console.WriteLine($"Getting recent builds... finished. {builds.Count} builds returned");

            return builds;
        }

        public Pipeline GetPipeline(string teamProjectName, int pipelineId)
        {
            return vssConnection.GetClient<PipelinesHttpClient>().GetPipelineAsync(teamProjectName, pipelineId).GetAwaiter().GetResult();
        }

        private RestClient GetBuildClientForBuildId(string buildId)
        {
            var buildLocationUrl = GetResourceLocationUrl(AdoResources.build);
            var clientUri = AzureDevOpsApiUriBuilder.GetUriForBuildId(buildLocationUrl, teamProjectName, buildId);

            return GetClient(clientUri);
        }

        public Build GetBuildForId(string buildId)
        {
            var buildClient = GetBuildClientForBuildId(buildId);
            var build = GetBuild(buildClient);
            return build;
        }

        private Build GetBuild(RestClient buildClient)
        {
            var uri = buildClient.Options.BaseUrl;
            var getRequest = new RestRequest(uri);
            var getResponse = buildClient.Execute(getRequest);

            if (!getResponse.IsSuccessful)
            {
                var fullResponseJson = JsonConvert.SerializeObject(getResponse);
                Console.WriteLine($"GET request was not successful. FullResponse={fullResponseJson}");
                throw new ArgumentException(getResponse.ErrorMessage);
            }

            var build = JsonConvert.DeserializeObject<Build>(getResponse.Content);
            return build;
        }

        public Build GetBuildForBuildNumber(string buildNumber)
        {
            var recentBuilds = GetRecentBuilds(3, buildNumber);
            var build = recentBuilds.FirstOrDefault(b => b.BuildNumber == buildNumber);
            return build;
        }

        public List<int> GetBuildLinkedWorkItemIds(Build build)
        {
            Console.WriteLine($"Getting referenced work items from BuildNumber={build.BuildNumber}, BuildId={build.Id}");

            var workItemsRef = _buildsClient.GetBuildWorkItemsRefsAsync(build.Project.Id, build.Id).GetAwaiter().GetResult();

            var workItemIds = workItemsRef.Select(wi => int.Parse(wi.Id)).ToList();

            var scopedIds = TryScopeReleaseBranchWorkItems(build, workItemIds);
            if (scopedIds != null && scopedIds.Count > 0)
            {
                workItemIds = scopedIds;
            }

            var workItemsForConsole = string.Join(", ", workItemIds);
            Console.WriteLine($"{workItemIds.Count} referenced work items found for BuildNumber={build.BuildNumber}, BuildId={build.Id}: {workItemsForConsole}");

            return workItemIds;
        }

        private List<int> TryScopeReleaseBranchWorkItems(Build build, List<int> allBuildWorkItemIds)
        {
            try
            {
                var sourceBranch = build?.SourceBranch;
                var repoId = build?.Repository?.Id;
                if (string.IsNullOrWhiteSpace(sourceBranch) || string.IsNullOrWhiteSpace(repoId))
                {
                    return null;
                }

                var normalizedBranch = sourceBranch.StartsWith("refs/heads/", StringComparison.OrdinalIgnoreCase)
                    ? sourceBranch.Substring("refs/heads/".Length)
                    : sourceBranch.TrimStart('/');

                if (!IsReleaseLikeBranchName(normalizedBranch))
                {
                    return null;
                }

                var gitClient = vssConnection.GetClient<GitHttpClient>();
                var branches = gitClient.GetBranchesAsync(build.Project.Id, repoId).GetAwaiter().GetResult();
                var branchNames = branches
                    .Where(b => !string.IsNullOrWhiteSpace(b?.Name))
                    .Select(b => b.Name)
                    .ToList();

                var previousReleaseBranch = FindPreviousReleaseBranch(normalizedBranch, branchNames);
                if (string.IsNullOrWhiteSpace(previousReleaseBranch))
                {
                    return null;
                }

                var commitsInReleaseDiff = gitClient.GetCommitsBatchAsync(
                    new GitQueryCommitsCriteria
                    {
                        IncludeWorkItems = true,
                        ItemVersion = new GitVersionDescriptor
                        {
                            Version = previousReleaseBranch,
                            VersionType = GitVersionType.Branch
                        },
                        CompareVersion = new GitVersionDescriptor
                        {
                            Version = normalizedBranch,
                            VersionType = GitVersionType.Branch
                        }
                    },
                    project: build.Project.Id,
                    repositoryId: repoId,
                    top: 250).GetAwaiter().GetResult();

                var scopedSet = new HashSet<int>();
                foreach (var commit in commitsInReleaseDiff ?? [])
                {
                    if (commit?.WorkItems == null)
                    {
                        continue;
                    }

                    foreach (var wi in commit.WorkItems)
                    {
                        if (int.TryParse(wi?.Id, out var wiId) && wiId > 0)
                        {
                            scopedSet.Add(wiId);
                        }
                    }
                }

                if (scopedSet.Count > 0)
                {
                    var filtered = allBuildWorkItemIds.Where(id => scopedSet.Contains(id)).ToList();
                    var finalScoped = filtered.Count > 0 ? filtered : scopedSet.OrderBy(x => x).ToList();
                    Console.WriteLine(
                        $"Scoped release branch '{normalizedBranch}' against predecessor '{previousReleaseBranch}': " +
                        $"{finalScoped.Count} work items in release diff (reduced from {allBuildWorkItemIds.Count} cumulative build work items).");
                    return finalScoped;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[WARNING] Could not scope release branch work items by predecessor branch diff: {ex.Message}");
            }

            return null;
        }

        private static bool IsReleaseLikeBranchName(string normalizedBranch)
        {
            return normalizedBranch.StartsWith("releases/", StringComparison.OrdinalIgnoreCase) ||
                   normalizedBranch.StartsWith("release/", StringComparison.OrdinalIgnoreCase) ||
                   normalizedBranch.StartsWith("ReleaseCandidate/", StringComparison.OrdinalIgnoreCase) ||
                   normalizedBranch.StartsWith("hotfix/", StringComparison.OrdinalIgnoreCase);
        }

        internal static string FindPreviousReleaseBranch(string currentReleaseBranch, IEnumerable<string> allBranchNames)
        {
            if (!TryParseReleaseBranchVersion(currentReleaseBranch, out var currentPrefix, out var currentVersion))
            {
                return null;
            }

            string bestBranch = null;
            Version bestVersion = null;

            foreach (var rawBranch in allBranchNames)
            {
                var candidate = rawBranch.StartsWith("refs/heads/", StringComparison.OrdinalIgnoreCase)
                    ? rawBranch.Substring("refs/heads/".Length)
                    : rawBranch.TrimStart('/');

                if (candidate.Equals(currentReleaseBranch, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (!TryParseReleaseBranchVersion(candidate, out var candidatePrefix, out var candidateVersion))
                {
                    continue;
                }

                if (!candidatePrefix.Equals(currentPrefix, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (candidateVersion < currentVersion && (bestVersion == null || candidateVersion > bestVersion))
                {
                    bestVersion = candidateVersion;
                    bestBranch = candidate;
                }
            }

            return bestBranch;
        }

        private static bool TryParseReleaseBranchVersion(string branchName, out string prefix, out Version version)
        {
            prefix = string.Empty;
            version = new Version(0, 0);

            var normalized = branchName.StartsWith("refs/heads/", StringComparison.OrdinalIgnoreCase)
                ? branchName.Substring("refs/heads/".Length)
                : branchName.TrimStart('/');

            var match = Regex.Match(normalized, @"^(?<prefix>(?:releases?|ReleaseCandidate|hotfix)/(?:Release[-_]?|v)?)(?<ver>\d+(?:\.\d+){1,3})$", RegexOptions.IgnoreCase);
            if (!match.Success)
            {
                return false;
            }

            var verText = match.Groups["ver"].Value;
            if (!Version.TryParse(verText, out var parsed) || parsed == null)
            {
                return false;
            }

            prefix = match.Groups["prefix"].Value;
            version = parsed;
            return true;
        }

        public List<string> GetBranchesForTag(string tagName, string repoId)
        {
            var gitClient = vssConnection.GetClient<GitHttpClient>();

            var branches = gitClient.GetBranchesAsync(repoId, new GitVersionDescriptor
            {
                Version = tagName,
                VersionType = GitVersionType.Tag
            }).Result;

            return branches.Select(b => b.Name).ToList();           
        }
    }
}
