namespace ServiceNowCLI.Core.ServiceNow
{
    public interface ISnCreateChangeRequestModel
    {
        string assignment_group { get; set; }
        string backout_plan { get; set; }
        string business_service { get; set; }
        string category { get; set; }
        string chg_model { get; set; }
        string cmdb_ci { get; set; }
        string correlation_id { get; set; }
        string description { get; set; }
        string end_date { get; set; }
        string impact { get; set; }
        string implementation_plan { get; set; }
        string justification { get; set; }
        string priority { get; set; }
        string reason { get; set; }
        string requested_by { get; set; }
        string risk { get; set; }
        string risk_impact_analysis { get; set; }
        string service_offering { get; set; }
        string short_description { get; set; }
        string start_date { get; set; }
        string std_change_producer_version { get; set; }
        string test_plan { get; set; }
        string type { get; set; }
        string urgency { get; set; }
        string work_notes { get; set; }
    }
}