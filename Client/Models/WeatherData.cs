using System;
using System.Collections.Generic;

namespace BlazorApp1.Client.Models
{
    public class WeatherData
    {
        public double Temperature { get; set; }
        public double FeelsLike { get; set; }
        public int Humidity { get; set; }
        public string Description { get; set; }
        public string Icon { get; set; }
        public DateTime DateTime { get; set; }
        public double WindSpeed { get; set; }
        public string DayOfWeek => DateTime.DayOfWeek.ToString();
    }

    public class WeatherForecast
    {
        public WeatherData Current { get; set; }
        public List<WeatherData> FiveDayForecast { get; set; } = new List<WeatherData>();
    }
} 