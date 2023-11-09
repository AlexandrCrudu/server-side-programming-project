using Azure.Storage.Queues; // Namespace for Queue storage types
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System;
using System.Net;
using System.Threading.Tasks;
using Azure;
using Azure.Storage.Queues.Models;
using Microsoft.Azure.Functions.Worker.Http;
using System.Text.Json;
using System.Text;

namespace SSP_assignment.Functions
{
    public class HttpTriggerQueueFunction
    {
        private readonly ILogger _logger;
        public HttpTriggerQueueFunction(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<HttpTriggerQueueFunction>();
            _logger.LogInformation("iaibal");
        }

        [Function("EnqueueWeatherJob")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequestData req,
            FunctionContext executionContext)
        {
            //var logger = executionContext.GetLogger("EnqueueWeatherJob");
            _logger.LogInformation("C# HTTP trigger function processed a request.");


            // Use the connection string "UseDevelopmentStorage=true" to use Azurite
            var queueClient = new QueueClient("DefaultEndpointsProtocol=https;AccountName=imagestoragessp;AccountKey=DUSq7JOwFuaYdzNjncs0fxmQpWSGD3PcmZ4YiqpkepkFZAcHGVsVXqnh2EnOIC1uGA0md0FU69y6+AStq/n2aA==;EndpointSuffix=core.windows.net", "weather-jobs-queue");
            await queueClient.CreateIfNotExistsAsync();

            // Simulate fetching weather station data or other necessary details
            var jobDetails = new
            {
                JobId = Guid.NewGuid().ToString(),
            };

            // Serialize to JSON string

            var messageBody = JsonSerializer.Serialize(jobDetails);
            var messageBytes = Encoding.UTF8.GetBytes(messageBody);
            var messageBase64 = Convert.ToBase64String(messageBytes);

            // Send the message to the queue
            await queueClient.SendMessageAsync(messageBase64);

            // Create response
            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteStringAsync($"Enqueued weather job: {jobDetails.JobId}");

            _logger.LogInformation($"Enqueued weather job: {jobDetails.JobId}");
            _logger.LogInformation($"Message sent to queue: {messageBody}");
            return response;
        }
    }
}
