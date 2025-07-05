using System.Collections.Generic;
using Microsoft.FSharp.Core;
using Microsoft.FSharp.Collections;

namespace WeatherTraveler.Web.Services;

// Create C# wrapper types for F# types
public class WeatherInfo
{
    public double Temperature { get; set; }
    public WeatherCondition Condition { get; set; }
    public int Humidity { get; set; }
    public double WindSpeed { get; set; }
    public DateTime Timestamp { get; set; }
    public required Coordinate Location { get; set; }
}

public class Coordinate
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }

    public Coordinate() { }
    public Coordinate(double latitude, double longitude)
    {
        Latitude = latitude;
        Longitude = longitude;
    }
}

public class TemperatureRange
{
    public double MinTemperature { get; set; }
    public double MaxTemperature { get; set; }

    public TemperatureRange() { }
    public TemperatureRange(double min, double max)
    {
        MinTemperature = min;
        MaxTemperature = max;
    }
}

public class UserProfile
{
    public string UserId { get; set; } = string.Empty;
    public UserPreferences Preferences { get; set; } = new();
    public NotificationSettings NotificationSettings { get; set; } = new();
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}

public class UserPreferences
{
    public TemperatureRange TemperatureRange { get; set; } = new();
    public bool AvoidStorms { get; set; } = true;
    public FuelType FuelType { get; set; } = FuelType.Regular;
    public double MaxDailyDriving { get; set; } = 8.0; // hours
    public List<string> PreferredStopTypes { get; set; } = new();
}

public class NotificationSettings
{
    public bool WeatherAlerts { get; set; } = true;
    public bool RouteUpdates { get; set; } = true;
    public bool FuelPriceAlerts { get; set; } = false;
    public string EmailAddress { get; set; } = string.Empty;
}

public enum FuelType
{
    Regular,
    Premium,
    Diesel,
    Electric
}

public enum WeatherCondition
{
    Sunny,
    Cloudy,
    Rainy,
    Snowy,
    Stormy,
    Foggy
}

public class GasStationInfo
{
    public string StationName { get; set; } = string.Empty;
    public double Price { get; set; }
    public double Distance { get; set; }
    public Coordinate Location { get; set; } = new();
    public FuelType FuelType { get; set; } = FuelType.Regular;
}

public interface IWeatherTravelerService
{
    Task<WeatherInfo?> GetCurrentWeatherAsync(Coordinate coordinate);
    Task<List<WeatherInfo>> GetWeatherForecastAsync(Coordinate coordinate);
    Task<UserProfile> LoadUserProfileAsync();
    Task SaveUserProfileAsync(UserProfile profile);
    Task<List<WeatherInfo>> PlanRouteAsync(List<Coordinate> waypoints, TemperatureRange temperatureRange);
    Task<double> CalculateDistanceAsync(Coordinate from, Coordinate to);
    Task<List<Coordinate>> GetMajorCitiesAsync();
    Task<string> ExportRouteToKmlAsync(List<Coordinate> waypoints);
    Task<GasStationInfo[]> GetGasPricesAsync(Coordinate location, double radiusKm);
}

public class WeatherTravelerService : IWeatherTravelerService
{
    private static UserProfile? _cachedUserProfile;
    private readonly Random _random = new();

