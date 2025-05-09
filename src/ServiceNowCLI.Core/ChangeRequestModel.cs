﻿using ServiceNowCLI.Core.Arguments;
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
            var parser = new Chronic.Core.Parser();
            var scheduledStartDate = parser.Parse(inputs.ScheduledStartDate) 
                ?? throw new ArgumentException("Unable to parse the Scheduled Start Time, please use a known format from Chronic, https://github.com/robertwilczynski/nChronic");

            ScheduledStartDate = DateTime.UtcNow;
            if (scheduledStartDate.Start != null) 
                ScheduledStartDate = ((DateTime) scheduledStartDate.Start).ToUniversalTime();

            var scheduledEndDate = parser.Parse(inputs.ScheduledEndDate) 
                ?? throw new ArgumentException("Unable to parse the Scheduled End Time, please use a known format from Chronic, https://github.com/robertwilczynski/nChronic");

            ScheduledEndDate = DateTime.UtcNow;
            if (scheduledEndDate.Start != null)
                ScheduledEndDate = ((DateTime)scheduledEndDate.Start).ToUniversalTime();

            start_date = ScheduledStartDate.ToString(DateTimeFormat);
            end_date = ScheduledEndDate.ToString(DateTimeFormat);            

            assignment_group = inputs.assignment_group;
            backout_plan = inputs.backout_plan;
            business_service = inputs.business_service;
            category = inputs.category;
            chg_model = inputs.chg_model;
            cmdb_ci = inputs.cmdb_ci;
            correlation_id = inputs.correlation_id;
            description = inputs.description;
            impact = inputs.impact;
            implementation_plan = inputs.implementation_plan;
            justification = inputs.justification;
            priority = inputs.priority;
            reason = inputs.reason;
            requested_by = inputs.requested_by;
            risk = inputs.risk;
            risk_impact_analysis = inputs.risk_impact_analysis;
            service_offering = inputs.service_offering;
            short_description = inputs.short_description;
            std_change_producer_version = inputs.std_change_producer_version;
            test_plan = inputs.test_plan;
            type = inputs.type;
            urgency = inputs.urgency;
            work_notes = inputs.work_notes;
        }

        public string assignment_group { get; set; }
        public string backout_plan { get; set; }
        public string business_service { get; set; }
        public string category { get; set; }
        public string chg_model { get; set; }
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
        public string std_change_producer_version { get; set; }
        public string test_plan { get; set; }
        public string type { get; set; }
        public string urgency { get; set; }
        public string work_notes { get; set; }

        public DateTime ScheduledStartDate { get; set; }
        public DateTime ScheduledEndDate { get; set; }
    }
}
