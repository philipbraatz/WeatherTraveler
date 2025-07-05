module WeatherTraveler.WeatherService

open System
open System.Net.Http
open FSharp.Data
open Newtonsoft.Json
open WeatherTraveler.Types
open WeatherTraveler.Configuration
open WeatherTraveler.CacheService

let private httpClient = new HttpClient()

// OpenWeatherMap API types
type OpenWeatherResponse = JsonProvider<"""
{
  "coord": {"lon": -122.08, "lat": 37.39},
  "weather": [{"id": 800, "main": "Clear", "description": "clear sky", "icon": "01d"}],
  "base": "stations",
  "main": {"temp": 282.55, "feels_like": 281.86, "temp_min": 280.37, "temp_max": 284.26, "pressure": 1023, "humidity": 100},
  "visibility": 16093,
  "wind": {"speed": 1.5, "deg": 350},
  "clouds": {"all": 1},
  "dt": 1560350645,
  "sys": {"type": 1, "id": 5122, "country": "US", "sunrise": 1560343627, "sunset": 1560396563},
  "timezone": -25200,
  "id": 420006353,
  "name": "Mountain View",
  "cod": 200
}
""">

/// Convert Kelvin to Celsius
let kelvinToCelsius (kelvin: float) = kelvin - 273.15

/// Parse weather condition from API response
let parseWeatherCondition (condition: string) =
    match condition.ToLower() with
    | "clear" -> Sunny
    | "clouds" when condition.Contains("few") || condition.Contains("scattered") -> PartlyCloudy
    | "clouds" -> Cloudy
    | "rain" | "drizzle" -> Rainy
    | "thunderstorm" -> Stormy
    | "snow" -> Snowy
    | "mist" | "fog" -> Foggy
    | _ -> Cloudy

/// Get current weather from OpenWeatherMap API
let private fetchRealWeatherFromApi (coordinate: Coordinate) (apiKey: string) = async {
    try
        let url = sprintf "https://api.openweathermap.org/data/2.5/weather?lat=%f&lon=%f&appid=%s&units=metric" 
                    coordinate.Latitude coordinate.Longitude apiKey
        
        let! response = httpClient.GetStringAsync(url) |> Async.AwaitTask
        let weather = OpenWeatherResponse.Parse(response)
        
        return Some {
            Temperature = float weather.Main.Temp
            Condition = parseWeatherCondition weather.Weather.[0].Main
            Humidity = weather.Main.Humidity
            WindSpeed = float weather.Wind.Speed
            Timestamp = DateTime.UtcNow
            Location = coordinate
        }
    with
    | ex -> 
        printfn "Error fetching real weather data: %s" ex.Message
        return None
}

/// Generate realistic mock weather based on location and season
let private generateMockWeather (coordinate: Coordinate) =
    let random = Random()
    
    // Base temperature varies by latitude (rough approximation)
    let latitudeFactor = Math.Abs(coordinate.Latitude) / 90.0
    let baseTemp = 25.0 - (latitudeFactor * 20.0) // Warmer near equator
    
    // Seasonal variation (simplified)
    let dayOfYear = DateTime.Now.DayOfYear
    let seasonalFactor = Math.Sin((float dayOfYear / 365.0) * 2.0 * Math.PI)
    let seasonalTemp = baseTemp + (seasonalFactor * 10.0)
    
    // Add some randomness
    let finalTemp = seasonalTemp + (random.NextDouble() * 10.0 - 5.0)
    
    // Weather conditions based on region (simplified)
    let isWetRegion = coordinate.Longitude > -100.0 && coordinate.Latitude > 35.0 // Northeast US
    let rainChance = if isWetRegion then 0.3 else 0.15
    
    let condition = 
        if random.NextDouble() < rainChance then
            if random.NextDouble() < 0.2 then Stormy else Rainy
        else
            match random.Next(3) with
            | 0 -> Sunny
            | 1 -> Cloudy
            | _ -> PartlyCloudy
    
    {
        Temperature = finalTemp
        Condition = condition
        Humidity = 40 + random.Next(40)
        WindSpeed = random.NextDouble() * 15.0
        Timestamp = DateTime.UtcNow
        Location = coordinate
    }

