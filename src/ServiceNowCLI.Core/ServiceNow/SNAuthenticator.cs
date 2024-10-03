using RestSharp;
using RestSharp.Authenticators;
using System.Threading.Tasks;

namespace ServiceNowCLI.Core.ServiceNow
{
    internal class SNAuthenticator : AuthenticatorBase
    {
        private readonly string subscrId;

        public SNAuthenticator(string subscriptionId) : base("")
        {
            this.subscrId = subscriptionId;
        }

        protected override ValueTask<Parameter> GetAuthenticationParameter(string accessToken)
        {
            var result = new HeaderParameter("ocp-apim-subscription-key", subscrId);
            return new ValueTask<Parameter>(result);
        }
    }
}
