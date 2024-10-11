using Newtonsoft.Json;
using RestSharp;
using ServiceNowCLI.Config.Dtos;
using System;

namespace ServiceNowCLI.Core.AzureDevOps
{
    public abstract class BaseLogic
    {
        protected readonly AzureDevOpsSettings adoSettings;
        protected readonly IAzureDevOpsTokenHandler tokenHandler;

        protected BaseLogic(
            AzureDevOpsSettings adoSettings,
            IAzureDevOpsTokenHandler tokenHandler)
        {
            this.adoSettings = adoSettings;
            this.tokenHandler = tokenHandler;
        }

        protected RestClient GetClient(string uri)
        {
            var accessToken = tokenHandler.GetToken();
            var options = RestClientOptionsBuilder.GetRestClientOptions(adoSettings, accessToken, uri);
            var client = new RestClient(options);

            return client;
        }

        protected string GetResourceLocationUrl(Guid resourceGuid)
        {
            var resourceLocationUri = AzureDevOpsApiUriBuilder.GetResourceAreasUri(adoSettings, resourceGuid.ToString());
            var client = GetClient(resourceLocationUri);
            var getResponse = client.Execute(new RestRequest());
            if (!getResponse.IsSuccessful)
            {
                throw new ArgumentException(getResponse.ErrorMessage);
            }
            var location = JsonConvert.DeserializeObject<ResourceObject>(getResponse.Content);

            return location.locationUrl.TrimEnd('/');
        }
    }


    public class ResourceObject
    {
        public string id { get; set; }
        public string name { get; set; }
        public string locationUrl { get; set; }
    }


    /// <summary>
    /// see https://learn.microsoft.com/en-us/azure/devops/extend/develop/work-with-urls
    /// </summary>
    public static class AdoResources
    {
        public static readonly Guid build = new Guid("5d6898bb-45ec-463f-95f9-54d49c71752e");
        public static readonly Guid release = new Guid("efc2f575-36ef-48e9-b672-0c6fb4a48ac5");
        public static readonly Guid wit = new Guid("5264459e-e5e0-4bd8-b118-0985e68a4ec5");
    }
}