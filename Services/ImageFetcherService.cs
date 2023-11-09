using Newtonsoft.Json;
using SSP_assignment.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace SSP_assignment.Services
{
    public class ImageFetcherService : IImageFetcherService
    {
        private readonly HttpClient _httpClient;

        public ImageFetcherService(HttpClient httpClient)
        {
            _httpClient = httpClient;
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Client-ID", "AUry3p_-uhcd9UX7WqJ51M5EudOzzk09jJrR_NzchNA");
        }

        public async Task<string> FetchRandomImageAsync()
        {
            var response = await _httpClient.GetAsync("https://api.unsplash.com/photos/random");
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                dynamic json = JsonConvert.DeserializeObject(content);
                string imageUrl = json.urls.full;  // Get the full-size image URL.
                return imageUrl;
            }

            throw new HttpRequestException($"Unsplash API error: {response.ReasonPhrase}");
        }
    }
  }

