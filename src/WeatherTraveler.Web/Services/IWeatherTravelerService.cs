using System.Collections.Generic;
using System.Threading.Tasks;

namespace WeatherTraveler.Web.Services
{
    /// <summary>
    /// Interface for WeatherTraveler service providing access to weather and route planning functionality
    /// </summary>
    public interface IWeatherTravelerService
    {
        /// <summary>
        /// Get current weather for a location
        /// </summary>
        Task<WeatherInfo> GetCurrentWeatherAsync(double latitude, double longitude);
        
        /// <summary>
        /// Plan a route with weather considerations
        /// </summary>
        Task<RouteResult> PlanRouteAsync(RouteRequest request);
        
        /// <summary>
        /// Get user preferences
        /// </summary>
        Task<UserPreferences> GetUserPreferencesAsync();
        
        /// <summary>
        /// Update user preferences
        /// </summary>
        Task UpdateUserPreferencesAsync(UserPreferences preferences);
    }

    /// <summary>
    /// Weather information data transfer object
    /// </summary>
    public class WeatherInfo
    {
        public double Temperature { get; set; }
        public string Condition { get; set; } = string.Empty;
        public double Humidity { get; set; }
        public string Location { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
    }

    /// <summary>
    /// Route planning request
    /// </summary>
    public class RouteRequest
    {
        public Location Origin { get; set; } = new();
        public Location Destination { get; set; } = new();
        public List<Location> Waypoints { get; set; } = new();
        public DateTime DepartureTime { get; set; }
        public TemperatureRange TemperatureRange { get; set; } = new();
    }

    /// <summary>
    /// Route planning result
    /// </summary>
    public class RouteResult
    {
        public List<RouteSegment> Segments { get; set; } = new();
        public double TotalDistance { get; set; }
        public TimeSpan EstimatedDuration { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    /// <summary>
    /// Location information
    /// </summary>
    public class Location
    {
        public string Name { get; set; } = string.Empty;
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }

    /// <summary>
    /// Route segment
    /// </summary>
    public class RouteSegment
    {
        public Location From { get; set; } = new();
        public Location To { get; set; } = new();
        public double Distance { get; set; }
        public TimeSpan Duration { get; set; }
        public WeatherInfo Weather { get; set; } = new();
    }

    /// <summary>
    /// Temperature range
    /// </summary>
    public class TemperatureRange
    {
        public double MinTemperature { get; set; }
        public double MaxTemperature { get; set; }
    }

    /// <summary>
    /// User preferences
    /// </summary>
    public class UserPreferences
    {
        public string Name { get; set; } = string.Empty;
        public TemperatureRange PreferredTemperatureRange { get; set; } = new();
        public bool AvoidRain { get; set; }
        public double MaxDailyDrivingHours { get; set; } = 8.0;
        public string PreferredFuelType { get; set; } = "Regular";
    }

    /// <summary>
    /// Implementation of WeatherTraveler service that wraps F# Core functionality
    /// </summary>
    public class WeatherTravelerService : IWeatherTravelerService
    {
        /// <summary>
        /// Get current weather for a location
        /// </summary>
        public async Task<WeatherInfo> GetCurrentWeatherAsync(double latitude, double longitude)
        {
            // For now, return mock data while we build the bridge to F# Core
            return await Task.FromResult(new WeatherInfo
            {
                Temperature = 22.5,
                Condition = "Partly Cloudy",
                Humidity = 65.0,
                Location = $"Location at {latitude:F2}, {longitude:F2}",
                Timestamp = DateTime.UtcNow
            });
        }

        /// <summary>
        /// Plan a route with weather considerations
        /// </summary>
        public async Task<RouteResult> PlanRouteAsync(RouteRequest request)
        {
            // For now, return mock data while we build the bridge to F# Core
            return await Task.FromResult(new RouteResult
            {
                Segments = new List<RouteSegment>
                {
                    new RouteSegment
                    {
                        From = request.Origin,
                        To = request.Destination,
                        Distance = 350.5,
                        Duration = TimeSpan.FromHours(4.5),
                        Weather = new WeatherInfo
                        {
                            Temperature = 25.0,
                            Condition = "Sunny",
                            Humidity = 50.0,
                            Location = "Route midpoint",
                            Timestamp = DateTime.UtcNow
                        }
                    }
                },
                TotalDistance = 350.5,
                EstimatedDuration = TimeSpan.FromHours(4.5),
                Status = "Success"
            });
        }

        /// <summary>
        /// Get user preferences
        /// </summary>
        public async Task<UserPreferences> GetUserPreferencesAsync()
        {
            // For now, return default preferences
            return await Task.FromResult(new UserPreferences
            {
                Name = "Weather Traveler User",
                PreferredTemperatureRange = new TemperatureRange
                {
                    MinTemperature = 15.0,
                    MaxTemperature = 25.0
                },
                AvoidRain = true,
                MaxDailyDrivingHours = 8.0,
                PreferredFuelType = "Regular"
            });
        }

        /// <summary>
        /// Update user preferences
        /// </summary>
        public async Task UpdateUserPreferencesAsync(UserPreferences preferences)
        {
            // For now, just simulate async operation
            await Task.Delay(100);
            // TODO: Implement actual preferences storage
        }
    }
}
