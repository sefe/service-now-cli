using System;
using System.Threading;
using Azure.Core;
using Azure.Identity;
using ServiceNowCLI.Config.Dtos;

namespace ServiceNowCLI.Core.AzureDevOps
{
    public interface IAzureDevOpsTokenHandler
    {
        string GetToken();
    }

    public class AzureDevOpsTokenHandler : IAzureDevOpsTokenHandler
    {
        private readonly AzureDevOpsSettings _adoSettings;

        private string _accessToken;
        private DateTime _validUntilUtc;

        public AzureDevOpsTokenHandler(AzureDevOpsSettings adoSettings)
        {
            _adoSettings = adoSettings;
            _validUntilUtc = DateTime.MinValue;
        }

        public string GetToken()
        {
            if (DateTime.UtcNow >= _validUntilUtc)
            {
                GetNewAccessTokenFromAad();
            }

            return _accessToken;
        }

        private void GetNewAccessTokenFromAad()
        {
            try
            {
                Console.WriteLine($"Getting Access Token from Azure Active Directory...");

                var credential = new ClientSecretCredential(_adoSettings.TenantId, _adoSettings.ClientId,
                    _adoSettings.ClientSecret);

                var scopes = new[] { _adoSettings.Scope };

                var tokenRequestContext = new TokenRequestContext(scopes);
                var token = credential.GetTokenAsync(tokenRequestContext, CancellationToken.None).Result;

                _accessToken = token.Token;
                _validUntilUtc = token.ExpiresOn.UtcDateTime;

                Console.WriteLine($"Token received, valid until {_validUntilUtc:u}...");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception while getting Access Token from Azure Active Directory - {ex}");
                throw;
            }

        }
    }
}
