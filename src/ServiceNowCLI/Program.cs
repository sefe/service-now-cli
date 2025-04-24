using CommandLine;
using Microsoft.VisualStudio.Services.Common;
using Newtonsoft.Json;
using ServiceNowCLI.Config;
using ServiceNowCLI.Config.Dtos;
using ServiceNowCLI.Core.Aikido;
using ServiceNowCLI.Core.Arguments;
using ServiceNowCLI.Core.AzureDevOps;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;

namespace ServiceNowCLI
{
    // Sample cmd line -
    // createcr -b "BuildNumber_21.11.22.1" -r "ReleaseNumber_21.11.22.1-Release_01" -e "NonProd" -u "user" -p "cr-inputs.json" -t "template.liquid" -i "15151" -m "comm-inputs.json"
    // createcr -b "BuildNumber_21.11.22.1" -r "ReleaseNumber_21.11.22.1-Release_01" -e "NonProd" -u "user" -p "cr-inputs.json" -t "template.liquid" -i "15151" -m "comm-inputs.json" -x "CHG0018962"
    // activitysuccess -n "Implementation Finished" -c "CHG0018188" 
    // setreleasevariable -t "ProjectName" -i "13157" -n "Comms_ReleaseID" -v "123456"
    // createcr -b "BuildNumber_23.11.3.2" -r "ReleaseNumber_23.11.3.2-Release_01" -e "NonProd" -u "user" -p "cr-inputs.json" -i "35313" -m "comm-inputs.json"


    public static class Program
    {
        static int Main(string[] args)
        {
            int returnCode = 0;

            try
            {
                ProcessCommand(args);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                returnCode = -1;
            }

            if (Environment.UserInteractive)
            {
                Console.WriteLine("User interactive mode, please press any key to end the process..");
                Console.ReadKey();
            }

            return returnCode;
        }

        private static void ProcessCommand(string[] args)
        {
            var activity = new Activity("ProcessCommand");
            activity.Start();

            Console.WriteLine($"Starting ServiceNowCLI - command line arguments: {JsonConvert.SerializeObject(args)}");

            Parser.Default.ParseArguments<CreateCrOptions, 
                ActivitySuccessOptions, 
                ActivityFailedOptions, 
                SetReleaseVariableOptions, 
                CancelCrsOptions, 
                GenerateSastReportOptions>(args)
                .MapResult(
                    (CreateCrOptions opts) => RunCreateChangeRequestAndReturnExitCode(opts),
                    (ActivitySuccessOptions opts) => RunActivitySuccessAndReturnExitCode(opts),
                    (ActivityFailedOptions opts) => RunActivityFailedAndReturnExitCode(opts),
                    (SetReleaseVariableOptions opts) => RunSetReleasePipelineVariableValueAndReturnExitCode(opts),
                    (CancelCrsOptions opts) => RunCancelChangeRequestNum(opts),
                    (GenerateSastReportOptions opts) => RunGenerateSastReport(opts),
                    errs => HandleArgumentParsingError(errs));

            activity.Stop();

            Console.WriteLine($"Finished - time taken = {activity.Duration:g}");
        }

        private static object RunGenerateSastReport(GenerateSastReportOptions opts)
        {
            var aikidoSettings = GetAikidoSettings();
            var aikidoLogic = new AikidoLogic(aikidoSettings.BaseUrl, aikidoSettings.ClientId, aikidoSettings.ClientSecret);
            var aikidoIssues = aikidoLogic.GetIssuesForRepo(opts.RepoName);
            ReportGenerator.GeneratePdfReport(opts.RepoName, aikidoIssues, opts.Filename ?? $"report_{opts.RepoName}_{DateTime.UtcNow.ToUnixEpochTime()}.pdf");
            return 0;
        }

