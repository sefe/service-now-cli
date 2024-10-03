using System;
using System.Collections.Generic;

namespace ServiceNowCLI.Core.ServiceNow
{
    internal class SnConfiguration
    {
        /// <summary>
        /// dictionary of linked lists of all possible states for CR per type
        /// </summary>
        public LinkedList<CrStates> CrWorkflow;
        public CrStates DesiredStateAfterCreation; // "Scheduled"
        public Dictionary<string, string> ClosedStateParams; // close_code: successful, close_notes: "auto closed from API"

        public SnConfiguration()
        {
            
        }

        public CrStates GetNextState(CrStates state)
        {
            return CrWorkflow.Find(state).Next.Value;
        }

        public static Dictionary<CrTypes, SnConfiguration> GetDefault()
        {
            CrStates[] arrayStandard = { CrStates.New, CrStates.Scheduled, CrStates.Implement, CrStates.Review, CrStates.Closed };
            CrStates[] arrayNormal = { CrStates.New, CrStates.Assess, CrStates.Authorize, CrStates.Scheduled, CrStates.Implement, CrStates.Review, CrStates.Closed };
            CrStates[] arrayEmergency = { CrStates.New, CrStates.Authorize, CrStates.Scheduled, CrStates.Implement, CrStates.Review, CrStates.Closed };

            var defaultCloseParams = new Dictionary<string, string>()
                {
                    { "close_notes",  "auto closed from API"}
                };

            SnConfiguration getConfig(CrStates[] arr, Dictionary<string, string> par, CrStates st)
            {
                return new SnConfiguration()
                {
                    CrWorkflow = new LinkedList<CrStates>(arr),
                    ClosedStateParams = par,
                    DesiredStateAfterCreation = st
                };
            }

            var configStd = getConfig(arrayStandard, defaultCloseParams, CrStates.Scheduled);
            var configNor = getConfig(arrayNormal, defaultCloseParams, CrStates.New);
            var configEme = getConfig(arrayEmergency, defaultCloseParams, CrStates.Authorize);

            return new Dictionary<CrTypes, SnConfiguration>
                {
                    { CrTypes.Standard, configStd },
                    { CrTypes.Normal,  configNor },
                    { CrTypes.Emergency,  configEme }
                };
        }
    }
}

