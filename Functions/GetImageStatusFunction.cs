using System;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Azure.Storage.Blobs;
using Azure.Data.Tables;
using Azure;
using System.Collections.Generic;
using Microsoft.Azure.Functions.Worker.Http;
using System.Net;
using Newtonsoft.Json;
using SSP_assignment.Models;
using Azure.Storage.Blobs.Models;

namespace SSP_assignment.Functions
{
    public class HttpGetImageStatusFunction
    {
        private readonly ILogger _logger;

        public HttpGetImageStatusFunction(ILogger<HttpGetImageStatusFunction> logger)
        {
            _logger = logger;
        }

        [Function(nameof(HttpGetImageStatusFunction))]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequestData req)
        {
            string jobId = req.Query["jobId"];
            if (string.IsNullOrEmpty(jobId))
            {
                _logger.LogError("JobId is missing in the query parameters.");
                return req.CreateResponse(HttpStatusCode.BadRequest);
            }

            var containerClient = new BlobContainerClient(Environment.GetEnvironmentVariable("StorageAccountConnectionString"), Environment.GetEnvironmentVariable("blob-container-name"));
            var tableClient = new TableClient(Environment.GetEnvironmentVariable("StorageAccountConnectionString"), Environment.GetEnvironmentVariable("table-name"));

            try
            {
                var results = new List<ImageResult>();
                var blobNames = new HashSet<string>();
           

                await foreach (var blobItem in containerClient.GetBlobsAsync())
                {
                    blobNames.Add(blobItem.Name.TrimStart('$').Split('.')[0]);
                }

           
                await foreach (var entity in tableClient.QueryAsync<TableEntity>(filter: $"PartitionKey eq '{jobId}'"))
                {
                    string url = blobNames.Contains(entity.RowKey) ? containerClient.GetBlobClient($"${entity.RowKey}.png").Uri.ToString() : "";

                    results.Add(new ImageResult
                    {
                        Url = url,
                        Status = entity["Status"].ToString()
                    });
                }

                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteAsJsonAsync(results);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error fetching image statuses: {ex.Message}");
                return req.CreateResponse(HttpStatusCode.InternalServerError);
            }
        }
    }
}
