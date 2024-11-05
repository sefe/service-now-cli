using Fluid;
using Microsoft.Azure.Pipelines.WebApi;
using Microsoft.TeamFoundation.Build.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Newtonsoft.Json;
using ServiceNowCLI.Config.Dtos;
using ServiceNowCLI.Core.Arguments;
using ServiceNowCLI.Core.ServiceNow;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace ServiceNowCLI.Core.AzureDevOps
{
    public class ChangeRequestLogic(AzureDevOpsSettings adoSettings, ServiceNowSettings snSettings, IAzureDevOpsTokenHandler tokenHandler, IVssConnectionFactory vssConnectionFactory)
    {
        public void CreateChangeRequest(CreateCrOptions arguments)
        {
            CreateNewChangeRequest(arguments);
        }

        public object CompleteActivity(SetActivityOptions opts, bool successfully)
        {
            var successName = successfully ? "Succeeded" : "Failed";
            string note = !string.IsNullOrEmpty(opts.CloseNote) ? $" with note: {opts.CloseNote}" : "";

            Console.WriteLine($"Closing CR {opts.ChangeNo} as {successName}{note}...");

            var client = new ServiceNowHttpClient(snSettings, tokenHandler.GetToken());

            var isCompleted = client.CompleteCR(opts.ChangeNo, successfully, opts.CloseNote);
            
            if (isCompleted)
            {
                Console.WriteLine($"Closing CR {opts.ChangeNo} as {successName} has been finished.");
            }
            else
            {
                return -1;
            }

            return 0;
        }

        public void CancelCrs(CancelCrsOptions opts)
        {
            var nums = opts.ChangeNums.Split(',').Select(p => p.Trim()).Where(n => !string.IsNullOrWhiteSpace(n)).ToList();
            var client = new ServiceNowHttpClient(snSettings, tokenHandler.GetToken());

            foreach (var number in nums)
            {
                var isOk = client.CancelCRByNumber(number);

                if (isOk)
                {
                    Console.WriteLine($"CR {number} has been cancelled.");
                }
             }
        }

        private void CreateNewChangeRequest(CreateCrOptions arguments)
        {
            Console.WriteLine($"CreateNewChangeRequest - arguments: {JsonConvert.SerializeObject(arguments)}");
            string inputContent = GetInputFileString(arguments);
            var vssConnection = vssConnectionFactory.CreateVssConnection(adoSettings);

            var crInputs = JsonConvert.DeserializeObject<CreateChangeRequestInput>(inputContent);
            var commSettings = GetCommSettings(arguments.CommParamsFile);
            var buildLogic = new BuildLogic(crInputs.TeamProjectName, adoSettings, tokenHandler, vssConnection);
            var workItemLogic = new WorkItemLogic(crInputs.TeamProjectName, adoSettings, tokenHandler, vssConnection);
            var releaseLogic = new ReleaseLogic(crInputs.TeamProjectName, adoSettings, tokenHandler);
            var changeDescriptionGenerator = new ChangeDescriptionGenerator();

            var build = GetBuildForRelease(releaseLogic, buildLogic, arguments);
            var pipeline = BuildLogic.GetPipeline(vssConnection, crInputs.TeamProjectName, build.Definition.Id);

            var isProd = IsReleaseEnvironmentProd(arguments.Environment);

            Console.WriteLine($"Got build, BuildNumber={build.BuildNumber}, BuildId={build.Id}");

            ValidateBranchUsedForBuild(arguments, build, crInputs, isProd, pipeline.Configuration.Type == ConfigurationType.Yaml);

            var buildLinkedWorkItemReferences = buildLogic.GetBuildLinkedWorkItems(build);
            var workItems = workItemLogic.GetWorkItemsLinkedToBuild(buildLinkedWorkItemReferences, build, arguments);
            var changeDescriptions = changeDescriptionGenerator.GenerateChangeDescription(workItems);

            var changeRequest = CreateChangeRequest(crInputs, arguments, changeDescriptions);

            var crNumber = CallServiceNowToCreateChangeRequest(changeRequest);

            if (!string.IsNullOrEmpty(crNumber))
            {
                Console.WriteLine($"CR raised: {crNumber}");

                AddTagsToWit(crNumber, workItems, workItemLogic, isProd);

                var variables = CollectPipelineVariablesToSet(
                    changeDescriptions,
                    crNumber,
                    changeRequest.ScheduledStartDate,
                    changeRequest.ScheduledEndDate,
                    commSettings);

                AddVariablesToPipeline(
                    variables,
                    arguments.ReleaseId,
                    releaseLogic);

                if (!string.IsNullOrEmpty(arguments.OutputCrNumberFile))
                {
                    Console.WriteLine($"Writing CR number to file: {arguments.OutputCrNumberFile}");
                    using (var writer = File.AppendText(arguments.OutputCrNumberFile))
                    {
                        writer.Write(crNumber);
                    }
                }
            }
        }

        private static string GetInputFileString(CreateCrOptions arguments)
        {
            string inputContent;
            if (!string.IsNullOrEmpty(arguments.TransformTemplateFile))
            {
                Console.WriteLine($"using template '{arguments.TransformTemplateFile}' to transform input file");
                var parser = new FluidParser();
                var template = File.ReadAllText(arguments.TransformTemplateFile);

                if (parser.TryParse(template, out var compiledTemplate, out var error))
                {
                    var model = JsonConvert.DeserializeObject<object>(File.ReadAllText(arguments.CrParamsFile));

                    var context = new TemplateContext(model);

                    inputContent = compiledTemplate.Render(context);
                }
                else
                {
                    throw new ArgumentException($"Bad template '{arguments.TransformTemplateFile}', parsing failed: {error}");
                }
            }
            else
            {
                inputContent = File.ReadAllText(arguments.CrParamsFile);
            }

            try
            {
                JsonConvert.DeserializeObject<object>(inputContent);
            }
            catch (Exception exc)
            {
                Console.WriteLine($"result after template render: \n{inputContent}");
                throw new ArgumentException($"Bad template '{arguments.TransformTemplateFile}', it produces non json output: {exc.Message}");
            }

            return inputContent;
        }

        private Build GetBuildForRelease(ReleaseLogic releaseLogic, BuildLogic buildLogic, CreateCrOptions arguments)
        {
            if (!string.IsNullOrEmpty(arguments.ReleaseId))
            {
                var buildFromReleaseId = GetBuildFromReleaseId(releaseLogic, buildLogic, arguments.ReleaseId);

                if (buildFromReleaseId != null)
                {
                    return buildFromReleaseId;
                }
            }
            
            var buildArgument = arguments.BuildNumber;

            if (buildArgument.Contains(';'))
            {
                throw new ArgumentException($"BuildNumber in CLI arguments is [{arguments.BuildNumber}], which contains a semi-colon. Only single builds are supported");
            }

            var buildFromBuildNumberInArguments = buildLogic.GetBuildForBuildNumber(arguments.BuildNumber) 
                ?? throw new ArgumentException($"Unable to get build definition for build number {arguments.BuildNumber} specified in CLI tool arguments");

            return buildFromBuildNumberInArguments;
        }

        private Build GetBuildFromReleaseId(ReleaseLogic releaseLogic, BuildLogic buildLogic, string ReleaseId)
        {
            try
            {
                Console.WriteLine($"Trying to get Build Id from Release Id {ReleaseId}");
                var buildId = releaseLogic.GetBuildIdFromRelease(ReleaseId);

                if (!string.IsNullOrEmpty(buildId))
                {
                    Console.WriteLine($"Build Id = {buildId} for Release Id {ReleaseId}. Trying to get build definition.");
                    var build = buildLogic.GetBuildForId(buildId);
                    return build;
                }

                Console.WriteLine($"Failed to get Build Id from Release Id {ReleaseId}");
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to get build from release ID {ReleaseId}. Exception = {ex}");
                return null;
            }

        }

        private CommSettings GetCommSettings(string commParamsFile)
        {
            CommSettings commSettings = null;

            if (!string.IsNullOrEmpty(commParamsFile))
            {
                if (File.Exists(commParamsFile))
                {
                    var commParamsFileContents = File.ReadAllText(commParamsFile);
                    commSettings = JsonConvert.DeserializeObject<CommSettings>(commParamsFileContents);
                    Console.WriteLine($"Comm inputs json file read successfully: {commParamsFileContents}");
                }
                else
                {
                    Console.WriteLine($"Comm inputs json file does not exist - {commParamsFile}");
                }
                
            }
            else
            {
                Console.WriteLine("No Comm inputs json file specified");
            }

            return commSettings;
        }

        private void AddVariablesToPipeline(
            Dictionary<string, string> variables,
            string releaseId,
            ReleaseLogic releaseLogic
            )
        {
            if (!string.IsNullOrEmpty(releaseId))
            {
                releaseLogic.UpdateReleaseVariables(releaseId, variables);
            }
            else
            {
                Console.WriteLine("Release ID not specified so no variables will be saved in the release");
            }
        }

        private void AddTagsToWit(string crNumber, List<WorkItem> workItems, WorkItemLogic workItemLogic, bool isProd)
        {
            var tagPrefix = !isProd ? "AutoDv" : "Auto";
            var crNumberTag = tagPrefix + crNumber.Replace("\"", "");

            AddCrNumberTagToPbis(workItems, workItemLogic, crNumberTag);
        }

        private bool IsReleaseEnvironmentProd(string envinronment)
        {
            if (string.IsNullOrEmpty(envinronment))
            {
                Console.WriteLine($"No 'environment' parameter passed, so assuming that is Non-Prod");
                return false;
            }
            envinronment = envinronment.ToLower();
            if (envinronment == "prod" || envinronment == "production")
            {
                return true;
            }

            return false;
        }

        private void AddCrNumberTagToPbis(
            List<Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models.WorkItem> workItems, 
            WorkItemLogic workItemLogic,
            string newTag)
        {
            Console.WriteLine($"Adding CR tag '{newTag}' to work items: {string.Join(",", workItems.Select(x => x.Id).ToList())}");
            
            foreach (var workItem in workItems)
            {
                workItemLogic.AddTagToWorkItem(workItem, newTag);
            }
        }

        private string CallServiceNowToCreateChangeRequest(ChangeRequestModel changeRequest)
        {
            Console.WriteLine($"Submitting CR: {JsonConvert.SerializeObject(changeRequest, Formatting.Indented)}");
            try
            {
                var httpClient = new ServiceNowHttpClient(snSettings, tokenHandler.GetToken());
                var crNumber = httpClient.CreateCR(changeRequest);

                return crNumber;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"CR creation failed: {ex.Message}");

                throw;
            }
        }

        private ChangeRequestModel CreateChangeRequest(CreateChangeRequestInput crInputs, CreateCrOptions arguments, List<string> changeDescriptions)
        {
            var crDescription = string.Join(Environment.NewLine, changeDescriptions);

            return new ChangeRequestModel(crInputs)
            {
                description =
                    $"The following enhancements will be delivered by this CR:{Environment.NewLine}{crDescription}",
                requested_by = arguments.ReleaseDeploymentRequestedFor,
                correlation_id = string.IsNullOrEmpty(crInputs.correlation_id) ? arguments.ExistingCr : null
            };
        } 

        private Dictionary<string, string> CollectPipelineVariablesToSet(List<string> changeChangesList, string crNumber, DateTime startDateTime, DateTime endDateTime, CommSettings commSettings)
        {
            var startDateTimeLocal = startDateTime.ToLocalTime();
            var endDateTimeLocal = endDateTime.ToLocalTime();

            var variables = new Dictionary<string, string>
            {
                { "CR_ID", crNumber.Replace("\"", string.Empty) },
                { "ActualStartTime", startDateTimeLocal.ToString("yyyy-MM-d HH:mm:ss") },
                { "ActualEndTime", endDateTimeLocal.ToString("yyyy-MM-d HH:mm:ss")}
            };

            AddCommSettingVariables(variables, commSettings, changeChangesList);
            return variables;
        }

        private void AddCommSettingVariables(
            Dictionary<string, string> variables, 
            CommSettings commSettings, 
            List<string> changesList)
        {
            if (commSettings is null)
            {
                var changeDescriptionSimple = string.Join(Environment.NewLine, changesList);
                variables.Add("ChangeDescription", changeDescriptionSimple);
                return;
            }

            var changeDescriptionHtml = this.ConvertChangeDescriptionToHtmlBulletList(changesList);

            variables.Add("Comms_ApplicationName", commSettings.ApplicationName);
            variables.Add("Comms_ApplicationAlias", commSettings.ApplicationAlias);
            variables.Add("Comms_ChangeImpact", commSettings.ChangeImpact);
            variables.Add("Comms_PointOfContact", commSettings.PointOfContact);
            variables.Add("Comms_UserActions", commSettings.UserActions);


            if (!string.IsNullOrEmpty(commSettings.ChangeDescriptionStart))
            {
                changeDescriptionHtml = $"<p>{commSettings.ChangeDescriptionStart}</p>{changeDescriptionHtml}";
            }

            if (!string.IsNullOrEmpty(commSettings.ChangeDescriptionEnd))
            {
                changeDescriptionHtml = $"{changeDescriptionHtml}<p>{commSettings.ChangeDescriptionEnd}</p>";
            }

            variables.Add("ChangeDescription", changeDescriptionHtml);
        }

        private string ConvertChangeDescriptionToHtmlBulletList(List<string> changes)
        {
            var sb = new StringBuilder();
            sb.Append("<ul>");

            foreach (var change in changes)
            {
                sb.Append($"<li>{change}</li>");
            }

            sb.Append("</ul>");

            return sb.ToString();
        }

        private void ValidateBranchUsedForBuild(CreateCrOptions crArguments, Build build, CreateChangeRequestInput crInputs, bool isProd, bool isYamlPipeline)
        {
            Dictionary<BranchingStrategies, string[]> branches =
            new()
            {
                {BranchingStrategies.GitHubFlow, new string[] {"project", "master", "feature", "bug", "release", "main"}},
                {BranchingStrategies.GitFlow, new string[] {"release", "master", "develop", "hotfix", "main"}}
            };

            if (!build.SourceBranch.ContainsAny(branches[crInputs.BranchingStrategy]))
            {
                if (isProd)
                    throw new ArgumentException(
                        $"Cannot raise a CR for Build {crArguments.BuildNumber} as this is not a valid branch for release!\nYou have specified {crInputs.BranchingStrategy} as your branching strategy, which includes the following releasable branches: {string.Join("|", branches[crInputs.BranchingStrategy])}");

                Console.WriteLine($"If this was a production deploy, the build isn't a valid branch for release and so would fail here.");
            }
            
            if (!isYamlPipeline && build.RetainedByRelease != true)
            {
                throw new ArgumentException($"Cannot raise a CR for Build {crArguments.BuildNumber} as this is not a pinned build. Pin the build and re-run the CR Creator");
            }
        }
    }
}
