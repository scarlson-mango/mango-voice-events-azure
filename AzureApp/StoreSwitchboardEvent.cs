using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using Newtonsoft.Json.Linq;
using Microsoft.AspNetCore.Http.Extensions;

namespace AzureApp
{
    public static class StoreSwitchboardEvent
    {
        [FunctionName("StoreSwitchboardEvent")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "switchboard_event")] HttpRequest req,
            [Sql("dbo.SwitchboardEvents", "AzureDbConnectionString")] IAsyncCollector<SwitchboardEvent> output,
            ILogger log)
        {
            log.LogInformation($"C# HTTP trigger function processed a request.");

            var switchboardEvent = ParseQueryStrings(req.Query);
            log.LogInformation(req.QueryString.ToString());

            await output.AddAsync(switchboardEvent);

            return new OkResult();
        }

        private static SwitchboardEvent ParseQueryStrings(IQueryCollection query)
        {
            SwitchboardEvent se = new();
            JObject customStrings = new();

            foreach (var pair in query) {
                switch (pair.Key) {
                    case "callerid": se.CallerId = pair.Value; break;
                    case "caller_number": se.CallerNumber = pair.Value; break;
                    case "number_dialed": se.NumberDialed = pair.Value; break;
                    case "call_uid": se.CallId = Guid.Parse(pair.Value); break;
                    case "dialed_number_name": se.DialedNumberName = pair.Value; break;
                    case "time_utc": se.CreatedAt = DateTime.Parse(pair.Value); break;
                    default:
                        customStrings[pair.Key] = pair.Value.ToString(); break;
                }
            }

            se.Payload = customStrings.ToString();

            return se;
        }

        public struct SwitchboardEvent
        {
            public Guid CallId { get; set; }
            public string CallerId { get; set; }
            public string CallerNumber { get; set; }
            public string NumberDialed { get; set; }
            public string DialedNumberName { get; set; }
            public DateTime CreatedAt { get; set; }
            public string Payload { get; set; }
        }
    }
}