/// Get current weather for a location with configuration and caching
let getCurrentWeather (config: AppConfig) (coordinate: Coordinate) = async {
    // Try cache first
    match getCachedWeatherData config coordinate "current" with
    | Some [weather] -> return Some weather
    | _ ->
        // Fetch fresh data
        let weather = 
            match config.WeatherApi.UseRealApis, config.WeatherApi.OpenWeatherApiKey with
            | true, Some apiKey -> 
                async {
                    let! result = fetchRealWeatherFromApi coordinate apiKey
                    match result with
                    | Some weather -> return weather
                    | None -> 
                        printfn "⚠️  Falling back to mock data due to API error"
                        return generateMockWeather coordinate
                }
            | _ -> 
                async { return generateMockWeather coordinate }
        
        let! weatherData = weather
        
        // Cache the result
        do! cacheWeatherData config coordinate "current" [weatherData]
        
        return Some weatherData
}

/// Get 5-day weather forecast with enhanced mock implementation
let getWeatherForecast (config: AppConfig) (coordinate: Coordinate) = async {
    try
        // Try cache first
        match getCachedWeatherData config coordinate "5day" with
        | Some cachedData -> return cachedData
        | None ->
            // Enhanced mock implementation with realistic patterns
            let random = Random()
            let baseWeather = generateMockWeather coordinate
            
            let forecasts = 
                [0..39] // 5 days, 8 forecasts per day (3-hour intervals)
                |> List.map (fun i -> 
                    let hourOffset = i * 3
                    let dayOfForecast = hourOffset / 24
                    let hourOfDay = hourOffset % 24
                    
                    // Daily temperature cycle (cooler at night, warmer during day)
                    let dailyCycle = Math.Sin((float hourOfDay - 6.0) / 24.0 * 2.0 * Math.PI) * 8.0
                    
                    // Multi-day temperature trend (slight variation over days)
                    let multiDayTrend = Math.Sin(float dayOfForecast / 5.0 * Math.PI) * 3.0
                    
                    // Random variation
                    let randomVariation = random.NextDouble() * 4.0 - 2.0
                    
                    let forecastTemp = baseWeather.Temperature + dailyCycle + multiDayTrend + randomVariation
                    
                    // Weather conditions with some persistence (weather systems last a while)
                    let conditionPersistence = i / 8 // Group by day mostly
                    let conditionSeed = random.Next(1000) + conditionPersistence * 100
                    Random(conditionSeed).Next() |> ignore // Seed the pattern
                    
                    let conditions = [| Sunny; Cloudy; PartlyCloudy; Rainy |]
                    let forecastCondition = 
                        if hourOfDay >= 6 && hourOfDay <= 18 then // Daytime
                            conditions.[Random(conditionSeed).Next(conditions.Length)]
                        else // Nighttime - adjust conditions
                            match conditions.[Random(conditionSeed).Next(conditions.Length)] with
                            | Sunny -> PartlyCloudy // Clear nights
                            | other -> other
                    
                    {
                        Temperature = forecastTemp
                        Condition = forecastCondition
                        Humidity = Math.Max(20, Math.Min(90, baseWeather.Humidity + random.Next(20) - 10))
                        WindSpeed = Math.Max(0.0, baseWeather.WindSpeed + random.NextDouble() * 6.0 - 3.0)
                        Timestamp = DateTime.UtcNow.AddHours(float hourOffset)
                        Location = coordinate
                    })
            
            // Cache the result
            do! cacheWeatherData config coordinate "5day" forecasts
            
            return forecasts
    with
    | ex -> 
        printfn "❌ Error generating forecast: %s" ex.Message
        return []
}

