namespace ServiceNowCLI.Core.Arguments
{
    public class ImpactQuestionResponses
    {
        public bool OutageOrRestrictedFunctionality { get; set; }
        public bool ServiceImpactedOnFailure { get; set; }
        public string Criticality { get; set; }
    }

    public class RiskQuestionResponses
    {
        public bool Question1 { get; set; }
        public bool Question2 { get; set; }
        public bool Question3 { get; set; }
        public bool Question4 { get; set; }
        public bool Question5 { get; set; }
        public bool Question6 { get; set; }
    }

    public class CreateChangeRequestInput
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public string Reason { get; set; }
        public string Notes { get; set; }
        public string ScheduledStartDate { get; set; }
        public string ScheduledEndDate { get; set; }
        public InstallInstructions ImplementationPlan { get; set; }
        public string TestPlan { get; set; }
        public BranchingStrategies BranchingStrategy { get; set; }
        public string TeamProjectName { get; set; }

        #region ServiceNow fields
        public string assignment_group { get; set; }
        public string backout_plan { get; set; }
        public string business_service { get; set; }
        public string category { get; set; }
        public string chg_model { get; set; }
        public string correlation_id { get; set; }
        public string impact { get; set; }
        public string implementation_plan { get; set; }
        public string justification { get; set; }
        public string priority { get; set; }
        public string reason { get; set; }
        public string requested_by { get; set; }
        public string risk { get; set; }
        public string risk_impact_analysis { get; set; }
        public string std_change_producer_version { get; set; }
        public string test_plan { get; set; }
        public string type { get; set; }
        public string urgency { get; set; }
        #endregion
    }
}
