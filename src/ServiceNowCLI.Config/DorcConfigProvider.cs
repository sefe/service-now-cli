using DOrc.API.Client;
using System;

namespace ServiceNowCLI.Config
{
    public interface IDorcConfigProvider
    {
        string GetDorcPropertyValue(string dorcPropertyName);
    }

    public class DorcConfigProvider(string dorcApiBaseUrl, string dorcEnvironment, string dorcClientId, string dorcClientSecret) : IDorcConfigProvider
    {
        public string GetDorcPropertyValue(string dorcPropertyName)
        {
            string baseUrl = dorcApiBaseUrl ?? DorcApiConfiguration.DefaultBaseUrl;

            try
            {
                var configuration = new DorcApiConfiguration()
                {
                    BaseUrl = baseUrl,
                    ClientId = dorcClientId,
                    ClientSecret = dorcClientSecret
                };

                var apiClient = new DorcApiClient(configuration);

                var dorcPropertyValue = apiClient.GetPropertyValueAsync(dorcEnvironment, dorcPropertyName).GetAwaiter().GetResult();

                return dorcPropertyValue;
            }
            catch (Exception e)
            {
                throw new ArgumentException($"Failed to get property from Dorc: Environment={dorcEnvironment}, PropertyName={dorcPropertyName}. Response status code: Error message: {e.Message}");
            }            
        }
    }
}
