using System.Collections.Generic;

namespace ServiceNowCLI.Core.Aikido.Models
{
    public class IssueGroup
    {
        // Required properties
        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; } // Can be null
        public string Type { get; set; } // Enum could be used for predefined values
        public int Severity_Score { get; set; }
        public string Severity { get; set; } // Enum could be used for predefined values
        public string Group_Status { get; set; } // Enum could be used for predefined values
        public int Time_To_Fix_Minutes { get; set; }
        public List<Location> Locations { get; set; } // A list of location objects
    }

    public class Location
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Type { get; set; } // Enum could be used for predefined values
    }
}
