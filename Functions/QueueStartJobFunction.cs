using System;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Azure.Storage.Queues;
using System.Threading.Tasks;
using SSP_assignment.Services; // Assuming this is where IImageFetcherService is located
using SSP_assignment.Interfaces; // Assuming this is where interfaces are located
using SSP_assignment.Models;
using System.Text;
using Azure.Data.Tables;
using Microsoft.ML.Transforms;
using System.Linq;
using System.Net.Http.Headers;

namespace SSP_assignment.Functions
{
    public class QueueStartJobFunction
    {
        private readonly ILogger _logger;
        private readonly IImageFetcherService _imageFetcherService;
        private readonly HttpClient _httpClient;

        public QueueStartJobFunction(ILogger<QueueStartJobFunction> logger, IImageFetcherService imageFetcherService, HttpClient httpClient)
        {
            _logger = logger;
            _imageFetcherService = imageFetcherService;
            _httpClient = httpClient;
        }

        [Function(nameof(QueueStartJobFunction))]
        public async Task Run([QueueTrigger("weather-jobs-queue", Connection = "AzureWebJobsStorage")] string myQueueItem)
        {
            var jobDetailsFromQueue = JsonConvert.DeserializeObject<JobDetails>(myQueueItem);
            var jobId = jobDetailsFromQueue.JobId ?? throw new InvalidOperationException("JobId is missing in the queue message.");
            var weatherData = await GetAllWeatherDataAsync();
            var queueClient = new QueueClient("DefaultEndpointsProtocol=https;AccountName=imagestoragessp;AccountKey=DUSq7JOwFuaYdzNjncs0fxmQpWSGD3PcmZ4YiqpkepkFZAcHGVsVXqnh2EnOIC1uGA0md0FU69y6+AStq/n2aA==;EndpointSuffix=core.windows.net", "image-queue");
            await queueClient.CreateIfNotExistsAsync();

            
            var tableClient = new TableClient("DefaultEndpointsProtocol=https;AccountName=imagestoragessp;AccountKey=DUSq7JOwFuaYdzNjncs0fxmQpWSGD3PcmZ4YiqpkepkFZAcHGVsVXqnh2EnOIC1uGA0md0FU69y6+AStq/n2aA==;EndpointSuffix=core.windows.net", "imageStatuses");
            await tableClient.CreateIfNotExistsAsync();
            JobDetails jobDetails = null;
            foreach (var singleWeatherData in weatherData)
            {
                var imageId = Guid.NewGuid().ToString();
                try
                {
                    jobDetails = new JobDetails
                    {
                        ImageId = imageId,
                        ImageUrl = await _imageFetcherService.FetchRandomImageAsync(),
                        WeatherData = singleWeatherData, // Assuming JobDetails has a property for weather data
                        JobId = jobId
                    };

                    var tableEntity = new ImageJobEntity
                    {
                        PartitionKey = jobId,
                        RowKey = imageId,
                        Status = "Pending"
                    };

                    await tableClient.AddEntityAsync(tableEntity);

                    _logger.LogInformation($"Added to Azure Table image with imageID: {jobDetails.ImageId}");

                    _logger.LogInformation($"Processing with imageID: {jobDetails.ImageId}");

                    var jobDetailsMessage = JsonConvert.SerializeObject(jobDetails);
                    var messageBytes = Encoding.UTF8.GetBytes(jobDetailsMessage);
                    var messageBase64 = Convert.ToBase64String(messageBytes);

                    await queueClient.SendMessageAsync(messageBase64);

                    _logger.LogInformation($"Enqueued job details for Image ID: {jobDetails.ImageId}");
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error processing weather data for Image ID:: {ex.Message}");
                    // Handle exception, potentially by enqueuing a poison message or some other error handling mechanism
                }
            }
        }


        private async Task<List<string>> GetAllWeatherDataAsync()
        {
            string weatherApiUrl = "https://data.buienradar.nl/2.0/feed/json";
            var allWeatherData = new List<string>();
            string accessKey = "PyeUj3j-zYHjvWMWZCZz7IEgOaaOHcaMUaP0ig3yHCc";

            try
            {
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Client-ID", accessKey);
                var response = await _httpClient.GetAsync(weatherApiUrl);
                response.EnsureSuccessStatusCode();
                var content = await response.Content.ReadAsStringAsync();

                dynamic json = JsonConvert.DeserializeObject(content);
                if (json == null || json.actual.stationmeasurements == null)
                    return allWeatherData;

                _logger.LogInformation($"Nr of weather station measurements retrieved from API: {json.actual.stationmeasurements.Count}");

                foreach (var station in json.actual.stationmeasurements)
                {
                    string weatherDescription = station.weatherdescription;
                    var temperature = station.temperature;
                    string windDirection = station.winddirection;
                    var windSpeed = station.windspeed;

                    string weatherData = $"Weather: {weatherDescription}\n" +
                                         $"Temperature: {temperature}°C\n" +
                                         $"Wind: {windDirection} at {windSpeed} m/s";
                    allWeatherData.Add(weatherData);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error fetching weather data: {ex.Message}");
                // You might want to handle this differently, perhaps returning an empty list is not the best approach
            }

            return allWeatherData;
        }
    }
}