using Microsoft.VisualStudio.Services.ReleaseManagement.WebApi;
using Microsoft.VisualStudio.Services.ReleaseManagement.WebApi.Contracts;
using Newtonsoft.Json;
using RestSharp;
using ServiceNowCLI.Config.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ServiceNowCLI.Core.AzureDevOps
{
    public class ReleaseLogic : BaseLogic
    {
        private readonly string teamProjectName;

        public ReleaseLogic(
            string teamProjectName,
            AzureDevOpsSettings adoSettings,
            IAzureDevOpsTokenHandler tokenHandler): base(adoSettings, tokenHandler)
        {
            this.teamProjectName = teamProjectName;
        }

        public void UpdateReleaseVariables(string releaseId, Dictionary<string, string> variableNamesAndValues)
        {
            var releaseClient = GetReleaseClient(releaseId);
            var release = GetRelease(releaseClient);
            UpdateReleaseObjectVariables(release, variableNamesAndValues);
            PublishUpdatedRelease(release, releaseClient);
        }

        private void PublishUpdatedRelease(Release release, RestClient releaseClient)
        {
            Console.WriteLine("Publishing updated release back to Azure DevOps");

            var uri = releaseClient.Options.BaseUrl;
            var updatedReleaseJson = JsonConvert.SerializeObject(release);
            var putRequest = new RestRequest(uri, Method.Put);
            putRequest.AddJsonBody(updatedReleaseJson);
            var putResponse = releaseClient.Execute(putRequest);

            if (!putResponse.IsSuccessful)
            {
                throw new ArgumentException(putResponse.ErrorMessage);
            }

            Console.WriteLine("Release published back to Azure DevOps successfully");
        }

        private void UpdateReleaseObjectVariables(Release release, Dictionary<string, string> variableNamesAndValues)
        {
            foreach (var variableName in variableNamesAndValues.Keys)
            {
                UpsertReleaseVariableValue(release, variableName, variableNamesAndValues[variableName]);
            }
        }

        private RestClient GetReleaseClient(string releaseId)
        {
            var releaseLocationUrl = GetResourceLocationUrl(AdoResources.release);
            var clientUri = AzureDevOpsApiUriBuilder.GetUriForReleaseId(releaseLocationUrl, teamProjectName, releaseId);
            
            return GetClient(clientUri);
        }

        public Release GetRelease(string releaseId)
        {
            var releaseClient = GetReleaseClient(releaseId);
            return GetRelease(releaseClient);
        }

        public string GetBuildIdFromRelease(string releaseId, string buildNumber)
        {
            var release = GetRelease(releaseId);

            var buildArtifacts = release.Artifacts.Where(x => x.Type == "Build").ToList();

            if (buildArtifacts.Count == 1)
            {
                var buildArtifact = buildArtifacts.First();
                return extractBuildId(buildArtifact);
            }
            else if (buildArtifacts.Count > 1)
            {
                var allBuildDefinitions = string.Join(", ", buildArtifacts.Select(x => x.DefinitionReference["definition"].Name));
                Console.WriteLine($"Multiple build artifacts found in the release with Id={releaseId}, will try to choose one from the next definitions: [{allBuildDefinitions}]");

                // try to find the build with definition starts from same as the build number
                var matchingBuildArtifacts = buildArtifacts.Where(x => buildNumber.StartsWith(x.DefinitionReference["definition"].Name, StringComparison.OrdinalIgnoreCase)).ToList();
                var buildArtifact = matchingBuildArtifacts.FirstOrDefault();
                if (buildArtifact != null)
                {
                    var buildId = extractBuildId(buildArtifact);
                    Console.WriteLine($"Found build artifact: {buildArtifact.DefinitionReference["definition"].Name}, Id: {buildId}");

                    return buildId;
                }
                else
                {
                    Console.WriteLine($"Could not find build artifact with definition matching start of build number among {buildArtifacts.Count} build artifacts, choosing the first one");
                    return extractBuildId(buildArtifacts.First());
                }
            }
            else
            {
                return string.Empty;
            }
        }

        private static string extractBuildId(Artifact buildArtifact)
        {
            const string buildUriName = "buildUri";
            if (!buildArtifact.DefinitionReference.TryGetValue(buildUriName, out ArtifactSourceReference value))
            {
                return string.Empty;
            }

            var buildUri = value;

            var buildId = buildUri.Id.Split('/').Last();

            return buildId;
        }

        private Release GetRelease(RestClient releaseClient)
        {
            var uri = releaseClient.Options.BaseUrl;
            var getRequest = new RestRequest(uri);

            var getResponse = releaseClient.Execute(getRequest);

            if (!getResponse.IsSuccessful)
            {
                throw new ArgumentException(getResponse.ErrorMessage);
            }

            var release = JsonConvert.DeserializeObject<Release>(getResponse.Content);
            return release;
        }

        private void UpsertReleaseVariableValue(Release release, string variableName, string variableValue)
        {
            if (!release.Variables.TryGetValue(variableName, out ConfigurationVariableValue value))
            {
                var newVariableValue = new ConfigurationVariableValue
                {
                    Value = variableValue,
                    AllowOverride = true
                };

                release.Variables.Add(variableName, newVariableValue);
                Console.WriteLine($"Adding variable to release, VariableName={variableName}, VariableValue={variableValue}");
            }
            else
            {
                var variableToUpdate = value;
                variableToUpdate.Value = variableValue;
                Console.WriteLine($"Updating variable in release, VariableName={variableName}, VariableValue={variableValue}");
            }
        }
    }
}
