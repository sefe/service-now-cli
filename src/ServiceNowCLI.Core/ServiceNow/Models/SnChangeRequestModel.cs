namespace ServiceNowCLI.Core.ServiceNow
{
    public class SnChangeRequestModel : ISnCreateChangeRequestModel
    {
        public string assignment_group { get; set; }
        public string backout_plan { get; set; }
        public string business_service { get; set; }
        public string category { get; set; }
        public string chg_model { get; set; }
        public string close_code { get; set; }
        public string close_notes { get; set; }
        public string cmdb_ci { get; set; }
        public string correlation_id { get; set; }
        public string description { get; set; }
        public string end_date { get; set; }
        public string impact { get; set; }
        public string implementation_plan { get; set; }
        public string justification { get; set; }
        public string priority { get; set; }
        public string reason { get; set; }
        public string requested_by { get; set; }
        public string risk { get; set; }
        public string risk_impact_analysis { get; set; }
        public string service_offering { get; set; }
        public string short_description { get; set; }
        public string start_date { get; set; }
        public string state { get; set; }
        public string std_change_producer_version { get; set; }
        public string test_plan { get; set; }
        public string type { get; set; }
        public string urgency { get; set; }
        public string work_notes { get; set; }
    }
}
