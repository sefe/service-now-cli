using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using CommandLine;
using Newtonsoft.Json;
using ServiceNowCLI.Config;
using ServiceNowCLI.Config.Dtos;
using ServiceNowCLI.Core.Arguments;
using ServiceNowCLI.Core.AzureDevOps;

namespace ServiceNowCLI
{
    // Sample cmd line -
    // createcr -s "http://ServiceNowApi" -c "https://vsrm.dev.azure.com/org" -b "BuildNumber_21.11.22.1" -r "ReleaseNumber_21.11.22.1-Release_01" -e "AzEnvironmentName" -u "user" -p "cr-inputs.json" -i "15151" -m "comm-inputs.json"
    // createcr -s "http://ServiceNowApi" -c "https://vsrm.dev.azure.com/org" -b "BuildNumber_21.11.22.1" -r "ReleaseNumber_21.11.22.1-Release_01" -e "AzEnvironmentName" -u "user" -p "cr-inputs.json" -i "15151" -m "comm-inputs.json" -x "CR18962"
    // activitysuccess -s "http://ServiceNowApi" -c "https://vsrm.dev.azure.com/org" -a "CR Implementation Phase" -c "CR8188" 
    // setreleasevariable -c "https://vsrm.dev.azure.com/org" -t "ProjectName" -i "13157" -n "Comms_ReleaseID" -v "123456"
    // createcr -s "http://ServiceNowApi" -c "https://vsrm.dev.azure.com/org" -b "BuildNumber_23.11.3.2" -r "ReleaseNumber_23.11.3.2-Release_01" -e "AzEnvironmentName" -u "user" -p "cr-inputs.json" -i "35313" -m "comm-inputs.json"


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
                Console.ReadKey();
            }

            return returnCode;
        }

        private static void ProcessCommand(string[] args)
        {
            var activity = new Activity("ProcessCommand");
            activity.Start();

            Console.WriteLine($"Starting ServiceNowCLI - command line arguments: {JsonConvert.SerializeObject(args)}");

            var adoSettings = GetAzureDevOpsSettings();
            var tokenHandler = new AzureDevOpsTokenHandler(adoSettings);
            var vssConnectionFactory = new VssConnectionFactory(adoSettings, tokenHandler);

            Parser.Default.ParseArguments<CreateCrOptions, ActivitySuccessOptions, ActivityFailedOptions, SetReleaseVariableOptions, CancelCrsOptions>(args)
                .MapResult(
                    (CreateCrOptions opts) => RunCreateChangeRequestAndReturnExitCode(opts, adoSettings, tokenHandler, vssConnectionFactory),
                    (ActivitySuccessOptions opts) => RunActivitySuccessAndReturnExitCode(opts, adoSettings, tokenHandler, vssConnectionFactory),
                    (ActivityFailedOptions opts) => RunActivityFailedAndReturnExitCode(opts, adoSettings, tokenHandler, vssConnectionFactory),
                    (SetReleaseVariableOptions opts) => RunSetReleasePipelineVariableValueAndReturnExitCode(opts, adoSettings, tokenHandler),
                    (CancelCrsOptions opts) => RunCancelChangeRequestNum(opts, adoSettings, tokenHandler, vssConnectionFactory),
                    errs => HandleArgumentParsingError(errs));

            activity.Stop();

            Console.WriteLine($"Finished - time taken = {activity.Duration:g}");
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

        private static int HandleArgumentParsingError(IEnumerable<Error> errors)
        {
            Console.WriteLine($"Argument Parsing Errors: {JsonConvert.SerializeObject(errors)}");
            throw new ArgumentException("Failed to parse command line arguments");
        }

        private static object RunActivityFailedAndReturnExitCode(
            ActivityFailedOptions opts, 
            AzureDevOpsSettings adoSettings, 
            IAzureDevOpsTokenHandler tokenHandler,
            VssConnectionFactory vssConnectionFactory)
        {
            var crLogic = new ChangeRequestLogic(adoSettings, tokenHandler, vssConnectionFactory);
            return crLogic.CompleteActivity(opts, false);
        }

        private static object RunActivitySuccessAndReturnExitCode(
            ActivitySuccessOptions opts, 
            AzureDevOpsSettings adoSettings, 
            IAzureDevOpsTokenHandler tokenHandler,
            VssConnectionFactory vssConnectionFactory)
        {
            var crLogic = new ChangeRequestLogic(adoSettings, tokenHandler, vssConnectionFactory);
            return crLogic.CompleteActivity(opts, true);
        }

        public static object RunCreateChangeRequestAndReturnExitCode(
            CreateCrOptions arguments, 
            AzureDevOpsSettings adoSettings, 
            IAzureDevOpsTokenHandler tokenHandler,
            VssConnectionFactory vssConnectionFactory)
        {
            var crLogic = new ChangeRequestLogic(adoSettings, tokenHandler, vssConnectionFactory);
            arguments.ExistingCr = arguments.ExistingCr.Replace("'", string.Empty);
            crLogic.CreateChangeRequest(arguments);
            return 0;
        }

        public static object RunSetReleasePipelineVariableValueAndReturnExitCode(
            SetReleaseVariableOptions arguments, 
            AzureDevOpsSettings adoSettings, 
            IAzureDevOpsTokenHandler tokenHandler)
        {
            var releaseLogic = new ReleaseLogic(arguments.CollectionUri, arguments.TeamProjectName, adoSettings, tokenHandler);

            var variableNamesAndValues = new Dictionary<string, string>
            {
                { arguments.VariableName, arguments.VariableValue }
            };

            releaseLogic.UpdateReleaseVariables(arguments.ReleaseId, variableNamesAndValues);
            
            return 0;
        }

        private static object RunCancelChangeRequestNum(CancelCrsOptions opts, AzureDevOpsSettings adoSettings, AzureDevOpsTokenHandler tokenHandler, VssConnectionFactory vssConnectionFactory)
        {
            var crLogic = new ChangeRequestLogic(adoSettings, tokenHandler, vssConnectionFactory);
            crLogic.CancelCrs(opts);

            return 0;
        }
    }
}
