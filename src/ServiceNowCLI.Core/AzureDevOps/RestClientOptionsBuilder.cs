using System;
using RestSharp;
using RestSharp.Authenticators.OAuth2;
using ServiceNowCLI.Config.Dtos;

namespace ServiceNowCLI.Core.AzureDevOps
{
    public static class RestClientOptionsBuilder
    {
        private static readonly int TimeoutSec = 60;

        public static RestClientOptions GetRestClientOptions(AzureDevOpsSettings adoSettings, string token, string clientUri)
        {
            if (!adoSettings.UseDefaultCredentials)
            {
                Console.WriteLine($"Creating RestClientOptions using OAuth2Authorization for Uri={clientUri}");
                return new RestClientOptions(clientUri)
                {
                    Authenticator = new OAuth2AuthorizationRequestHeaderAuthenticator(token, "Bearer"),
                    Timeout = TimeSpan.FromSeconds(TimeoutSec)
                };
            }

            Console.WriteLine($"Creating RestClientOptions using default credentials for Uri={clientUri}");
            return new RestClientOptions(clientUri)
            {
                UseDefaultCredentials = true,
                Timeout = TimeSpan.FromSeconds(TimeoutSec)
            };
        }

    }
}
