using Microsoft.VisualStudio.Services.Common.CommandLine;
using Newtonsoft.Json;
using RestSharp;
using RestSharp.Authenticators;
using RestSharp.Serializers.Json;
using ServiceNowCLI.Config.Dtos;
using ServiceNowCLI.Core.Extensions;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ServiceNowCLI.Core.ServiceNow
{
    internal class ServiceNowHttpClient : IDisposable
    {
        private const string crUri = "/change_request";

        private readonly Dictionary<CrTypes, CrFlowConfiguration> configurations;
        readonly RestClient _client;
        private bool disposedValue;

        public ServiceNowHttpClient(ServiceNowSettings settings, string bearerToken)
        {
            var options = new RestClientOptions(settings.ApiUrl)
            {
                Authenticator = new JwtAuthenticator(bearerToken),
            };

            _client = new RestClient(options,
                configureSerialization: s =>
                {
                    s.UseSystemTextJson(new JsonSerializerOptions() { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull });
                });

            _client.AddDefaultHeader(settings.SubscriptionHeaderName, settings.SubscriptionHeaderValue);
            configurations = CrFlowConfiguration.GetDefault();
        }

        public string CreateCR(ISnCreateChangeRequestModel cr)
        {
            var restRequest = new RestRequest(crUri);
            restRequest.AddJsonBody(cr);
            var response = _client.Post(restRequest);
            if (response.StatusCode == System.Net.HttpStatusCode.Created)
            {
                var res = JsonConvert.DeserializeObject<ResultObject<SnChangeRequestResponseModel>>(response.Content);
                var state = res.result.state.ToEnum<CrStates>();
                var type = res.result.type.ToEnum<CrTypes>();
                if (state != configurations[type].DesiredStateAfterCreation)
                {
                    UpdateCRStateFromTo(res.result.sys_id, res.result.type.ToEnum<CrTypes>(), state, configurations[type].DesiredStateAfterCreation);
                }
                return res.result.number;
            }

            throw new ArgumentException($"CR creation failed with code {response.StatusCode}, content: {response.Content}");
        }

        private void UpdateCRStateFromTo(string sys_id, CrTypes type, CrStates fromState, CrStates toState)
        {
            var currentState = fromState;
            while (currentState != toState)
            {
                currentState = configurations[type].GetNextState(currentState);
                if (!SetCrStateTo(sys_id, currentState))
                {
                    break;
                }
            }
        }

        public bool CompleteCR(string number, bool successfully = true)
        {
            var cr = GetCrByNumber(number);
            if (cr != null)
            {
                var crType = cr.type.ToEnum<CrTypes>();
                var crState = cr.state.ToEnum<CrStates>(); 
                var preCloseState = configurations[crType].CrWorkflow.Find(CrStates.Closed).Previous.Value;
                UpdateCRStateFromTo(cr.sys_id, crType, crState, preCloseState);
                return SetCrStateClosed(cr.sys_id, successfully ? CrCloseCodes.successful : CrCloseCodes.unsuccessful, configurations[crType].ClosedStateParams);
            }
            return false;
        }

        public bool CancelCRByNumber(string number)
        {
            return ChangeCrStateByNumber(number, CrStates.Cancelled, "cancelled by request");
        }

        public SnChangeRequestResponseModel GetCrByNumber(string number)
        {
            var restRequest = new RestRequest($"{crUri}?number={number}");
            var response = _client.Get(restRequest);
            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                var res = JsonConvert.DeserializeObject<ResultArray<SnChangeRequestResponseModel>>(response.Content);
                if (res.result.Length == 1)
                {
                    return res.result[0];
                }
                else
                {
                    Console.WriteLine($"Several CRs found by number = {number}");
                }
            }
            else
            {
                Console.WriteLine($"set CR {number} state failed: {response.StatusCode} {response.ErrorMessage}, {response.Content}");
            }
            return null;
        }


        public bool ChangeCrStateByNumber(string number, CrStates newState, string reason)
        {
            var cr = GetCrByNumber(number);

            if (cr != null) 
            {
                return SetCrStateTo(cr.sys_id, newState, reason);
            }

            return false;
        }

        private bool SetCrStateWithBody(string sysId, CrStates newState, SnChangeRequestModel body)
        {
            body.state = newState.ToString("d");
            var changeCrRequest = new RestRequest($"{crUri}/{sysId}");
            changeCrRequest.AddJsonBody(body);

            var response = _client.ExecutePut(changeCrRequest);

            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                var changedCr = JsonConvert.DeserializeObject<ResultObject<SnChangeRequestResponseModel>>(response.Content);
                if (changedCr.result.state.ToEnum<CrStates>() == newState)
                {
                    return true;
                }
                else
                {
                    Console.WriteLine($"State for CR sys_id = {sysId} was not changed and left {changedCr.result.state}");
                }
            }
            else
            {
                Console.WriteLine($"changing state failed: {response.StatusCode} {response.ErrorMessage}, {response.Content}");
            }

            return false;
        }

        private bool SetCrStateTo(string sysId, CrStates newState, string reason = null)
        {
            return SetCrStateWithBody(sysId, newState, new SnChangeRequestModel
            {
                reason = reason,
            });
        }

        private bool SetCrStateClosed(string sysId, CrCloseCodes close_code, Dictionary<string, string> closeParams)
        {
            return SetCrStateWithBody(sysId, CrStates.Closed, new SnChangeRequestModel
            {
                close_code = close_code.ToString(),
                close_notes = closeParams[CloseFields.CloseNotes],
                reason = closeParams.GetValueOrDefault(CloseFields.Reason) ?? ""
            });
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _client?.Dispose();
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