/// Get historical weather data (mock implementation)
let getHistoricalWeather (coordinate: Coordinate) (startDate: DateTime) (endDate: DateTime) = async {
    // This is a simplified mock implementation
    // In a real app, you'd use a service like Visual Crossing Weather API or NOAA
    let random = Random()
    let days = (endDate - startDate).Days
    
    return [
        for i in 0..days do
            let date = startDate.AddDays(float i)
            let baseTemp = 15.0 + 10.0 * Math.Sin(float i / 30.0 * Math.PI) // Seasonal variation
            yield {
                Date = date
                AverageTemp = baseTemp + random.NextDouble() * 4.0 - 2.0
                MinTemp = baseTemp - 5.0 + random.NextDouble() * 2.0
                MaxTemp = baseTemp + 5.0 + random.NextDouble() * 2.0
                Condition = if random.NextDouble() > 0.7 then Rainy else Sunny
                Location = coordinate
            }
    ]
}

/// Check if weather meets temperature requirements
let meetsTemperatureRange (weather: WeatherInfo) (range: TemperatureRange) =
    weather.Temperature >= range.MinCelsius && weather.Temperature <= range.MaxCelsius

/// Filter locations by weather conditions
let filterByWeatherConditions (weathers: WeatherInfo list) (range: TemperatureRange) (avoidRain: bool) =
    weathers
    |> List.filter (fun w -> 
        meetsTemperatureRange w range &&
        (not avoidRain || w.Condition <> Rainy))

/// Get weather summary for a route
let getRouteSummary (weathers: WeatherInfo list) =
    if List.isEmpty weathers then
        None
    else
        let temps = weathers |> List.map (fun w -> w.Temperature)
        let avgTemp = temps |> List.average
        let minTemp = temps |> List.min
        let maxTemp = temps |> List.max
        
        let rainyLocations = weathers |> List.filter (fun w -> w.Condition = Rainy) |> List.length
        let sunnyLocations = weathers |> List.filter (fun w -> w.Condition = Sunny) |> List.length
        
        Some {|
            AverageTemperature = avgTemp
            MinTemperature = minTemp
            MaxTemperature = maxTemp
            TotalLocations = weathers.Length
            RainyLocations = rainyLocations
            SunnyLocations = sunnyLocations
            MostCommonCondition = 
                weathers 
                |> List.groupBy (fun w -> w.Condition)
                |> List.maxBy (fun (_, group) -> group.Length)
                |> fst
        |}

/// Find best weather window in forecast
let findBestWeatherWindow (forecasts: WeatherInfo list) (range: TemperatureRange) (windowHours: int) =
    if forecasts.Length < windowHours / 3 then // 3-hour intervals
        None
    else
        let windowSize = windowHours / 3
        forecasts
        |> List.windowed windowSize
        |> List.indexed
        |> List.filter (fun (_, window) -> 
            window |> List.forall (fun w -> meetsTemperatureRange w range))
        |> List.sortBy (fun (_, window) -> 
            // Score by average temperature (closer to middle of range is better)
            let avgTemp = window |> List.map (fun w -> w.Temperature) |> List.average
            let rangeCenter = (range.MinCelsius + range.MaxCelsius) / 2.0
            Math.Abs(avgTemp - rangeCenter))
        |> List.tryHead
        |> Option.map (fun (startIndex, window) -> 
            {|
                StartTime = window.Head.Timestamp
                EndTime = window |> List.last |> fun w -> w.Timestamp
                AverageTemperature = window |> List.map (fun w -> w.Temperature) |> List.average
                Conditions = window |> List.map (fun w -> w.Condition)
            |})

/// Granular forecasting configuration based on time proximity
type ForecastGranularity = {
    HoursUntilTravel: float
    ForecastIntervalHours: float
    DetailLevel: string
    UpdateFrequency: float // hours between updates
}

