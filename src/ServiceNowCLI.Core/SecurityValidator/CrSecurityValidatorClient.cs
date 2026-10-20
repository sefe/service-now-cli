using Newtonsoft.Json;
using RestSharp;
using System;

namespace ServiceNowCLI.Core.SecurityValidator
{
    public class CrSecurityValidatorClient
    {
        private readonly string _baseUrl;
        private readonly RestClient _client;

        public CrSecurityValidatorClient(string baseUrl = null)
        {
            var resolvedUrl = !string.IsNullOrWhiteSpace(baseUrl)
                ? baseUrl
                : Environment.GetEnvironmentVariable("CR_SECURITY_VALIDATOR_URL");

            if (string.IsNullOrWhiteSpace(resolvedUrl))
            {
                throw new ArgumentException(
                    "CrSecurityValidatorUrl is not configured. Set 'CrSecurityValidatorUrl' in App.config or the 'CR_SECURITY_VALIDATOR_URL' environment variable.");
            }

            _baseUrl = resolvedUrl.TrimEnd('/');
            _client = new RestClient(_baseUrl);
        }

        public static string ResolveConfiguredUrl(string configuredUrl)
        {
            if (!string.IsNullOrWhiteSpace(configuredUrl))
            {
                return configuredUrl;
            }

            return Environment.GetEnvironmentVariable("CR_SECURITY_VALIDATOR_URL");
        }

        public AutoCrScanResponse ScanAutoCr(AutoCrScanRequest scanRequest)
        {
            Console.WriteLine($"Running pre-AutoCR security validation against {_baseUrl}/api/v1/autocr/scan for repo '{scanRequest.RepositoryName}' (branch '{scanRequest.SourceBranch}')...");

            var request = new RestRequest("/api/v1/autocr/scan", Method.Post);
            request.AddHeader("Accept", "application/json");
            request.AddHeader("Content-Type", "application/json");
            request.AddStringBody(JsonConvert.SerializeObject(scanRequest), DataFormat.Json);

            var response = _client.Execute(request);
            if (!response.IsSuccessful || string.IsNullOrWhiteSpace(response.Content))
            {
                throw new ApplicationException(
                    $"Failed to call CR Security Validator endpoint '{_baseUrl}/api/v1/autocr/scan': HTTP {(int)response.StatusCode} ({response.StatusCode}) - {response.ErrorMessage ?? response.Content}");
            }

            var result = JsonConvert.DeserializeObject<AutoCrScanResponse>(response.Content);
            if (result == null)
            {
                throw new ApplicationException(
                    $"CR Security Validator returned an empty or invalid response from '{_baseUrl}/api/v1/autocr/scan'.");
            }

            return result;
        }
    }
}
