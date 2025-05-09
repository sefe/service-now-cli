﻿using Fluid;
using Microsoft.Azure.Pipelines.WebApi;
using Microsoft.TeamFoundation.Build.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Newtonsoft.Json;
using ServiceNowCLI.Config.Dtos;
using ServiceNowCLI.Core.Aikido;
using ServiceNowCLI.Core.Arguments;
using ServiceNowCLI.Core.ServiceNow;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace ServiceNowCLI.Core.AzureDevOps
{
    public class ChangeRequestLogic(AzureDevOpsSettings adoSettings, ServiceNowSettings snSettings, IAzureDevOpsTokenHandler tokenHandler, IVssConnectionFactory vssConnectionFactory, AikidoSettings aikidoSettings)
    {
        public void CreateChangeRequest(CreateCrOptions arguments)
        {
            CreateNewChangeRequest(arguments);
        }

        public object CompleteActivity(SetActivityOptions opts, bool successfully)
        {
            var successName = successfully ? "Succeeded" : "Failed";

            var snLogic = new ServiceNowLogic(snSettings, tokenHandler.GetToken());
            return snLogic.CompleteCR(opts.ChangeNo, successfully, opts.CloseNote);
        }

        public void CancelCrs(CancelCrsOptions opts)
        {
            var snLogic = new ServiceNowLogic(snSettings, tokenHandler.GetToken());
            snLogic.CancelCrs(opts.ChangeNums);
        }

        private void CreateNewChangeRequest(CreateCrOptions arguments)
        {
            if (!string.IsNullOrEmpty(arguments.OutputCrNumberFile))
            {
                // clean content of the file in case smth fails, no need to keep the old CR number there
                File.WriteAllText(arguments.OutputCrNumberFile, "");
            }

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
            var pipeline = buildLogic.GetPipeline(crInputs.TeamProjectName, build.Definition.Id);

            var isProd = IsReleaseEnvironmentProd(arguments.Environment);

            Console.WriteLine($"Got build, BuildNumber={build.BuildNumber}, BuildId={build.Id}");

            ValidateBranchUsedForBuild(buildLogic, build, crInputs, isProd, pipeline.Configuration.Type == ConfigurationType.Yaml);

            var buildLinkedWorkItemReferences = buildLogic.GetBuildLinkedWorkItems(build);
            var workItems = workItemLogic.GetWorkItemsLinkedToBuild(buildLinkedWorkItemReferences, build, arguments);
            var changeDescriptions = changeDescriptionGenerator.GenerateChangeDescription(workItems);

            var changeRequest = CreateChangeRequest(crInputs, arguments, changeDescriptions);
            var serviceNowLogic = new ServiceNowLogic(snSettings, tokenHandler.GetToken());

            var crNumber = serviceNowLogic.CreateChangeRequest(changeRequest);

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
                    File.WriteAllText(arguments.OutputCrNumberFile, crNumber);
                }

                var aikidoLogic = new AikidoLogic(aikidoSettings.BaseUrl, aikidoSettings.ClientId, aikidoSettings.ClientSecret);
                using (var memoryStream = new MemoryStream())
                {
                    if (aikidoLogic.GenerateIssuesReport(build.Repository.Name, memoryStream, crInputs.IssuePathFilter))
                        serviceNowLogic.AttachFileToCreatedCr(crNumber, memoryStream, $"security_report.pdf");
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
            List<WorkItem> workItems, 
            WorkItemLogic workItemLogic,
            string newTag)
        {
            Console.WriteLine($"Adding CR tag '{newTag}' to work items: {string.Join(",", workItems.Select(x => x.Id).ToList())}");
            
            foreach (var workItem in workItems)
            {
                workItemLogic.AddTagToWorkItem(workItem, newTag);
            }
        }

        private ChangeRequestModel CreateChangeRequest(CreateChangeRequestInput crInputs, CreateCrOptions arguments, List<string> changeDescriptions)
        {
            var crDescription = string.Join(Environment.NewLine, changeDescriptions);
            
            if (!string.IsNullOrWhiteSpace(crInputs.description))
            {
                crDescription = $"{crDescription}{Environment.NewLine}{Environment.NewLine}{crInputs.description}";
            }

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

        private void ValidateBranchUsedForBuild(BuildLogic buildLogic, Build build, CreateChangeRequestInput crInputs, bool isProd, bool isYamlPipeline)
        {
            Dictionary<BranchingStrategies, string[]> branches =
            new()
            {
                {BranchingStrategies.GitHubFlow, new string[] {"project", "master", "feature", "bug", "release", "main"}},
                {BranchingStrategies.GitFlow, new string[] {"release", "master", "develop", "hotfix", "main"}}
            };

            var branchesToCheck = new List<string> { build.SourceBranch };
            if (build.SourceBranch.StartsWith("refs/tags"))
            {
                // here we need to check in which branches tag is created
                var tagname = build.SourceBranch.Split('/').Last();
                Console.WriteLine($"Build by tag '{tagname}'. Checking branches containing this tag");

                var repoId = build.Repository.Id;
                var branchesForTag = buildLogic.GetBranchesForTag(tagname, repoId);

                branchesToCheck.AddRange(branchesForTag);
            }

            if (!branchesToCheck.Any(b => b.ContainsAny(branches[crInputs.BranchingStrategy])))
            {
                if (isProd)
                    throw new ArgumentException(
                        $"Cannot raise a CR for Build {build.BuildNumber} as this is not a valid branch for release!\nYou have specified {crInputs.BranchingStrategy} as your branching strategy, which includes the following releasable branches: {string.Join("|", branches[crInputs.BranchingStrategy])}");

                Console.WriteLine($"If this was a production deploy, the build isn't a valid branch for release and so would fail here.");
            }
            
            if (!isYamlPipeline && build.RetainedByRelease != true && build.KeepForever != true)
            {
                throw new ArgumentException($"Cannot raise a CR for Build {build.BuildNumber} as this is not a pinned build. Pin the build and re-run the CR Creator");
            }
        }
    }
}
