using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AzureApp
{
    public static class StoreMangoEvent
    {
        [FunctionName("StoreMangoEvent")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "mango_event")] HttpRequest req,
            [Sql("dbo.MangoEvents", "AzureDbConnectionString")] IAsyncCollector<MangoEventDTO> output,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            // X-Mango-Remote-ID check
            string remoteId = Environment.GetEnvironmentVariable("X-Mango-Remote-ID");
            if (remoteId != null)
            {                
                req.Headers.TryGetValue("X-Mango-Remote-ID", out var remoteIdHeader);
                if (remoteIdHeader.Count == 0)
                {
                    log.LogInformation("Missing header \"X-Mango-Remote-ID\"");
                    return new BadRequestResult();
                }
                else if (remoteIdHeader[0] != remoteId)
                {
                    log.LogInformation($"Invalid header \"X-Mango-Remote-ID\": {remoteIdHeader[0]}");
                    return new BadRequestResult();
                }
            }
            
            // Parse JSON request body
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            MangoEvent mangoEvent = JsonConvert.DeserializeObject<MangoEvent>(requestBody);

            log.LogInformation(mangoEvent.ToString());

            MangoEventDTO dto = new(mangoEvent);

            // write event to database table
            await output.AddAsync(dto);

            return new OkResult();
        }
    }

    public class MangoEvent
    {  
        public Guid Id { get; set; }
        [JsonProperty("event_originated_at")]
        public long CreatedAt { get; set; }
        [JsonProperty("webhook_sent_at")]
        public long SentAt { get; set; }
        public string Type { get; set; }
        public JObject Payload { get; set; }

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
}
