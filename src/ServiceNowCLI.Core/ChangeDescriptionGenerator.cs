using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;

namespace ServiceNowCLI.Core
{
    public class ChangeDescriptionGenerator
    {
        public List<string> GenerateChangeDescription(List<WorkItem> linkedWorkItems)
        {
            Dictionary<string, bool> changeDescriptions = new Dictionary<string, bool>();

            foreach (var workItem in linkedWorkItems)
            {
                var workItemType = workItem.Fields["System.WorkItemType"].ToString();
                var workItemTitle = workItem.Fields["System.Title"].ToString();
                var changeDescription = $"{workItemType} {workItem.Id}: {workItemTitle}";

                if (!changeDescriptions.Keys.Contains(changeDescription))
                {
                    changeDescriptions.Add(changeDescription, true);
                }
            }

            if (!changeDescriptions.Any())
            {
                throw new ArgumentException(
                    "No linked PBI's have been found on this build to be able to attach to CR!");
            }

            var sortedChangeDescriptions = changeDescriptions.Keys.OrderBy(key => key);
            return sortedChangeDescriptions.ToList();
        }
    }
}