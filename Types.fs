module WeatherTraveler.Types

open System

/// Geographic coordinate
type Coordinate = {
    Latitude: float
    Longitude: float
}

/// Temperature range specification
type TemperatureRange = {
    MinCelsius: float
    MaxCelsius: float
}

/// Weather condition types
type WeatherCondition =
    | Sunny
    | Cloudy
    | PartlyCloudy
    | Rainy
    | Stormy
    | Snowy
    | Foggy

/// Current weather information
type WeatherInfo = {
    Temperature: float
    Condition: WeatherCondition
    Humidity: int
    WindSpeed: float
    Timestamp: DateTime
    Location: Coordinate
}

/// Historical weather data point
type HistoricalWeather = {
    Date: DateTime
    AverageTemp: float
    MinTemp: float
    MaxTemp: float
    Condition: WeatherCondition
    Location: Coordinate
}

/// Location with travel preferences
type TravelLocation = {
    Name: string
    Coordinate: Coordinate
    PreferredArrivalDate: DateTime option
    PreferredDepartureDate: DateTime option
    IsRequired: bool
}

/// Route segment between two locations
type RouteSegment = {
    From: TravelLocation
    To: TravelLocation
    Distance: float // kilometers
    EstimatedDrivingTime: TimeSpan
    WeatherForecast: WeatherInfo list
}

/// Complete travel route
type TravelRoute = {
    Segments: RouteSegment list
    TotalDistance: float
    TotalTime: TimeSpan
    EstimatedFuelCost: float
    WeatherCompliance: bool // meets temperature requirements
}

/// Gas price information
type GasPriceInfo = {
    Location: Coordinate
    PricePerLiter: float
    StationName: string
    LastUpdated: DateTime
}

/// Travel planning request
type TravelPlanRequest = {
    StartLocation: TravelLocation
    DestinationLocations: TravelLocation list
    TemperatureRange: TemperatureRange
    StartDate: DateTime
    AvoidRain: bool
    MaxDrivingHoursPerDay: int
}

/// Planning result
type PlanningResult = {
    RecommendedRoute: TravelRoute
    AlternativeRoutes: TravelRoute list
    WeatherWarnings: string list
    EstimatedCosts: {| Fuel: float; Accommodation: float option |}
}
