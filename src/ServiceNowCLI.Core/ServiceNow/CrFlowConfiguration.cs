using System.Collections.Generic;

namespace ServiceNowCLI.Core.ServiceNow
{
    internal class CrFlowConfiguration
    {
        /// <summary>
        /// dictionary of linked lists of all possible states for CR per type
        /// </summary>
        public LinkedList<CrStates> CrWorkflow;
        public CrStates DesiredStateAfterCreation; // "Scheduled"
        public Dictionary<string, string> ClosedStateParams; // close_code: successful, close_notes: "auto closed from API"

        public CrStates GetNextState(CrStates state)
        {
            return CrWorkflow.Find(state).Next.Value;
        }

        public static Dictionary<CrTypes, CrFlowConfiguration> GetDefault()
        {
            CrStates[] arrayStandard = { CrStates.New, CrStates.Scheduled, CrStates.Implement, CrStates.Review, CrStates.Closed };
            CrStates[] arrayNormal = { CrStates.New, CrStates.Assess, CrStates.Authorize, CrStates.Scheduled, CrStates.Implement, CrStates.Review, CrStates.Closed };
            CrStates[] arrayEmergency = { CrStates.New, CrStates.Authorize, CrStates.Scheduled, CrStates.Implement, CrStates.Review, CrStates.Closed };

            var defaultCloseParams = new Dictionary<string, string>()
                {
                    { CloseFields.CloseNotes,  "auto closed from API"}
                };

            CrFlowConfiguration getConfig(CrStates[] arr, Dictionary<string, string> par, CrStates st)
            {
                return new CrFlowConfiguration()
                {
                    CrWorkflow = new LinkedList<CrStates>(arr),
                    ClosedStateParams = par,
                    DesiredStateAfterCreation = st
                };
            }

            var configStd = getConfig(arrayStandard, defaultCloseParams, CrStates.Implement);
            var configNor = getConfig(arrayNormal, defaultCloseParams, CrStates.New);
            var configEme = getConfig(arrayEmergency, defaultCloseParams, CrStates.Authorize);

            return new Dictionary<CrTypes, CrFlowConfiguration>
                {
                    { CrTypes.Standard, configStd },
                    { CrTypes.Normal,  configNor },
                    { CrTypes.Emergency,  configEme }
                };
        }
    }
}

