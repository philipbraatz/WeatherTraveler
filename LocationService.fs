module WeatherTraveler.LocationService

open System
open System.Net.Http
open FSharp.Data
open Newtonsoft.Json
open WeatherTraveler.Types

let private httpClient = new HttpClient()/// Major US cities with coordinates for quick reference
let usMajorCities = [
    ("New York, NY", { Latitude = 40.7128; Longitude = -74.0060 })
    ("Los Angeles, CA", { Latitude = 34.0522; Longitude = -118.2437 })
    ("Chicago, IL", { Latitude = 41.8781; Longitude = -87.6298 })
    ("Houston, TX", { Latitude = 29.7604; Longitude = -95.3698 })
    ("Phoenix, AZ", { Latitude = 33.4484; Longitude = -112.0740 })
    ("Philadelphia, PA", { Latitude = 39.9526; Longitude = -75.1652 })
    ("San Antonio, TX", { Latitude = 29.4241; Longitude = -98.4936 })
    ("San Diego, CA", { Latitude = 32.7157; Longitude = -117.1611 })
    ("Dallas, TX", { Latitude = 32.7767; Longitude = -96.7970 })
    ("San Jose, CA", { Latitude = 37.3382; Longitude = -121.8863 })
    ("Austin, TX", { Latitude = 30.2672; Longitude = -97.7431 })
    ("Jacksonville, FL", { Latitude = 30.3322; Longitude = -81.6557 })
    ("Fort Worth, TX", { Latitude = 32.7555; Longitude = -97.3308 })
    ("Columbus, OH", { Latitude = 39.9612; Longitude = -82.9988 })
    ("Charlotte, NC", { Latitude = 35.2271; Longitude = -80.8431 })
    ("San Francisco, CA", { Latitude = 37.7749; Longitude = -122.4194 })
    ("Indianapolis, IN", { Latitude = 39.7684; Longitude = -86.1581 })
    ("Seattle, WA", { Latitude = 47.6062; Longitude = -122.3321 })
    ("Denver, CO", { Latitude = 39.7392; Longitude = -104.9903 })
    ("Boston, MA", { Latitude = 42.3601; Longitude = -71.0589 })
]

/// Calculate distance between two coordinates using Haversine formula
let calculateDistance (coord1: Coordinate) (coord2: Coordinate) =
    let earthRadius = 6371.0 // Earth's radius in kilometers
    let lat1Rad = coord1.Latitude * Math.PI / 180.0
    let lat2Rad = coord2.Latitude * Math.PI / 180.0
    let deltaLatRad = (coord2.Latitude - coord1.Latitude) * Math.PI / 180.0
    let deltaLonRad = (coord2.Longitude - coord1.Longitude) * Math.PI / 180.0
    
    let a = Math.Sin(deltaLatRad / 2.0) * Math.Sin(deltaLatRad / 2.0) +
            Math.Cos(lat1Rad) * Math.Cos(lat2Rad) *
            Math.Sin(deltaLonRad / 2.0) * Math.Sin(deltaLonRad / 2.0)
    let c = 2.0 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1.0 - a))
    
    earthRadius * c/// Get current location (mock implementation)
let getCurrentLocation () = async {
    // In a real implementation, you would use geolocation services
    // For now, return a default location (e.g., Denver, CO)
    return {
        Latitude = 39.7392
        Longitude = -104.9903
    }
}

/// Geocode a location name to coordinates (mock implementation)
let geocodeLocation (locationName: string) = async {
    try
        // Check if it's one of our known major cities first
        let knownCity = 
            usMajorCities
            |> List.tryFind (fun (name, _) -> name.ToLower().Contains(locationName.ToLower()))
        
        match knownCity with
        | Some (_, coordinate) -> return Some coordinate
        | None ->
            // For unknown locations, return a mock coordinate
            printfn "Using mock coordinates for '%s'" locationName
            return Some { Latitude = 39.0; Longitude = -98.0 } // Geographic center of US
    with
    | ex -> 
        printfn "Error geocoding location '%s': %s" locationName ex.Message
        return None
}

/// Get route information between two points
let getRouteInfo (from: Coordinate) (destination: Coordinate) = async {
    // Mock implementation - in reality you'd use Google Maps API, OSRM, etc.
    let distance = calculateDistance from destination
    let averageSpeed = 80.0 // km/h
    let drivingTime = TimeSpan.FromHours(distance / averageSpeed)
    
    return {
        From = { Name = "Start"; Coordinate = from; PreferredArrivalDate = None; PreferredDepartureDate = None; IsRequired = true }
        To = { Name = "End"; Coordinate = destination; PreferredArrivalDate = None; PreferredDepartureDate = None; IsRequired = true }
        Distance = distance
        EstimatedDrivingTime = drivingTime
        WeatherForecast = [] // Will be populated by weather service
    }
}

/// Find cities within a radius of a coordinate
let findNearbyMajorCities (center: Coordinate) (radiusKm: float) =
    usMajorCities
    |> List.filter (fun (_, coord) -> calculateDistance center coord <= radiusKm)
    |> List.map (fun (name, coord) -> name, coord, calculateDistance center coord)
    |> List.sortBy (fun (_, _, distance) -> distance)
