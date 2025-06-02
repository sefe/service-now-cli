using Newtonsoft.Json;
using RestSharp;
using ServiceNowCLI.Core.Aikido.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace ServiceNowCLI.Core.Aikido
{
    internal class AikidoApiClient
    {
        private readonly string _apiPrefix;
        private readonly RestClient _client;
        private readonly string _baseUrl;
        private string _accessToken;

        public string BaseUrl => _baseUrl;
        public string LinkToIssues(int repoId) => $"{_baseUrl}/repositories/{repoId}";

        public AikidoApiClient(string baseUrl)
        {
            _apiPrefix = "/api/public/v1/";
            _client = new RestClient(baseUrl);
            _baseUrl = baseUrl;
        }

        /// <summary>
        /// Authenticates using OAuth2 and retrieves the access token.
        /// </summary>
        public void Authenticate(string clientId, string clientSecret)
        {
            var request = new RestRequest("/api/oauth/token", Method.Post);

            var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{clientId}:{clientSecret}"));
            request.AddHeader("accept", "application/json");
            request.AddHeader("authorization", $"Basic {credentials}");

            request.AddJsonBody(new { grant_type = "client_credentials" });

            var response = _client.Execute<TokenResponse>(request);

            if (!response.IsSuccessful || response.Data == null || string.IsNullOrEmpty(response.Data.access_token))
            {
                throw new Exception($"Failed to retrieve access token: {response.StatusCode} - {response.ErrorMessage}");
            }

            _accessToken = response.Data.access_token;
            _client.AddDefaultHeader("Authorization", $"Bearer {_accessToken}");
            Console.WriteLine("Successfully authenticated to Aikido.");
        }

        /// <summary>
        /// Fetches a list of issues from the API.
        /// </summary>
        public List<Issue> GetIssues()
        {
            var request = new RestRequest($"{_apiPrefix}issues", Method.Get);
            var response = _client.Execute<List<Issue>>(request);

            if (!response.IsSuccessful)
            {
                throw new Exception($"Failed to fetch issues: {response.StatusCode} - {response.ErrorMessage}");
            }

            return response.Data;
        }

        public List<Issue> ExportIssuesJson(
            string filterStatus = null,
            int? filterTeamId = null,
            int? filterIssueGroupId = null,
            int? filterCodeRepoId = null,
            int? filterContainerRepoId = null,
            string filterContainerRepoName = null,
            string filterIssueType = null,
            string filterSeverities = null)
        {
            var request = new RestRequest($"{_apiPrefix}issues/export", Method.Get);

            request.AddQueryParameter("format", "json");

            if (!string.IsNullOrEmpty(filterStatus))
                request.AddQueryParameter("filter_status", filterStatus);

            if (filterTeamId.HasValue)
                request.AddQueryParameter("filter_team_id", filterTeamId.Value.ToString());

            if (filterIssueGroupId.HasValue)
                request.AddQueryParameter("filter_issue_group_id", filterIssueGroupId.Value.ToString());

            if (filterCodeRepoId.HasValue)
                request.AddQueryParameter("filter_code_repo_id", filterCodeRepoId.Value.ToString());

            if (filterContainerRepoId.HasValue)
                request.AddQueryParameter("filter_container_repo_id", filterContainerRepoId.Value.ToString());

            if (!string.IsNullOrEmpty(filterContainerRepoName))
                request.AddQueryParameter("filter_container_repo_name", filterContainerRepoName);

            if (!string.IsNullOrEmpty(filterIssueType))
                request.AddQueryParameter("filter_issue_type", filterIssueType);

            if (!string.IsNullOrEmpty(filterSeverities))
                request.AddQueryParameter("filter_severities", filterSeverities);

            var response = _client.Execute<List<Issue>>(request);

            if (!response.IsSuccessful)
            {
                throw new Exception($"Failed to export issues: {response.StatusCode} - {response.ErrorMessage}");
            }

            return response.Data;
        }

        /// <summary>
        /// Fetches a list of issue groups from the API.
        /// </summary>
        public List<IssueGroup> GetIssueGroups(
            int page = 0,
            int perPage = 20,
            int? filterCodeRepoId = null,
            string filterExternalCodeRepoId = null,
            string filterCodeRepoName = null,
            int? filterContainerRepoId = null,
            int? filterTeamId = null)
        {
            var request = new RestRequest($"{_apiPrefix}open-issue-groups", Method.Get);

            // Add pagination parameters
            request.AddQueryParameter("page", page.ToString());
            request.AddQueryParameter("per_page", perPage.ToString());

            // Add filters if they are provided
            if (filterCodeRepoId.HasValue)
                request.AddQueryParameter("filter_code_repo_id", filterCodeRepoId.Value.ToString());

            if (!string.IsNullOrEmpty(filterExternalCodeRepoId))
                request.AddQueryParameter("filter_external_code_repo_id", filterExternalCodeRepoId);

            if (!string.IsNullOrEmpty(filterCodeRepoName))
                request.AddQueryParameter("filter_code_repo_name", filterCodeRepoName);

            if (filterContainerRepoId.HasValue)
                request.AddQueryParameter("filter_container_repo_id", filterContainerRepoId.Value.ToString());

            if (filterTeamId.HasValue)
                request.AddQueryParameter("filter_team_id", filterTeamId.Value.ToString());

            // Execute the request
            var response = _client.Execute(request);

            if (!response.IsSuccessful)
            {
                throw new Exception($"Failed to fetch issue groups: {response.StatusCode} - {response.ErrorMessage}, {response.Content}");
            }

            return JsonConvert.DeserializeObject<List<IssueGroup>>(response.Content);
        }

        /// <summary>
        /// Fetches a specific issue group by ID.
        /// </summary>
        public IssueGroup GetIssueGroupByIdAsync(int groupId)
        {
            var request = new RestRequest($"{_apiPrefix}issue-groups/{groupId}", Method.Get);
            var response = _client.Execute<IssueGroup>(request);

            if (!response.IsSuccessful)
            {
                throw new Exception($"Failed to fetch issue group: {response.StatusCode} - {response.ErrorMessage}");
            }

            return response.Data;
        }
    }
}
