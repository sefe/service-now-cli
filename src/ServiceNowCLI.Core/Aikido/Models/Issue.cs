using System;

namespace ServiceNowCLI.Core.Aikido.Models
{
    public class Issue
    {
        public int id { get; set; } 
        public int group_id { get; set; } 
        public string type { get; set; } 
        public string attack_surface { get; set; } 
        public int severity_score { get; set; } 
        public string severity { get; set; } 
        public string status { get; set; } 
        public long first_detected_at { get; set; } 

        public string rule { get; set; } 
        public string rule_id { get; set; } 
        public string affected_package { get; set; } 
        public string affected_file { get; set; } 
        public string cve_id { get; set; } 
        public int? code_repo_id { get; set; } 
        public string code_repo_name { get; set; } 
        public int? cloud_id { get; set; } 
        public string cloud_name { get; set; } 
        public int? container_repo_id { get; set; } 
        public string container_repo_name { get; set; } 
        public int? sla_days { get; set; } 
        public long? sla_remediate_by { get; set; } 
        public long? ignored_at { get; set; } 
        public string ignored_by { get; set; } 
        public long? closed_at { get; set; } 
        public int? start_line { get; set; } 
        public int? end_line { get; set; } 
        public long? snooze_until { get; set; }
        
        public DateTime FirstDetectedAtDate => DateTimeOffset.FromUnixTimeSeconds(first_detected_at).DateTime;
        public DateTime? SlaRemediateByDate => sla_remediate_by.HasValue ? DateTimeOffset.FromUnixTimeSeconds(sla_remediate_by.Value).DateTime : null;
        public DateTime? IgnoredAtDate => ignored_at.HasValue ? DateTimeOffset.FromUnixTimeSeconds(ignored_at.Value).DateTime : null;
        public DateTime? ClosedAtDate => closed_at.HasValue ? DateTimeOffset.FromUnixTimeSeconds(closed_at.Value).DateTime : null;
        public DateTime? SnoozeUntilDate => snooze_until.HasValue ? DateTimeOffset.FromUnixTimeSeconds(snooze_until.Value).DateTime : null;
    }

    public static class IssueStatuses
    {
        public static string Open = "open";
        public static string Ignored = "ignored";
        public static string Snoozed = "snoozed";
        public static string Closed = "closed";
    }
}
