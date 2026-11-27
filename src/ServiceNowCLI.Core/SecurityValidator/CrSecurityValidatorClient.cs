using Newtonsoft.Json;
using RestSharp;
using System;
using System.Linq;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

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
            var options = new RestClientOptions(_baseUrl)
            {
                RemoteCertificateValidationCallback = ValidateInternalServerCertificate
            };
            _client = new RestClient(options);
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
                var innerDetail = response.ErrorException?.InnerException?.Message;
                var errorDetail = !string.IsNullOrWhiteSpace(innerDetail)
                    ? $"{response.ErrorMessage} ({innerDetail})"
                    : (response.ErrorMessage ?? response.Content);

                throw new InvalidOperationException(
                    $"Failed to call CR Security Validator endpoint '{_baseUrl}/api/v1/autocr/scan': HTTP {(int)response.StatusCode} ({response.StatusCode}) - {errorDetail}");
            }

            var result = JsonConvert.DeserializeObject<AutoCrScanResponse>(response.Content);
            if (result == null)
            {
                throw new InvalidOperationException(
                    $"CR Security Validator returned an empty or invalid response from '{_baseUrl}/api/v1/autocr/scan'.");
            }

            return result;
        }

        internal bool ValidateInternalServerCertificate(
            object sender,
            X509Certificate certificate,
            X509Chain chain,
            SslPolicyErrors sslPolicyErrors)
        {
            if (sslPolicyErrors == SslPolicyErrors.None)
            {
                return true;
            }

            // Never allow hostname mismatches or missing certificates
            if (certificate == null || (sslPolicyErrors & SslPolicyErrors.RemoteCertificateNameMismatch) != 0)
            {
                return false;
            }

            // On Windows build agents where the intermediate CA for internal *.sefe.eu / *.sefe-mt.com AKS ingresses
            // is not pre-cached in the local machine store, allow chain errors only when the certificate is time-valid,
            // not revoked, and matches an internal SEFE domain.
            if (sslPolicyErrors == SslPolicyErrors.RemoteCertificateChainErrors &&
                Uri.TryCreate(_baseUrl, UriKind.Absolute, out var baseUri) &&
                (baseUri.Host.EndsWith(".sefe.eu", StringComparison.OrdinalIgnoreCase) ||
                 baseUri.Host.EndsWith(".sefe-mt.com", StringComparison.OrdinalIgnoreCase)) &&
                chain != null &&
                chain.ChainStatus.All(s =>
                    s.Status == X509ChainStatusFlags.NoError ||
                    s.Status == X509ChainStatusFlags.UntrustedRoot ||
                    s.Status == X509ChainStatusFlags.PartialChain ||
                    s.Status == X509ChainStatusFlags.RevocationStatusUnknown ||
                    s.Status == X509ChainStatusFlags.OfflineRevocation))
            {
                return true;
            }

            return false;
        }
    }
}
