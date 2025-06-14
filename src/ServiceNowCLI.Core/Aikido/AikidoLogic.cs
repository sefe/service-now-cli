﻿using ServiceNowCLI.Core.Aikido.Models;
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

        public bool GenerateIssuesReport(string repoName, string filename, string issuePathFilter = null)
        {
            var issues = GetIssuesForRepo(repoName, out var repoId, issuePathFilter);
            if (issues == null)
                return false;
            ReportGenerator.GeneratePdfReport(repoName, issues, filename, _apiClient.LinkToIssues(repoId));
            return true;
        }

        public bool GenerateIssuesReport(string repoName, Stream stream, string issuePathFilter = null)
        {
            var issues = GetIssuesForRepo(repoName, out var repoId, issuePathFilter);
            if (issues == null)
                return false;

            ReportGenerator.GeneratePdfReport(repoName, issues, stream, _apiClient.LinkToIssues(repoId));
            return true;
        }

        public List<Issue> GetIssuesForRepo(string repoName, out int repoId, string pathFilter = null)
        {
            repoId = GetRepoId(repoName);
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
    }
}