        private static AzureDevOpsSettings GetAzureDevOpsSettings()
        {
            var dorcApiBaseUrl = ConfigurationManager.AppSettings["DorcApiBaseUrl"];
            var dorcEnvironment = ConfigurationManager.AppSettings["DorcEnvironment"];
            var dorcConfigProvider = new DorcConfigProvider(dorcApiBaseUrl, dorcEnvironment);
            var azureDevOpsSettingsBuilder = new AzureDevOpsSettingsBuilder(dorcConfigProvider);

            var settings = azureDevOpsSettingsBuilder.GetSettings();

            return settings;
        }

        private static AikidoSettings GetAikidoSettings()
        {
            return new AikidoSettings()
            {
                BaseUrl = ConfigurationManager.AppSettings["AikidoBaseUrl"],
                ClientId = ConfigurationManager.AppSettings["AikidoClientId"],
                ClientSecret = ConfigurationManager.AppSettings["AikidoClientSecret"]
            };
        }

        private static ServiceNowSettings GetServiceNowSettings()
        {
            return new ServiceNowSettings()
            {
                ApiUrl = ConfigurationManager.AppSettings["ServiceNowApiUrl"],
                SubscriptionHeaderName = "subscription-key",
                SubscriptionHeaderValue = ConfigurationManager.AppSettings["ServiceNowApiSubscriptionKey"]
            };
        }

        private static int HandleArgumentParsingError(IEnumerable<Error> errors)
        {
            Console.WriteLine($"Argument Parsing Errors: {JsonConvert.SerializeObject(errors)}");
            throw new ArgumentException("Failed to parse command line arguments");
        }

        private static 
            (AzureDevOpsSettings adoSettings, 
            AzureDevOpsTokenHandler tokenHandler, 
            VssConnectionFactory vssConnectionFactory)
            GetAdoObjects()
        {
            var adoSettings = GetAzureDevOpsSettings();
            var tokenHandler = new AzureDevOpsTokenHandler(adoSettings);
            var vssConnectionFactory = new VssConnectionFactory(tokenHandler);

            return (adoSettings, tokenHandler, vssConnectionFactory);
        }

        private static object RunActivityFailedAndReturnExitCode(
            ActivityFailedOptions opts)
        {
            var crLogic = CreateChangeRequestLogic();
            return crLogic.CompleteActivity(opts, false);
        }

        private static object RunActivitySuccessAndReturnExitCode(
            ActivitySuccessOptions opts)
        {
            ChangeRequestLogic crLogic = CreateChangeRequestLogic();
            return crLogic.CompleteActivity(opts, true);
        }

        private static ChangeRequestLogic CreateChangeRequestLogic()
        {
            var (adoSettings, tokenHandler, vssConnectionFactory) = GetAdoObjects();
            var snSettings = GetServiceNowSettings();
            var aikidoSettings = GetAikidoSettings();

            var crLogic = new ChangeRequestLogic(adoSettings, snSettings, tokenHandler, vssConnectionFactory, aikidoSettings);
            return crLogic;
        }

        public static object RunCreateChangeRequestAndReturnExitCode(
            CreateCrOptions arguments)
        {
            var crLogic = CreateChangeRequestLogic();

            arguments.ExistingCr = arguments.ExistingCr.Replace("'", string.Empty);
            crLogic.CreateChangeRequest(arguments);
            return 0;
        }

        public static object RunSetReleasePipelineVariableValueAndReturnExitCode(
            SetReleaseVariableOptions arguments)
        {
            var (adoSettings, tokenHandler, _) = GetAdoObjects();
            var releaseLogic = new ReleaseLogic(arguments.TeamProjectName, adoSettings, tokenHandler);

            var variableNamesAndValues = new Dictionary<string, string>
            {
                { arguments.VariableName, arguments.VariableValue }
            };

            releaseLogic.UpdateReleaseVariables(arguments.ReleaseId, variableNamesAndValues);
            
            return 0;
        }

        private static object RunCancelChangeRequestNum(CancelCrsOptions opts)
        {
            var crLogic = CreateChangeRequestLogic();

            crLogic.CancelCrs(opts);

            return 0;
        }
    }
}
