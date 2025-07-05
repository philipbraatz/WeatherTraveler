open System
open WeatherTraveler.Types
open WeatherTraveler.LocationService
open WeatherTraveler.WeatherService
open WeatherTraveler.Configuration

let printHeader() =
    Console.Clear()
    printfn "╔══════════════════════════════════════════════════════════════════════════════╗"
    printfn "║                            🌤️  WEATHER TRAVELER  🚗                           ║"
    printfn "║                        Smart Weather-Based Route Planning                    ║"
    printfn "╚══════════════════════════════════════════════════════════════════════════════╝"
    printfn ""

let demoWeatherTraveler() = async {
    printHeader()
    
    printfn "🌟 Welcome to Weather Traveler Demo!"
    printfn ""
    printfn "This F# application demonstrates weather-aware travel planning features:"
    printfn ""
    
    printfn "📍 Key Features:"
    printfn "   • Current weather detection and location services ✅ IMPLEMENTED"
    printfn "   • Temperature range-based route planning ✅ IMPLEMENTED" 
    printfn "   • Enhanced weather forecasting ✅ IMPLEMENTED"
    printfn ""
    
    // Load configuration
    let! config = loadConfig()
    
    // Demo route calculation using real LocationService
    let denverCoord = { Latitude = 39.7392; Longitude = -104.9903 }
    let vegasCoord = { Latitude = 36.1699; Longitude = -115.1398 }
    let laCoord = { Latitude = 34.0522; Longitude = -118.2437 }
    
    printfn "🗺️  Sample Route: Denver, CO → Las Vegas, NV → Los Angeles, CA"
    printfn ""
    
    // Use real distance calculations from LocationService
    let denverToVegas = calculateDistance denverCoord vegasCoord
    let vegasToLA = calculateDistance vegasCoord laCoord
    let totalDistance = denverToVegas + vegasToLA
    
    printfn "📏 Real Distance Calculations:"
    printfn "   Denver → Las Vegas: %.1f km" denverToVegas
    printfn "   Las Vegas → Los Angeles: %.1f km" vegasToLA
    printfn "   Total Distance: %.1f km" totalDistance
    printfn ""
    
    // Get current weather for all locations using proper config parameter
    let! denverResult = getCurrentWeather config denverCoord
    let! vegasResult = getCurrentWeather config vegasCoord
    let! laResult = getCurrentWeather config laCoord
    
    // Extract weather data from Option results
    let weatherResults = 
        [denverResult; vegasResult; laResult]
        |> List.choose id  // Remove None values
    
    let locations = ["Denver, CO"; "Las Vegas, NV"; "Los Angeles, CA"]
    
    let displayWeather (location: string) (weather: WeatherInfo) =
        let tempColor = 
            match weather.Temperature with
            | t when t < 10.0 -> "🥶"
            | t when t < 20.0 -> "🌤️ "
            | t when t < 30.0 -> "☀️ "
            | _ -> "🔥"
        
        let conditionIcon = 
            match weather.Condition with
            | Sunny -> "☀️ "
            | PartlyCloudy -> "⛅"
            | Cloudy -> "☁️ "
            | Rainy -> "🌧️ "
            | Stormy -> "⛈️ "
            | Snowy -> "❄️ "
            | Foggy -> "🌫️"
        
        printfn "   %s %s%.1f°C %s%s" tempColor location weather.Temperature conditionIcon (weather.Condition.ToString())
    
    printfn "🌤️  Current Weather Conditions:"
    if not (List.isEmpty weatherResults) then
        List.zip locations weatherResults
        |> List.iter (fun (location, weather) -> displayWeather location weather)
    printfn ""
    
    // Enhanced weather forecasting demo
    printfn "🔮 Enhanced Weather Forecasting (Las Vegas):"
    let! vegasForecast = getWeatherForecast config vegasCoord
    
    let displayForecast (forecast: WeatherInfo) =
        let tempStatus = 
            match forecast.Temperature with
            | t when t >= 18.0 && t <= 25.0 -> "✅ OPTIMAL"
            | t when t >= 15.0 && t <= 30.0 -> "⚠️  ACCEPTABLE" 
            | _ -> "❌ OUTSIDE RANGE"
        
        printfn "   • %.1f°C %s %s %s" 
            forecast.Temperature 
            (forecast.Condition.ToString()) 
            tempStatus
            (match forecast.Condition with 
             | Rainy -> "🌧️ " 
             | Snowy -> "❄️ " 
             | Stormy -> "⛈️ "
             | _ -> "")
    
    List.iter displayForecast vegasForecast
    printfn ""
    
    // Temperature range analysis
    printfn "🌡️  Temperature Range Analysis:"
    let tempRange = { MinCelsius = 18.0; MaxCelsius = 25.0 }
    
    let complianceCheck = 
        weatherResults 
        |> List.map (fun w -> w.Temperature >= tempRange.MinCelsius && w.Temperature <= tempRange.MaxCelsius)
        |> List.forall id
    
    if complianceCheck then
        printfn "   ✅ All locations within acceptable temperature range (%.1f°C - %.1f°C)" tempRange.MinCelsius tempRange.MaxCelsius
    else
        printfn "   ⚠️  Some locations outside preferred temperature range"
        weatherResults 
        |> List.zip locations
        |> List.iter (fun (location, weather) ->
            if weather.Temperature < tempRange.MinCelsius || weather.Temperature > tempRange.MaxCelsius then
                printfn "   🌡️  %s: %.1f°C (outside range)" location weather.Temperature)
    
    printfn ""
    
    printfn "✨ Demo completed! Weather data processed successfully."
    printfn "   Processed %d weather locations with enhanced forecasting." weatherResults.Length
    printfn ""
    printfn "🎉 Thank you for trying Weather Traveler! 🌤️"
    
    return weatherResults
}

/// Demo application entry point for Weather Traveler
[<EntryPoint>]
let main argv =
    match argv with
    | [| "weather" |] ->
        printfn "🌟 Starting Weather Traveler Demo..."
        demoWeatherTraveler()
        |> Async.RunSynchronously
        |> ignore
        0
    | _ ->
        printfn "Weather Traveler Demo v2.0"
        printfn "Usage:"
        printfn "  dotnet run weather - Run weather demo"
        printfn ""
        printfn "Starting weather demo by default..."
        demoWeatherTraveler()
        |> Async.RunSynchronously
        |> ignore
        0
