using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using Parlot.Fluent;
using ServiceNowCLI.Config.Dtos;
using System;
using System.IO;
using System.Linq;

namespace ServiceNowCLI.Core.ServiceNow
{
    public class ServiceNowLogic
    {
        private readonly ServiceNowHttpClient _httpClient;
        private SnChangeRequestResponseModel createdCr = null;

        public ServiceNowLogic(ServiceNowSettings settings, string token)
        {
            _httpClient = new ServiceNowHttpClient(settings, token);
        }

        public string CreateChangeRequest(ChangeRequestModel cr)
        {
            Console.WriteLine($"Submitting CR: {JsonConvert.SerializeObject(cr, Formatting.Indented)}");
            try
            {
                createdCr = _httpClient.CreateCR(cr);

                return createdCr.number;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"CR creation failed: {ex.Message}");

                throw;
            }
        }

        public int CompleteCR(string number, bool successfully = true, string closeNote = "")
        {
            string note = !string.IsNullOrEmpty(closeNote) ? $" with note: {closeNote}" : "";

            Console.WriteLine($"Closing CR {number} as {successfully}{note}...");

            var isCompleted = _httpClient.CompleteCR(number, successfully, closeNote);

            if (isCompleted)
            {
                Console.WriteLine($"Closing CR {number} as {successfully} has been finished.");
            }
            else
            {
                return -1;
            }

            return 0;
        }

        public void CancelCrs(string crs)
        {
            var nums = crs.Split(',').Select(p => p.Trim()).Where(n => !string.IsNullOrWhiteSpace(n)).ToList();

            foreach (var number in nums)
            {
                var isOk = _httpClient.CancelCRByNumber(number);

                if (isOk)
                {
                    Console.WriteLine($"CR {number} has been cancelled.");
                }
            }
        }

        public void AttachFileToCreatedCr(string crNumber, Stream stream, string filename)
        {
            Console.WriteLine($"Attaching file {filename} to CR {crNumber}...");
            if (createdCr.number != crNumber)
            {
                Console.WriteLine($"CR {crNumber} is not created in this session. Please create it first.");
                return;
            }

            _httpClient.AttachFileToCr(createdCr.sys_id, stream, filename);            
        }
    }
}
