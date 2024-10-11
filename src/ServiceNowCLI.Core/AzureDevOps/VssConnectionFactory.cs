using Microsoft.VisualStudio.Services.Client;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;
using ServiceNowCLI.Config.Dtos;
using System;

namespace ServiceNowCLI.Core.AzureDevOps
{
    public interface IVssConnectionFactory
    {
        VssConnection CreateVssConnection(AzureDevOpsSettings adoSettings);
    }

    public class VssConnectionFactory : IVssConnectionFactory
    {
        private readonly IAzureDevOpsTokenHandler _tokenHandler;

        public VssConnectionFactory(IAzureDevOpsTokenHandler tokenHandler)
        {
            _tokenHandler = tokenHandler;
        }

        public VssConnection CreateVssConnection(AzureDevOpsSettings adoSettings)
        {
            if (!adoSettings.UseDefaultCredentials)
            {
                return CreateCloudVssConnection(adoSettings);
            }

            Console.WriteLine($"Creating VssConnection for on-prem AzureDevOps");
            return new VssConnection(new Uri(adoSettings.OrganizationUrl), new VssCredentials());
        }

        private VssConnection CreateCloudVssConnection(AzureDevOpsSettings adoSettings)
        {
            Console.WriteLine($"Creating VssConnection for cloud AzureDevOps");

            var accessToken = _tokenHandler.GetToken();
            var token = new VssAadToken("Bearer", accessToken);
            var credentials = new VssAadCredential(token);

            var settings = VssClientHttpRequestSettings.Default.Clone();

            var organizationUri = new Uri(adoSettings.OrganizationUrl);

            return new VssConnection(organizationUri, credentials, settings);
        }
    }
}