/// Get forecast granularity based on time until travel
let getForecastGranularity (hoursUntilTravel: float) =
    if hoursUntilTravel <= 6.0 then
        { HoursUntilTravel = hoursUntilTravel; ForecastIntervalHours = 0.5; DetailLevel = "Hyper-detailed"; UpdateFrequency = 0.25 }
    elif hoursUntilTravel <= 24.0 then
        { HoursUntilTravel = hoursUntilTravel; ForecastIntervalHours = 1.0; DetailLevel = "High-detail"; UpdateFrequency = 1.0 }
    elif hoursUntilTravel <= 72.0 then
        { HoursUntilTravel = hoursUntilTravel; ForecastIntervalHours = 3.0; DetailLevel = "Standard"; UpdateFrequency = 3.0 }
    elif hoursUntilTravel <= 168.0 then // 7 days
        { HoursUntilTravel = hoursUntilTravel; ForecastIntervalHours = 6.0; DetailLevel = "Broad"; UpdateFrequency = 6.0 }
    else
        { HoursUntilTravel = hoursUntilTravel; ForecastIntervalHours = 12.0; DetailLevel = "Overview"; UpdateFrequency = 12.0 }

/// Enhanced granular weather forecast with variable detail levels
let getGranularWeatherForecast (config: AppConfig) (coordinate: Coordinate) (travelTime: DateTime) = async {
    try
        let hoursUntilTravel = (travelTime - DateTime.Now).TotalHours
        
        // Try cache first
        let cacheKey = sprintf "granular_%.1f" hoursUntilTravel
        match getCachedWeatherData config coordinate cacheKey with
        | Some cachedData -> return cachedData
        | None ->
            let granularity = getForecastGranularity hoursUntilTravel
            
            let numForecasts = 
                if hoursUntilTravel <= 6.0 then 48 // 30-minute intervals for 24 hours
                elif hoursUntilTravel <= 24.0 then 72 // 1-hour intervals for 3 days
                elif hoursUntilTravel <= 72.0 then 64 // 3-hour intervals for 8 days
                else 56 // 6-12 hour intervals for 2 weeks
            
            let baseWeather = generateMockWeather coordinate
            let random = Random()
            
            let forecasts = 
                [0..numForecasts-1]
                |> List.map (fun i -> 
                    let hourOffset = float i * granularity.ForecastIntervalHours
                    let forecastTime = DateTime.Now.AddHours(hourOffset)
                    let dayOfForecast = int (hourOffset / 24.0)
                    let hourOfDay = int (hourOffset % 24.0)
                    
                    // Enhanced granular patterns
                    let granularTempVariation = 
                        match granularity.DetailLevel with
                        | "Hyper-detailed" ->
                            // Micro-weather patterns (cloud movements, local effects)
                            let microCycle = Math.Sin(hourOffset * 2.0 * Math.PI / 4.0) * 1.5 // 4-hour micro cycles
                            let cloudEffect = if random.NextDouble() < 0.3 then random.NextDouble() * 2.0 - 1.0 else 0.0
                            microCycle + cloudEffect
                        | "High-detail" ->
                            // Detailed hourly patterns
                            let hourlyVariation = Math.Sin(hourOffset * 2.0 * Math.PI / 8.0) * 2.0 // 8-hour cycles
                            hourlyVariation + (random.NextDouble() * 1.0 - 0.5)
                        | _ ->
                            // Standard variation for longer-term forecasts
                            random.NextDouble() * 3.0 - 1.5
                    
                    // Enhanced daily cycle with granular detail
                    let dailyCycle = 
                        let baseDaily = Math.Sin((float hourOfDay - 6.0) / 24.0 * 2.0 * Math.PI) * 8.0
                        let granularAdjustment = 
                            if granularity.DetailLevel = "Hyper-detailed" then
                                // Add thermal lag and local heating effects
                                let thermalLag = Math.Sin((float hourOfDay - 8.0) / 24.0 * 2.0 * Math.PI) * 2.0
                                baseDaily + thermalLag
                            else baseDaily
                        granularAdjustment
                    
                    // Multi-day weather system evolution
                    let weatherSystemTrend = Math.Sin(float dayOfForecast / 3.0 * Math.PI) * 4.0
                    
                    let forecastTemp = baseWeather.Temperature + dailyCycle + weatherSystemTrend + granularTempVariation
                    
                    // Enhanced condition modeling with persistence and transitions
                    let conditionSeed = i / max 1 (int (8.0 / granularity.ForecastIntervalHours))
                    let persistentRandom = Random(random.Next(1000) + conditionSeed * 100)
                    
                    let forecastCondition = 
                        if granularity.DetailLevel = "Hyper-detailed" then
                            // Model rapid weather transitions for near-term forecasts
                            let transitionProb = if i > 0 then 0.1 else 0.3 // Weather persistence
                            if persistentRandom.NextDouble() < transitionProb then
                                // Weather change
                                match persistentRandom.Next(6) with
                                | 0 -> Sunny | 1 -> PartlyCloudy | 2 -> Cloudy 
                                | 3 -> Rainy | 4 -> Stormy | _ -> Foggy
                            else
                                // Keep similar to base weather
                                baseWeather.Condition
                        else
                            // Longer-term pattern modeling
                            let conditions = [| Sunny; Cloudy; PartlyCloudy; Rainy |]
                            conditions.[persistentRandom.Next(conditions.Length)]
                    
                    // Enhanced wind and humidity modeling
                    let granularWindSpeed = 
                        let baseWind = baseWeather.WindSpeed
                        let windVariation = 
                            if granularity.DetailLevel = "Hyper-detailed" then
                                // Model wind gusts and local effects
                                let gustFactor = Math.Sin(hourOffset * Math.PI / 2.0) * 3.0
                                gustFactor + (random.NextDouble() * 4.0 - 2.0)
                            else
                                random.NextDouble() * 6.0 - 3.0
                        Math.Max(0.0, baseWind + windVariation)
                    
                    let granularHumidity = 
                        let baseHumidity = baseWeather.Humidity
                        let humidityAdjustment = 
                            match forecastCondition with
                            | Rainy | Stormy -> 20
                            | Foggy -> 15
                            | Sunny -> -10
                            | _ -> 0
                        Math.Max(10, Math.Min(95, baseHumidity + humidityAdjustment + random.Next(10) - 5))
                    
                    {
                        Temperature = forecastTemp
                        Condition = forecastCondition
                        Humidity = granularHumidity
                        WindSpeed = granularWindSpeed
                        Timestamp = forecastTime
                        Location = coordinate
                    })
            
            // Cache the result
            do! cacheWeatherData config coordinate cacheKey forecasts
            
            return forecasts
    with
    | ex -> 
        printfn "❌ Error generating granular forecast: %s" ex.Message
        return []
}

