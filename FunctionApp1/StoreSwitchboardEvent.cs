using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.Sql;

namespace AzureApp
{
    public class StoreSwitchboardEvent
    {
        private readonly ILogger<StoreSwitchboardEvent> _logger;

        public StoreSwitchboardEvent(ILogger<StoreSwitchboardEvent> logger)
        {
            _logger = logger;
        }

        [Function("StoreSwitchboardEvent")]
        [SqlOutput("dbo.SwitchboardEvents", connectionStringSetting: "AzureDbConnectionString")]
        public SwitchboardEvent Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "switchboard_event")] HttpRequest req)
        {
            _logger.LogInformation($"C# HTTP trigger function processed a request.");

            var switchboardEvent = ParseQueryStrings(req.Query);
            _logger.LogInformation(req.QueryString.ToString());

            return switchboardEvent;
        }

        private static SwitchboardEvent ParseQueryStrings(IQueryCollection query)
        {
            SwitchboardEvent se = new();
            JObject customStrings = new();

            foreach (var pair in query) {
                switch (pair.Key) {
                    case "callerid": se.CallerId = pair.Value.ToString(); break;
                    case "caller_number": se.CallerNumber = pair.Value.ToString(); break;
                    case "number_dialed": se.NumberDialed = pair.Value.ToString(); break;
                    case "call_uid": se.CallId = Guid.Parse(pair.Value.ToString()); break;
                    case "dialed_number_name": se.DialedNumberName = pair.Value.ToString(); break;
                    case "time_utc": se.CreatedAt = DateTime.Parse(pair.Value.ToString()); break;
                    default:
                        customStrings[pair.Key] = pair.Value.ToString(); break;
                }
            }

            se.Payload = customStrings.ToString();

            return se;
        }

        
        public struct SwitchboardEvent
        {
            //[SqlOutput("Mango_InitialStage.SwitchboardEvents", connectionStringSetting: "AzureDbConnectionString")]

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
