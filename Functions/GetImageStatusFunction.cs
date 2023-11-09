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
        private readonly string _blobStorageConnectionString = "DefaultEndpointsProtocol=https;AccountName=imagestoragessp;AccountKey=DUSq7JOwFuaYdzNjncs0fxmQpWSGD3PcmZ4YiqpkepkFZAcHGVsVXqnh2EnOIC1uGA0md0FU69y6+AStq/n2aA==;EndpointSuffix=core.windows.net"; // TODO: replace with your actual connection string
        private readonly string _tableStorageConnectionString = "DefaultEndpointsProtocol=https;AccountName=imagestoragessp;AccountKey=DUSq7JOwFuaYdzNjncs0fxmQpWSGD3PcmZ4YiqpkepkFZAcHGVsVXqnh2EnOIC1uGA0md0FU69y6+AStq/n2aA==;EndpointSuffix=core.windows.net";

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

            var containerClient = new BlobContainerClient(_blobStorageConnectionString, "processed-images");
            var tableClient = new TableClient(_tableStorageConnectionString, "imageStatuses");

            try
            {

                var blobPages = containerClient.GetBlobsAsync();

                var entityPages = tableClient.QueryAsync<TableEntity>(filter: $"PartitionKey eq '{jobId}'");


                var results = new List<ImageResult>(); 
                await foreach (var blobItem in blobPages)
                {
                    string rowKey = blobItem.Name.TrimStart('$').Split('.')[0];
                    var blobClient = containerClient.GetBlobClient(blobItem.Name);
                    var blobUrl = blobClient.Uri.ToString();

                    var matchingEntity = await tableClient.GetEntityAsync<TableEntity>(jobId, rowKey);
                    if (matchingEntity != null)
                    {
                        results.Add(new ImageResult
                        {
                            Url = blobUrl,
                            Status = matchingEntity.Value["Status"].ToString() // Ensure the property name matches your schema
                        });
                    }
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
