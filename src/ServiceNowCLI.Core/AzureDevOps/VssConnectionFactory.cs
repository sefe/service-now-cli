using System;
using Microsoft.VisualStudio.Services.Client;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;
using ServiceNowCLI.Config.Dtos;
using ServiceNowCLI.Core.Extensions;

namespace ServiceNowCLI.Core.AzureDevOps
{
    public interface IVssConnectionFactory
    {
        VssConnection CreateVssConnection(string collectionUri);
    }

    public class VssConnectionFactory : IVssConnectionFactory
    {
        private readonly AzureDevOpsSettings _adoSettings;
        private readonly IAzureDevOpsTokenHandler _tokenHandler;

        public VssConnectionFactory(AzureDevOpsSettings adoSettings, IAzureDevOpsTokenHandler tokenHandler)
        {
            _adoSettings = adoSettings;
            _tokenHandler = tokenHandler;
        }

        public VssConnection CreateVssConnection(string collectionUri)
        {
            if (collectionUri.Contains(_adoSettings.CollectionUrlCloudIndicator, StringComparison.OrdinalIgnoreCase))
            {
                return CreateCloudVssConnection();
            }

            Console.WriteLine($"Creating VssConnection for on-prem AzureDevOps");
            return new VssConnection(new Uri(collectionUri), new VssCredentials());
        }

        public VssConnection CreateCloudVssConnection()
        {
            Console.WriteLine($"Creating VssConnection for cloud AzureDevOps");

            var accessToken = _tokenHandler.GetToken();
            var token = new VssAadToken("Bearer", accessToken);
            var credentials = new VssAadCredential(token);

            var settings = VssClientHttpRequestSettings.Default.Clone();

            var organizationUri = new Uri(_adoSettings.OrganizationUrl);

            return new VssConnection(organizationUri, credentials, settings);
        }
    }
}
