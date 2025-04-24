using CommandLine;

namespace ServiceNowCLI.Core.Arguments
{
    [Verb("createcr", HelpText = "Add file contents to the index.")]
    public class CreateCrOptions
    {
        [Option('b', "buildnumber", Required = true, HelpText = "Azure DevOps Build Number to attach to the Change Request.")]
        public string BuildNumber { get; set; }

        [Option('e', "environment", Required = true, HelpText = "If set to 'Prod' or 'Production', the tool will validate the branch name, and created tag in PBI won't contain 'dv'")]
        public string Environment { get; set; }

        [Option('u', "requestedby", Required = true, HelpText = "The display name of the identity that triggered (started) the deployment currently in progress.")]
        public string ReleaseDeploymentRequestedFor { get; set; }

        [Option('p', "crparamsfile", Required = true, HelpText = "The JSON File to use when constructing the CR, contains all required data to create a CR.")]
        public string CrParamsFile { get; set; }

        [Option('i', "releaseid", Required = false, Default = "", HelpText = "Azure DevOps Release ID, used for setting variable values.")]
        public string ReleaseId { get; set; }

        [Option('m', "commparamsfile", Required = false, Default = "", HelpText = "JSON File containing details required for sending out release comms.")]
        public string CommParamsFile { get; set; }

        [Option('w', "workItemLinking", Required = false, Default = "WorkItem", HelpText = "Flag to determine how linked work items should be found. Valid values are 'All' and 'WorkItem'.")]
        public string WorkItemLinking { get; set; }

        [Option('x', "existingCr", Required = false, Default = "", HelpText = "Existing CR number")]
        public string ExistingCr { get; set; }

        [Option('o', "outputCrNumberFile", Required = false, Default = "", HelpText = "File to write newly created CR number to. Useful for yaml pipelines as there is no possibility to set runtime variables via ADO API")]
        public string OutputCrNumberFile { get; set; }

        [Option('t', "transformtemplatefile", Required = false, HelpText = "The liquid template file which should be applied to crparamsfile before to produce the correct input.")]
        public string TransformTemplateFile { get; set; }

        public bool IncludeAllLinkedWorkItems => WorkItemLinking == "All";
    }

    public class SetActivityOptions
    {
        [Option('n', "closenote", Required = false, HelpText = "Close note for Change Request.")]
        public string CloseNote { get; set; }

        [Option('r', "changeno", Required = true, HelpText = "Change Request to Update, in format 'CR123456'.")]
        public string ChangeNo { get; set; }
    }

    [Verb("activitysuccess", HelpText = "Record changes to the repository.")]
    public class ActivitySuccessOptions : SetActivityOptions
    {

    }

    [Verb("activityfailed", HelpText = "Clone a repository into a new directory.")]
    public class ActivityFailedOptions : SetActivityOptions
    {
        
    }

    [Verb("cancelcrs", HelpText = "Cancel CR or the list of CRs.")]
    public class CancelCrsOptions
    {
        [Option('r', "changenumbers", Required = false, HelpText = "Comma separated list of Change Request Numbers to Cancel.")]
        public string ChangeNums { get; set; }
    }

    [Verb("setreleasevariable", HelpText = "Set variable value within a release pipeline.")]
    public class SetReleaseVariableOptions
    {
        [Option('i', "releaseid", Required = true, HelpText = "Azure DevOps Release ID.")]
        public string ReleaseId { get; set; }

        [Option('t', "teamprojectname", Required = false, HelpText = "Team Project Name, e.g. 'Data and Analytics'")]
        public string TeamProjectName { get; set; }

        [Option('n', "variablename", Required = false, HelpText = "Variable Name to be updated'")]
        public string VariableName { get; set; }

        [Option('v', "variablevalue", Required = false, HelpText = "Variable value'")]
        public string VariableValue { get; set; }
    }

    [Verb("generatesastreport", HelpText = "Read data and generate report from SAST.")]
    public class GenerateSastReportOptions
    {
        [Option('r', "reponame", Required = true, HelpText = "Repository name for which to generate report.")]
        public string RepoName { get; set; }

        [Option('f', "filename", Required = false, HelpText = "Report filename")]
        public string Filename { get; set; }
    }
}
