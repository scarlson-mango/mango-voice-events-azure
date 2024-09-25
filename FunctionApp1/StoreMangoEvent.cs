using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.Sql;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Microsoft.Azure.Functions.Worker.Http;
using System.Net;

namespace AzureApp
{
    public class StoreMangoEvent
    {
        private readonly ILogger<StoreMangoEvent> _logger;

        public StoreMangoEvent(ILogger<StoreMangoEvent> logger)
        {
            _logger = logger;
        }

        [Function("StoreMangoEvent")]        
        public async Task<Output> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "mango_event")] HttpRequest req,
            FunctionContext context)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");

            // X-Mango-Remote-ID check
            string? remoteId = Environment.GetEnvironmentVariable("X-Mango-Remote-ID");
            if (remoteId != null)
            {
                req.Headers.TryGetValue("X-Mango-Remote-ID", out var remoteIdHeader);
                if (remoteIdHeader.Count == 0)
                {
                    string error = "Missing header \"X-Mango-Remote-ID\"";
                    _logger.LogInformation(error);
                    return new Output(error);
                }
                else if (remoteIdHeader[0] != remoteId)
                {
                    string error = $"Invalid header \"X-Mango-Remote-ID\": {remoteIdHeader[0]}";
                    _logger.LogInformation(error);
                    return new Output(error);
                }
            }
            else
            {
                _logger.LogInformation("No environment variable found for \"X-Mango-Remote-ID\"... Skipping check.");
            }
            
            // Parse JSON request body
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            MangoEvent mangoEvent = JsonConvert.DeserializeObject<MangoEvent>(requestBody) ?? new();

            _logger.LogInformation(mangoEvent.ToString());

            MangoEventDTO dto = new(mangoEvent);

            return new Output(dto);
        }
    }

    public class MangoEvent
    {
        public Guid Id { get; set; }
        [JsonProperty("event_originated_at")]
        public long CreatedAt { get; set; }
        [JsonProperty("webhook_sent_at")]
        public long SentAt { get; set; }
        public string Type { get; set; } = "";
        public JObject Payload { get; set; } = new();

        public override string ToString()
        {
            return $"{{" +
                $"\"id\":\"{Id}\"\n" +
                $"\"created_at\":\"{CreatedAt}\"\n" +
                $"\"type\":\"{Type}\"\n" +
                $"\"payload\":{Payload.ToString()}" +
                $"}}";
        }
    }

    public class MangoEventDTO    
    {
        [Key]
        public Guid Id { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public DateTime SentAt { get; private set; }
        public string Type { get; private set; }
        public string Payload { get; private set; }

        public MangoEventDTO(MangoEvent mangoEvent) {
            Id = mangoEvent.Id;
            CreatedAt = DateTimeOffset.FromUnixTimeSeconds(mangoEvent.CreatedAt).DateTime;
            SentAt = DateTimeOffset.FromUnixTimeSeconds(mangoEvent.SentAt).DateTime;
            Type = mangoEvent.Type;
            Payload = mangoEvent.Payload.ToString();
        }
    }

    public class Output
    {
        public Output(string errorMessage)
        {
            Response = new BadRequestObjectResult(errorMessage);
        }
        public Output(MangoEventDTO dto)
        {
            DTO = dto;
            Response = new OkResult();
        }

        [SqlOutput("dbo.MangoEvents", "AzureDbConnectionString")]
        public MangoEventDTO? DTO { get; private set; }

        [HttpResult]
        public IActionResult Response { get; private set; }
    }
}