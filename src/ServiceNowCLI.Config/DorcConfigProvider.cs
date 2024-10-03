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

    public class DorcConfigProvider : IDorcConfigProvider
    {
        private readonly string _dorcApiBaseUrl;
        private readonly string _dorcEnvironment;

        public DorcConfigProvider(string dorcApiBaseUrl, string dorcEnvironment)
        {
            _dorcApiBaseUrl = dorcApiBaseUrl;
            _dorcEnvironment = dorcEnvironment;
        }

        public string GetDorcPropertyValue(string dorcPropertyName)
        {
            var httpClientHandler = new HttpClientHandler { UseDefaultCredentials = true };

            var httpClient = new HttpClient(httpClientHandler) { BaseAddress = new Uri(_dorcApiBaseUrl) };

            var response = httpClient.GetAsync($"PropertyValues?environmentName={_dorcEnvironment}&propertyName={dorcPropertyName}").GetAwaiter().GetResult();
            var responseContent = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();

            var dorcPropertyValue = JsonConvert.DeserializeObject<DorcPropertyValue[]>(responseContent);

            return dorcPropertyValue[0].Value;
        }
    }
}