/// Get forecast confidence based on time proximity and granularity
let getForecastConfidence (hoursUntilTravel: float) =
    if hoursUntilTravel <= 6.0 then 0.95
    elif hoursUntilTravel <= 24.0 then 0.85
    elif hoursUntilTravel <= 72.0 then 0.75
    elif hoursUntilTravel <= 168.0 then 0.65
    else 0.50

/// Analyze forecast reliability and suggest update timing
let analyzeForecastReliability (coordinate: Coordinate) (travelTime: DateTime) = async {
    let hoursUntilTravel = (travelTime - DateTime.Now).TotalHours
    let granularity = getForecastGranularity hoursUntilTravel
    let confidence = getForecastConfidence hoursUntilTravel
    
    let nextUpdateTime = DateTime.Now.AddHours(granularity.UpdateFrequency)
    
    return {|
        HoursUntilTravel = hoursUntilTravel
        Granularity = granularity
        Confidence = confidence
        NextRecommendedUpdate = nextUpdateTime
        RecommendedAction = 
            if hoursUntilTravel <= 6.0 then "Monitor for real-time changes"
            elif hoursUntilTravel <= 24.0 then "Check for updates every hour"
            elif hoursUntilTravel <= 72.0 then "Daily forecast review recommended"
            else "Weekly forecast review sufficient"
    |}
}

/// Compatibility function for RouteService (uses default config)
let getWeatherForecastCompat (coordinate: Coordinate) = async {
    let! defaultConfig = loadConfig()
    return! getWeatherForecast defaultConfig coordinate
}

/// Compatibility function for RouteService (uses default config)  
let getCurrentWeatherCompat (coordinate: Coordinate) = async {
    let! defaultConfig = loadConfig()
    return! getCurrentWeather defaultConfig coordinate
}
