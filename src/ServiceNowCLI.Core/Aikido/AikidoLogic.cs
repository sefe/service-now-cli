using ServiceNowCLI.Core.Aikido.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ServiceNowCLI.Core.Aikido
{
    public class AikidoLogic
    {
        private readonly AikidoApiClient _apiClient;

        public AikidoLogic(string baseUrl, string clientId, string clientSecret)
        {
            _apiClient = new AikidoApiClient(baseUrl, clientId, clientSecret);
        }

        public bool GenerateIssuesReport(string repoName, string filename, string issuePathFilter = null, string branchName = null, string externalRepoId = null)
        {
            var issues = GetIssuesForRepo(repoName, out var repoId, issuePathFilter, branchName, externalRepoId);
            if (issues == null)
                return false;
            ReportGenerator.GeneratePdfReport(repoName, issues, filename, _apiClient.LinkToIssues(repoId));
            return true;
        }

        public bool GenerateIssuesReport(string repoName, Stream stream, string issuePathFilter = null, string branchName = null, string externalRepoId = null)
        {
            try
            {
                var issues = GetIssuesForRepo(repoName, out var repoId, issuePathFilter, branchName, externalRepoId);
                if (issues == null)
                    return false;

                ReportGenerator.GeneratePdfReport(repoName, issues, stream, _apiClient.LinkToIssues(repoId));
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error generating issues report from Aikido: {ex.Message}");
                return false;
            }
        }

        public List<Issue> GetIssuesForRepo(string repoName, out int repoId, string pathFilter = null, string branchName = null, string externalRepoId = null)
        {
            repoId = GetRepoId(repoName, branchName, externalRepoId);
            if (repoId == 0)
            {
                Console.WriteLine($"No issues found for '{repoName}' in Aikido.");
                return new List<Issue>();
            }

            var issues = _apiClient.ExportIssuesJson(filterCodeRepoId: repoId);
            Console.WriteLine($"Found {issues.Count} security issues for repository '{repoName}'.");

            if (!string.IsNullOrWhiteSpace(pathFilter))
            {
                Console.WriteLine($"filtering issues by path '{pathFilter}'..");
                issues = issues.Where(issue =>
                        string.IsNullOrEmpty(issue.affected_file) || issue.affected_file.StartsWith(pathFilter, StringComparison.OrdinalIgnoreCase)
                    ).ToList();
            }

            return issues.Where(iss => iss.status == IssueStatuses.Open).ToList();
        }

        private int GetRepoId(string repoName, string branchName = null, string externalRepoId = null)
        {
            try
            {
                var normalizedBranch = NormalizeBranchName(branchName);
                var catalogMatchId = ResolveRepoIdFromCatalog(repoName, normalizedBranch, externalRepoId);
                if (catalogMatchId != 0)
                {
                    Console.WriteLine($"Resolved Aikido repository ID {catalogMatchId} from catalog for '{repoName}' (branch: '{normalizedBranch ?? "default"}').");
                    return catalogMatchId;
                }

                int currentPage = 0;
                int perPage = 100;
                int repoId = 0; // To store the repository ID once we find it

                while (true)
                {
                    // Fetch issue groups for the current page
                    var issueGroups = _apiClient.GetIssueGroups(page: currentPage, perPage: perPage, filterCodeRepoName: repoName);

                    // Check if there are any issue groups in the response
                    if (issueGroups == null || issueGroups.Count == 0)
                    {
                        Console.WriteLine($"No issue groups found for repo {repoName}");
                        break;
                    }

                    // Search for the repository ID in the locations of the issue groups
                    repoId = issueGroups
                        .SelectMany(group => group.Locations) // Flatten the locations from all issue groups
                        .Where(location => location.Name.Equals(repoName, StringComparison.OrdinalIgnoreCase))
                        .Select(location => location.Id)
                        .FirstOrDefault(); // Get the first matching repository ID or default to 0

                    // If we found the repository ID, exit the loop
                    if (repoId != 0)
                    {
                        Console.WriteLine($"Repository ID for '{repoName}': {repoId}");
                        break;
                    }

                    currentPage++;
                }

                return repoId;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                throw;
            }
        }

        private int ResolveRepoIdFromCatalog(string repoName, string normalizedBranch, string externalRepoId)
        {
            try
            {
                var allRepos = new List<AikidoCodeRepository>();
                for (var page = 0; page < 15; page++)
                {
                    var pageItems = _apiClient.GetCodeRepositories(page, 100);
                    if (pageItems == null || pageItems.Count == 0)
                    {
                        break;
                    }

                    allRepos.AddRange(pageItems.Where(r => r.Active));
                    if (pageItems.Count < 100)
                    {
                        break;
                    }
                }

                var candidates = allRepos.Where(r =>
                    (!string.IsNullOrWhiteSpace(externalRepoId) &&
                     !string.IsNullOrWhiteSpace(r.ExternalRepoId) &&
                     (r.ExternalRepoId.Equals(externalRepoId, StringComparison.OrdinalIgnoreCase) ||
                      r.ExternalRepoId.StartsWith(externalRepoId + "_aikidoclone_", StringComparison.OrdinalIgnoreCase)))
                    || (!string.IsNullOrWhiteSpace(repoName) &&
                        !string.IsNullOrWhiteSpace(r.Name) &&
                        r.Name.Equals(repoName, StringComparison.OrdinalIgnoreCase)))
                    .ToList();

                if (candidates.Count == 0)
                {
                    return 0;
                }

                if (!string.IsNullOrWhiteSpace(normalizedBranch))
                {
                    var exactBranchMatch = candidates
                        .Where(c => string.Equals(NormalizeBranchName(c.Branch), normalizedBranch, StringComparison.OrdinalIgnoreCase))
                        .OrderByDescending(c => c.Id)
                        .FirstOrDefault();

                    if (exactBranchMatch != null)
                    {
                        return exactBranchMatch.Id;
                    }
                }

                var primaryRepo = candidates
                    .Where(c => string.IsNullOrWhiteSpace(c.ExternalRepoId) ||
                                c.ExternalRepoId.IndexOf("_aikidoclone_", StringComparison.OrdinalIgnoreCase) < 0)
                    .OrderBy(c => c.Id)
                    .FirstOrDefault();

                return (primaryRepo ?? candidates.First()).Id;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: failed to query Aikido code repository catalog ({ex.Message}); falling back to issue-groups lookup.");
                return 0;
            }
        }

        private static string NormalizeBranchName(string branch)
        {
            if (string.IsNullOrWhiteSpace(branch))
            {
                return null;
            }

            var trimmed = branch.Trim();
            if (trimmed.StartsWith("refs/heads/", StringComparison.OrdinalIgnoreCase))
            {
                return trimmed.Substring("refs/heads/".Length);
            }

            if (trimmed.StartsWith("refs/tags/", StringComparison.OrdinalIgnoreCase))
            {
                return trimmed.Substring("refs/tags/".Length);
            }

            return trimmed;
        }
    }
}
