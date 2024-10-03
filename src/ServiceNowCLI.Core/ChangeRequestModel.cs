using ServiceNowCLI.Core.Arguments;
using ServiceNowCLI.Core.ServiceNow;
using System;

namespace ServiceNowCLI.Core
{
    public class ChangeRequestModel : ISnCreateChangeRequestModel
    {
        private const string DateTimeFormat = "dd.MM.yyyy HH:mm:ss";

        public ChangeRequestModel(CreateChangeRequestInput inputs)
        {
            // dates should be in GMT, SM will shift to local
            var parser = new Chronic.Parser();
            var scheduledStartDate = parser.Parse(inputs.ScheduledStartDate);
            if (scheduledStartDate == null)
            {
                throw new ArgumentException("Unable to parse the Scheduled Start Time, please use a known format from Chronic, https://github.com/robertwilczynski/nChronic");
            }
            ScheduledStartDate = DateTime.UtcNow;
            if (scheduledStartDate.Start != null) 
                ScheduledStartDate = ((DateTime) scheduledStartDate.Start).ToUniversalTime();

            var scheduledEndDate = parser.Parse(inputs.ScheduledEndDate);
            if (scheduledEndDate == null)
            {
                throw new ArgumentException("Unable to parse the Scheduled End Time, please use a known format from Chronic, https://github.com/robertwilczynski/nChronic");
            }

            ScheduledEndDate = DateTime.UtcNow;
            if (scheduledEndDate.Start != null)
                ScheduledEndDate = ((DateTime)scheduledEndDate.Start).ToUniversalTime();

            start_date = ScheduledStartDate.ToString(DateTimeFormat);
            end_date = ScheduledEndDate.ToString(DateTimeFormat);
            short_description = inputs.Title;
            description = inputs.Description;
            justification = inputs.Reason;
            work_notes = inputs.Notes;
            test_plan = inputs.TestPlan;

            assignment_group = inputs.assignment_group;
            backout_plan = inputs.backout_plan;
            business_service = inputs.business_service;
            category = inputs.category;
            chg_model = inputs.chg_model;
            correlation_id = inputs.correlation_id;
            impact = inputs.impact;
            implementation_plan = inputs.implementation_plan;
            priority = inputs.priority;
            reason = inputs.reason;
            requested_by = inputs.requested_by;
            risk = inputs.risk;
            risk_impact_analysis = inputs.risk_impact_analysis;
            std_change_producer_version = inputs.std_change_producer_version;
            type = inputs.type;
            urgency = inputs.urgency;
        }

        public string assignment_group { get; set; }
        public string backout_plan { get; set; }
        public string business_service { get; set; }
        public string category { get; set; }
        public string chg_model { get; set; }
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
        public string short_description { get; set; }
        public string start_date { get; set; }
        public string std_change_producer_version { get; set; }
        public string test_plan { get; set; }
        public string type { get; set; }
        public string urgency { get; set; }
        public string work_notes { get; set; }

        public DateTime ScheduledStartDate { get; set; }
        public DateTime ScheduledEndDate { get; set; }
    }
}
