using System;
using System.Net.Http;
using System.Threading.Tasks;
using BlazorApp1.Client.Models;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Authorization;

namespace BlazorApp1.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [AllowAnonymous]
    public class WeatherController : ControllerBase
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly ILogger<WeatherController> _logger;
        private const string MANILA_LAT = "14.5995";
        private const string MANILA_LON = "120.9842";

        public WeatherController(IConfiguration configuration, IHttpClientFactory httpClientFactory, ILogger<WeatherController> logger)
        {
            _httpClient = httpClientFactory.CreateClient();
            _apiKey = configuration["OpenWeatherMap:ApiKey"];
            _logger = logger;

            if (string.IsNullOrEmpty(_apiKey))
            {
                _logger.LogError("OpenWeatherMap API key is not configured");
            }
        }

        [HttpGet("GetManilaCurrent")]
        public async Task<ActionResult<WeatherForecast>> GetManilaCurrent()
        {
            try
            {
                // Validate API key
                if (string.IsNullOrEmpty(_apiKey))
                {
                    _logger.LogError("OpenWeatherMap API key is not configured");
                    return StatusCode(500, "Weather API key is not configured. Please check your configuration.");
                }

                _logger.LogInformation($"Starting weather data fetch for Manila using API key: {_apiKey[..6]}...");
                var forecast = new WeatherForecast();

                // Get current weather
                var currentUrl = $"https://api.openweathermap.org/data/2.5/weather?lat={MANILA_LAT}&lon={MANILA_LON}&appid={_apiKey}&units=metric";
                _logger.LogInformation($"Calling current weather API: {currentUrl.Replace(_apiKey, "[HIDDEN]")}");
                
                var currentResponse = await _httpClient.GetAsync(currentUrl);
                if (!currentResponse.IsSuccessStatusCode)
                {
                    var errorContent = await currentResponse.Content.ReadAsStringAsync();
                    _logger.LogError($"OpenWeatherMap API error: Status {currentResponse.StatusCode}, Content: {errorContent}");
                    return StatusCode((int)currentResponse.StatusCode, $"Weather API error: {errorContent}");
                }

                var currentData = JsonSerializer.Deserialize<JsonElement>(
                    await currentResponse.Content.ReadAsStringAsync());

                forecast.Current = new WeatherData
                {
                    Temperature = currentData.GetProperty("main").GetProperty("temp").GetDouble(),
                    FeelsLike = currentData.GetProperty("main").GetProperty("feels_like").GetDouble(),
                    Humidity = currentData.GetProperty("main").GetProperty("humidity").GetInt32(),
                    Description = currentData.GetProperty("weather")[0].GetProperty("description").GetString(),
                    Icon = currentData.GetProperty("weather")[0].GetProperty("icon").GetString(),
                    DateTime = DateTimeOffset.FromUnixTimeSeconds(currentData.GetProperty("dt").GetInt64()).DateTime,
                    WindSpeed = currentData.GetProperty("wind").GetProperty("speed").GetDouble() * 3.6
                };

                // Get 5-day forecast
                var forecastUrl = $"https://api.openweathermap.org/data/2.5/forecast?lat={MANILA_LAT}&lon={MANILA_LON}&appid={_apiKey}&units=metric";
                _logger.LogInformation($"Calling forecast API: {forecastUrl.Replace(_apiKey, "[HIDDEN]")}");
                
                var forecastResponse = await _httpClient.GetAsync(forecastUrl);
                if (!forecastResponse.IsSuccessStatusCode)
                {
                    var errorContent = await forecastResponse.Content.ReadAsStringAsync();
                    _logger.LogError($"OpenWeatherMap forecast API error: Status {forecastResponse.StatusCode}, Content: {errorContent}");
                    return StatusCode((int)forecastResponse.StatusCode, $"Weather forecast API error: {errorContent}");
                }

                var forecastData = JsonSerializer.Deserialize<JsonElement>(
                    await forecastResponse.Content.ReadAsStringAsync());

                var forecastList = forecastData.GetProperty("list").EnumerateArray();
                var processedDates = new HashSet<DateTime>();
                forecast.FiveDayForecast = new List<WeatherData>();

                foreach (var item in forecastList)
                {
                    var forecastTime = DateTimeOffset.FromUnixTimeSeconds(item.GetProperty("dt").GetInt64()).DateTime;
                    var forecastDate = forecastTime.Date;

                    if (forecastDate <= DateTime.Today || processedDates.Contains(forecastDate))
                        continue;

                    if (forecastTime.Hour is >= 11 and <= 13)
                    {
                        processedDates.Add(forecastDate);
                        forecast.FiveDayForecast.Add(new WeatherData
                        {
                            Temperature = item.GetProperty("main").GetProperty("temp").GetDouble(),
                            FeelsLike = item.GetProperty("main").GetProperty("feels_like").GetDouble(),
                            Humidity = item.GetProperty("main").GetProperty("humidity").GetInt32(),
                            Description = item.GetProperty("weather")[0].GetProperty("description").GetString(),
                            Icon = item.GetProperty("weather")[0].GetProperty("icon").GetString(),
                            DateTime = forecastTime,
                            WindSpeed = item.GetProperty("wind").GetProperty("speed").GetDouble() * 3.6
                        });

                        if (forecast.FiveDayForecast.Count >= 5)
                            break;
                    }
                }

                _logger.LogInformation("Successfully processed weather data");
                return forecast;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Error calling OpenWeatherMap API");
                return StatusCode(500, $"Error calling weather API: {ex.Message}");
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Error parsing weather data");
                return StatusCode(500, $"Error parsing weather data: {ex.Message}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error fetching weather data");
                return StatusCode(500, $"Unexpected error: {ex.Message}");
            }
        }
    }
} 