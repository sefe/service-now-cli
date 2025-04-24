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
            _apiClient = new AikidoApiClient(baseUrl);
            _apiClient.Authenticate(clientId, clientSecret);
        }

        public void GenerateIssuesReport(string repoName, string filename)
        {
            var issues = GetIssuesForRepo(repoName);
            ReportGenerator.GeneratePdfReport(repoName, issues, filename);
        }

        public void GenerateIssuesReport(string repoName, Stream stream)
        {
            var issues = GetIssuesForRepo(repoName);
            ReportGenerator.GeneratePdfReport(repoName, issues, stream);
        }

        public List<Issue> GetIssuesForRepo(string repoName)
        {
            var repoId = GetRepoId(repoName);
            var issues = _apiClient.ExportIssuesJson(filterCodeRepoId: repoId);
            return issues;
        }

        private int GetRepoId(string repoName)
        {
            try
            {
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
                        Console.WriteLine($"No more issue groups found. Repository '{repoName}' does not exist.");
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

                // If we finished all pages and repoId is still 0, it means the repository was not found
                if (repoId == 0)
                {
                    Console.WriteLine($"Repository '{repoName}' not found in the issue groups.");
                }

                return repoId;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return 0;
            }
        }
    }
}
