using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.TeamFoundation.Build.WebApi;
using Microsoft.VisualStudio.Services.WebApi;
using Newtonsoft.Json;
using RestSharp;
using ServiceNowCLI.Config.Dtos;

namespace ServiceNowCLI.Core.AzureDevOps
{
    public class BuildLogic : BaseLogic
    {
        private readonly BuildHttpClient _buildsClient;
        private readonly string teamProjectName;
        private readonly VssConnection vssConnection;

        public BuildLogic(
            string teamProjectName,
            AzureDevOpsSettings adoSettings,
            IAzureDevOpsTokenHandler tokenHandler,
            VssConnection vssConnection) : base(adoSettings, tokenHandler)
        {
            this.teamProjectName = teamProjectName;
            this.vssConnection = vssConnection;

            _buildsClient = vssConnection.GetClient<BuildHttpClient>();
        }

        public IEnumerable<Build> GetRecentBuilds(int howManyMonthsAgo)
        {
            Console.WriteLine($"Getting recent builds... starting getting last {howManyMonthsAgo} months");

            string continuationToken = null;
            List<BuildDefinitionReference> buildDefinitions = [];

            do
            {
                IPagedList<BuildDefinitionReference> buildsPage = _buildsClient.GetDefinitionsAsync2(
                    project: teamProjectName,
                    builtAfter: DateTime.Today.AddMonths(howManyMonthsAgo * -1),
                    continuationToken: continuationToken).Result;

                buildDefinitions.AddRange(buildsPage);

                continuationToken = buildsPage.ContinuationToken;

            } while (!string.IsNullOrEmpty(continuationToken));

            var builds = new List<Build>();

            foreach (var buildDefinitionReference in buildDefinitions)
            {
                do
                {
                    IPagedList<Build> buildsPage = _buildsClient.GetBuildsAsync2(statusFilter: BuildStatus.Completed,
                        project: teamProjectName, resultFilter: BuildResult.Succeeded,
                        definitions: [buildDefinitionReference.Id],
                        continuationToken: continuationToken).Result;

                    builds.AddRange(buildsPage);

                    continuationToken = buildsPage.ContinuationToken;

                } while (!string.IsNullOrEmpty(continuationToken));
            }

            Console.WriteLine($"Getting recent builds... finished. {builds.Count} builds returned");

            return builds;
        }

        private RestClient GetBuildClientForBuildId(string buildId)
        {
            var buildLocationUrl = GetResourceLocationUrl(AdoResources.build);
            var clientUri = AzureDevOpsApiUriBuilder.GetUriForBuildId(buildLocationUrl, teamProjectName, buildId);

            return GetClient(clientUri);
        }

        public Build GetBuildForId(string buildId)
        {
            var buildClient = GetBuildClientForBuildId(buildId);
            var build = GetBuild(buildClient);
            return build;
        }

        private Build GetBuild(RestClient buildClient)
        {
            var uri = buildClient.Options.BaseUrl;
            var getRequest = new RestRequest(uri);
            var getResponse = buildClient.Execute(getRequest);

            if (!getResponse.IsSuccessful)
            {
                var fullResponseJson = JsonConvert.SerializeObject(getResponse);
                Console.WriteLine($"GET request was not successful. FullResponse={fullResponseJson}");
                throw new ArgumentException(getResponse.ErrorMessage);
            }

            var build = JsonConvert.DeserializeObject<Build>(getResponse.Content);
            return build;
        }

        public Build GetBuildForBuildNumber(string buildNumber)
        {
            var recentBuilds = GetRecentBuilds(3);
            var build = recentBuilds.FirstOrDefault(b => b.BuildNumber == buildNumber);
            return build;
        }

        public List<ResourceRef> GetBuildLinkedWorkItems(Build build)
        {
            Console.WriteLine($"Getting linked work items from BuildNumber={build.BuildNumber}, BuildId={build.Id}");

            var workItems = _buildsClient.GetBuildWorkItemsRefsAsync(build.Project.Id, build.Id).GetAwaiter().GetResult();

            if (workItems.Count == 0)
                throw new ArgumentException(
                    "No Work Items could be found for release, please ensure that your build has linked work items!");

            var workItemsForConsole = string.Join(", ",workItems.Select(x => x.Id));

            Console.WriteLine($"{workItems.Count} linked work items found for BuildNumber={build.BuildNumber}, BuildId={build.Id}: {workItemsForConsole}");

            return workItems;
        }
    }
}
