using System;
using System.Net.Http;
using Newtonsoft.Json;
using ServiceNowCLI.Config.Dtos;

namespace ServiceNowCLI.Config
{
    public interface IDorcConfigProvider
    {
        string GetDorcPropertyValue(string dorcPropertyName);
    }

    public class DorcConfigProvider(string dorcApiBaseUrl, string dorcEnvironment) : IDorcConfigProvider
    {
        public string GetDorcPropertyValue(string dorcPropertyName)
        {
            var httpClientHandler = new HttpClientHandler { UseDefaultCredentials = true };

            var httpClient = new HttpClient(httpClientHandler) { BaseAddress = new Uri(dorcApiBaseUrl) };

            var response = httpClient.GetAsync($"PropertyValues?environmentName={dorcEnvironment}&propertyName={dorcPropertyName}").GetAwaiter().GetResult();
            var responseContent = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            try
            {
                var dorcPropertyValue = JsonConvert.DeserializeObject<DorcPropertyValue[]>(responseContent);
                return dorcPropertyValue[0].Value;
            }
            catch
            {
                throw new ArgumentException($"Failed to get property from Dorc: Environment={dorcEnvironment}, PropertyName={dorcPropertyName}");
            }            
        }
    }
}