    public async Task<WeatherInfo?> GetCurrentWeatherAsync(Coordinate coordinate)
    {
        try
        {
            await Task.Delay(300);
            
            return new WeatherInfo
            {
                Temperature = 20 + _random.NextDouble() * 15,
                Condition = GetRandomCondition(),
                Humidity = _random.Next(30, 90),
                WindSpeed = _random.NextDouble() * 20,
                Timestamp = DateTime.UtcNow,
                Location = coordinate
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting current weather: {ex.Message}");
            return null;
        }
    }

    public async Task<List<WeatherInfo>> GetWeatherForecastAsync(Coordinate coordinate)
    {
        try
        {
            await Task.Delay(400);
            
            var forecast = new List<WeatherInfo>();
            var baseTemp = 18 + _random.NextDouble() * 12;
            
            for (int i = 0; i < 5; i++)
            {
                forecast.Add(new WeatherInfo
                {
                    Temperature = baseTemp + _random.NextDouble() * 8 - 4,
                    Condition = GetRandomCondition(),
                    Humidity = _random.Next(40, 85),
                    WindSpeed = _random.NextDouble() * 15,
                    Timestamp = DateTime.UtcNow.AddDays(i),
                    Location = coordinate
                });
            }
            
            return forecast;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting weather forecast: {ex.Message}");
            return new List<WeatherInfo>();
        }
    }

    public async Task<UserProfile> LoadUserProfileAsync()
    {
        await Task.Delay(50);
        
        if (_cachedUserProfile != null)
        {
            return _cachedUserProfile;
        }

        _cachedUserProfile = CreateDefaultUserProfile();
        return _cachedUserProfile;
    }

    public async Task SaveUserProfileAsync(UserProfile profile)
    {
        await Task.Delay(100);
        _cachedUserProfile = profile;
        _cachedUserProfile.LastUpdated = DateTime.UtcNow;
    }

    public async Task<List<WeatherInfo>> PlanRouteAsync(List<Coordinate> waypoints, TemperatureRange temperatureRange)
    {
        try
        {
            await Task.Delay(600);
            
            var routeWeather = new List<WeatherInfo>();
            
            foreach (var waypoint in waypoints)
            {
                var weather = await GetCurrentWeatherAsync(waypoint);
                if (weather != null && 
                    weather.Temperature >= temperatureRange.MinTemperature && 
                    weather.Temperature <= temperatureRange.MaxTemperature)
                {
                    routeWeather.Add(weather);
                }
            }
            
            return routeWeather;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error planning route: {ex.Message}");
            return new List<WeatherInfo>();
        }
    }

    public async Task<double> CalculateDistanceAsync(Coordinate from, Coordinate to)
    {
        await Task.Delay(50);
        
        // Haversine formula
        const double earthRadius = 6371.0; // Earth's radius in kilometers
        
        var lat1Rad = from.Latitude * Math.PI / 180.0;
        var lat2Rad = to.Latitude * Math.PI / 180.0;
        var deltaLatRad = (to.Latitude - from.Latitude) * Math.PI / 180.0;
        var deltaLonRad = (to.Longitude - from.Longitude) * Math.PI / 180.0;
        
        var a = Math.Sin(deltaLatRad / 2) * Math.Sin(deltaLatRad / 2) +
                Math.Cos(lat1Rad) * Math.Cos(lat2Rad) *
                Math.Sin(deltaLonRad / 2) * Math.Sin(deltaLonRad / 2);
        
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        
        return earthRadius * c;
    }

    public async Task<List<Coordinate>> GetMajorCitiesAsync()
    {
        await Task.Delay(100);
        
        return new List<Coordinate>
        {
            new(40.7128, -74.0060),  // New York
            new(34.0522, -118.2437), // Los Angeles
            new(41.8781, -87.6298),  // Chicago
            new(29.7604, -95.3698),  // Houston
            new(33.4484, -112.0740), // Phoenix
            new(39.9526, -75.1652),  // Philadelphia
            new(29.4241, -98.4936),  // San Antonio
            new(32.7767, -96.7970),  // Dallas
            new(37.7749, -122.4194), // San Francisco
            new(37.3382, -121.8863), // San Jose
            new(30.2672, -97.7431),  // Austin
            new(30.3322, -81.6557)   // Jacksonville
        };
    }

    public async Task<string> ExportRouteToKmlAsync(List<Coordinate> waypoints)
    {
        try
        {
            await Task.Delay(100);
            
            if (!waypoints.Any())
            {
                Console.WriteLine("ExportRouteToKmlAsync: No waypoints provided");
                return string.Empty;
            }
            
            var coordinateString = string.Join(" ", waypoints.Select(w => $"{w.Longitude:F6},{w.Latitude:F6},0"));
            Console.WriteLine($"ExportRouteToKmlAsync: Generated coordinate string: {coordinateString}");
            
            // Generate valid KML with proper <name> tags
            var kml = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n" +
                     "<kml xmlns=\"http://www.opengis.net/kml/2.2\">\n" +
                     "  <Document>\n" +
                     "    <name>Weather Traveler Route</name>\n" +
                     $"    <description>Travel route generated by Weather Traveler on {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC</description>\n" +
                     "    <Style id=\"routeStyle\">\n" +
                     "      <LineStyle>\n" +
                     "        <color>ff0000ff</color>\n" +
                     "        <width>3</width>\n" +
                     "      </LineStyle>\n" +
                     "    </Style>\n" +
                     "    <Style id=\"waypointStyle\">\n" +
                     "      <IconStyle>\n" +
                     "        <color>ff00ff00</color>\n" +
                     "        <scale>1.2</scale>\n" +
                     "      </IconStyle>\n" +
                     "    </Style>\n" +
                     "    <Placemark>\n" +
                     $"      <name>Travel Route ({waypoints.Count} waypoints)</name>\n" +
                     "      <description>Planned route with weather considerations</description>\n" +
                     "      <styleUrl>#routeStyle</styleUrl>\n" +
                     "      <LineString>\n" +
                     "        <tessellate>1</tessellate>\n" +
                     "        <coordinates>\n" +
                     $"          {coordinateString}\n" +
                     "        </coordinates>\n" +
                     "      </LineString>\n" +
                     "    </Placemark>\n";
            
            // Add individual waypoint placemarks with proper <name> tags
            for (int i = 0; i < waypoints.Count; i++)
            {
                var waypoint = waypoints[i];
                var waypointName = i == 0 ? "Start" : i == waypoints.Count - 1 ? "End" : $"Waypoint {i}";
                
                kml += "    <Placemark>\n" +
                       $"      <name>{waypointName}</name>\n" +
                       $"      <description>Coordinate: {waypoint.Latitude:F6}, {waypoint.Longitude:F6}</description>\n" +
                       "      <styleUrl>#waypointStyle</styleUrl>\n" +
                       "      <Point>\n" +
                       $"        <coordinates>{waypoint.Longitude:F6},{waypoint.Latitude:F6},0</coordinates>\n" +
                       "      </Point>\n" +
                       "    </Placemark>\n";
            }
            
            kml += "  </Document>\n" +
                   "</kml>";
            
            Console.WriteLine($"ExportRouteToKmlAsync: Generated valid KML length: {kml.Length}");
            Console.WriteLine($"ExportRouteToKmlAsync: KML content preview: {kml.Substring(0, Math.Min(200, kml.Length))}...");
            
            return kml;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error generating KML: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            return string.Empty;
        }
    }

    public async Task<GasStationInfo[]> GetGasPricesAsync(Coordinate location, double radiusKm)
    {
        try
        {
            await Task.Delay(200);
            
            var stations = new List<GasStationInfo>();
            var stationNames = new[] { "Shell", "BP", "Exxon", "Chevron", "Mobil", "Valero" };
            
            for (int i = 0; i < 6; i++)
            {
                var distance = _random.NextDouble() * radiusKm;
                var price = 3.50 + _random.NextDouble() * 0.50;
                
                stations.Add(new GasStationInfo
                {
                    StationName = stationNames[i % stationNames.Length],
                    Price = price,
                    Distance = distance,
                    Location = new Coordinate(
                        location.Latitude + (_random.NextDouble() - 0.5) * 0.1,
                        location.Longitude + (_random.NextDouble() - 0.5) * 0.1
                    ),
                    FuelType = (FuelType)_random.Next(0, 4)
                });
            }
            
            return stations.OrderBy(s => s.Distance).ToArray();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting gas prices: {ex.Message}");
            return Array.Empty<GasStationInfo>();
        }
    }

    private WeatherCondition GetRandomCondition()
    {
        var conditions = Enum.GetValues<WeatherCondition>();
        return conditions[_random.Next(conditions.Length)];
    }

    private static UserProfile CreateDefaultUserProfile()
    {
        return new UserProfile
        {
            UserId = Guid.NewGuid().ToString(),
            Preferences = new UserPreferences
            {
                TemperatureRange = new TemperatureRange(15, 25),
                AvoidStorms = true,
                FuelType = FuelType.Regular,
                MaxDailyDriving = 8.0,
                PreferredStopTypes = new List<string> { "Restaurant", "Gas Station", "Rest Area" }
            },
            NotificationSettings = new NotificationSettings
            {
                WeatherAlerts = true,
                RouteUpdates = true,
                FuelPriceAlerts = false,
                EmailAddress = string.Empty
            },
            LastUpdated = DateTime.UtcNow
        };
    }
}

// Extension methods for F# interop (when we add it later)
public static class FSharpInteropExtensions
{
    // These will be added later when we integrate with the F# core library
}
