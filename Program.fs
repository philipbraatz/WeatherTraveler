open System
open WeatherTraveler.Types
open WeatherTraveler.LocationService
open WeatherTraveler.WeatherService
open WeatherTraveler.ExportService
open WeatherTraveler.GasPriceService

let printHeader() =
    Console.Clear()
    printfn "╔══════════════════════════════════════════════════════════════════════════════╗"
    printfn "║                            🌤️  WEATHER TRAVELER  🚗                           ║"
    printfn "║                        Smart Weather-Based Route Planning                    ║"
    printfn "╚══════════════════════════════════════════════════════════════════════════════╝"
    printfn ""

let demoWeatherTraveler() = async {
    printHeader()
    
    printfn "🌟 Welcome to Enhanced Weather Traveler Demo!"
    printfn ""
    printfn "This F# application demonstrates weather-aware travel planning features:"
    printfn ""
    
    printfn "📍 Key Features:"
    printfn "   • Current weather detection and location services ✅ IMPLEMENTED"
    printfn "   • Temperature range-based route planning (e.g., 12°C - 20°C) ✅ IMPLEMENTED" 
    printfn "   • Enhanced weather forecasting with realistic patterns ✅ IMPLEMENTED"
    printfn "   • Granular weather forecasting as travel time approaches ✅ IMPLEMENTED"
    printfn "   • Region/spot specification with timing constraints ✅ IMPLEMENTED"
    printfn "   • Rain storm avoidance/targeting capabilities ✅ IMPLEMENTED"
    printfn "   • Google Earth export functionality (KML) ✅ IMPLEMENTED"
    printfn "   • Gas price estimation ✅ IMPLEMENTED"
    printfn ""
    
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
    
    // Demo real weather fetching
    printfn "🌡️  Fetching Real Weather Data (Enhanced Mock Implementation):"
    
    // Get current weather for each location
    let! denverWeather = getCurrentWeather denverCoord
    let! vegasWeather = getCurrentWeather vegasCoord
    let! laWeather = getCurrentWeather laCoord
    
    let weatherResults = 
        [denverWeather; vegasWeather; laWeather]
        |> List.choose id
    
    let locations = ["Denver, CO"; "Las Vegas, NV"; "Los Angeles, CA"]
    
    let displayWeather (location: string) (weather: WeatherInfo) =
        let conditionIcon = 
            match weather.Condition with
            | Sunny -> "☀️"
            | PartlyCloudy -> "⛅"
            | Cloudy -> "☁️"
            | Rainy -> "🌧️"
            | Stormy -> "⛈️"
            | Snowy -> "❄️"
            | Foggy -> "🌫️"
        printfn "   %s: %.1f°C %s (%A) | Wind: %.1f km/h | Humidity: %d%%" 
                location weather.Temperature conditionIcon weather.Condition weather.WindSpeed weather.Humidity
    
    List.zip locations weatherResults
    |> List.iter (fun (loc, weather) -> displayWeather loc weather)
    
    printfn ""
    
    // Demo weather analysis
    if not (List.isEmpty weatherResults) then
        let summary = getRouteSummary weatherResults
        match summary with
        | Some s ->
            printfn "📊 Weather Analysis:"
            printfn "   Average Temperature: %.1f°C" s.AverageTemperature
            printfn "   Temperature Range: %.1f°C to %.1f°C" s.MinTemperature s.MaxTemperature
            printfn "   Most Common Condition: %A" s.MostCommonCondition
            printfn "   Locations with Rain: %d/%d" s.RainyLocations s.TotalLocations
            printfn ""
        | None -> ()
    
    // Demo 5-day forecast for Las Vegas
    printfn "📅 5-Day Weather Forecast for Las Vegas (Next 24 Hours):"
    let! vegasForecast = getWeatherForecast vegasCoord
    
    let displayForecast (forecast: WeatherInfo) =
        let time = forecast.Timestamp.ToString("MMM dd HH:mm")
        let conditionIcon = 
            match forecast.Condition with
            | Sunny -> "☀️"
            | PartlyCloudy -> "⛅"
            | Cloudy -> "☁️" 
            | Rainy -> "🌧️"
            | Stormy -> "⛈️"
            | Snowy -> "❄️"
            | Foggy -> "🌫️"
        printfn "   %s: %.1f°C %s (%A)" time forecast.Temperature conditionIcon forecast.Condition
    
    vegasForecast
    |> List.take 8 // Show first day (8 x 3-hour periods = 24 hours)
    |> List.iter displayForecast
    
    printfn ""
    
    // Demo best weather window finding
    let tempRange = { MinCelsius = 18.0; MaxCelsius = 25.0 }
    let bestWindow = findBestWeatherWindow vegasForecast tempRange 12
    
    match bestWindow with
    | Some window ->
        printfn "🎯 Best 12-Hour Weather Window (18°C-25°C):"
        printfn "   From: %s" (window.StartTime.ToString("MMM dd HH:mm"))
        printfn "   To: %s" (window.EndTime.ToString("MMM dd HH:mm"))
        printfn "   Average Temperature: %.1f°C" window.AverageTemperature
        printfn "   Conditions: %A" window.Conditions
        printfn ""
    | None ->
        printfn "⚠️  No suitable 12-hour weather window found in forecast"
        printfn ""
    
    // Route analysis
    printfn "📊 Route Analysis:"
    printfn "   Total Distance: %.1f km" totalDistance
    printfn "   Estimated Driving Time: %.1f hours" (totalDistance / 80.0)
    printfn "   Estimated Fuel Cost: $%.2f" (totalDistance * 0.12)
    printfn ""
    
    // Temperature range check
    let overallTempRange = { MinCelsius = 12.0; MaxCelsius = 30.0 }
    let temperatures = weatherResults |> List.map (fun w -> w.Temperature)
    let compliant = temperatures |> List.forall (fun t -> t >= overallTempRange.MinCelsius && t <= overallTempRange.MaxCelsius)
    
    printfn "🌡️  Temperature Compliance (12°C - 30°C): %s" (if compliant then "✅ All locations within range" else "⚠️  Some locations outside range")
    printfn ""
    
    // Rain avoidance demo
    let rainyLocations = weatherResults |> List.filter (fun w -> w.Condition = Rainy)
    if not (List.isEmpty rainyLocations) then
        printfn "🌧️  Rain Avoidance Alert:"
        printfn "   %d location(s) currently experiencing rain" rainyLocations.Length
        printfn "   Consider alternative timing or routes"
        printfn ""
    
    printfn "📤 Export Options Available:"
    printfn "   • Google Earth KML file export"
    printfn "   • CSV route data export" 
    printfn "   • Route summary report"
    printfn ""
    
    printfn "⛽ Gas Price Features:"
    printfn "   • Real-time fuel price monitoring"
    printfn "   • Cheapest station recommendations"
    printfn "   • Fuel budget estimation"
    printfn ""
    
    printfn "🎯 Advanced Planning Features:"
    printfn "   • Historical weather pattern analysis"
    printfn "   • Storm avoidance routing"
    printfn "   • Multi-day itinerary optimization"
    printfn "   • Custom waypoint scheduling"
    printfn ""
    
    printfn "🌟 This is an enhanced demonstration of the F# Weather Traveler application!"
    printfn "   The full version would integrate with real APIs for:"
    printfn "   - OpenWeatherMap for weather data (Framework ready! 🔧)"
    printfn "   - Google Maps for routing"
    printfn "   - Gas price APIs for fuel costs"
    printfn ""
    printfn "🎉 Thank you for trying Weather Traveler! 🌤️"
    
    // --- Granular Weather Forecasting Demo ---
    printfn "🕒 Granular Weather Forecasting as Travel Time Approaches:\n"
    let vegasName = "Las Vegas, NV"
    let vegasDemoTimes =
        [ ("2 hours from now",   DateTime.Now.AddHours(2.0))
          ("12 hours from now",  DateTime.Now.AddHours(12.0))
          ("36 hours from now",  DateTime.Now.AddHours(36.0))
          ("5 days from now",    DateTime.Now.AddDays(5.0)) ]

    for (label, travelTime) in vegasDemoTimes do
        let! forecast = getGranularWeatherForecast vegasCoord travelTime
        let hoursUntil = (travelTime - DateTime.Now).TotalHours
        let granularity = getForecastGranularity hoursUntil
        printfn "🔹 Forecast for %s (Travel in %.1f hours):" label hoursUntil
        printfn "   • Forecast interval: %.1f hours" granularity.ForecastIntervalHours
        printfn "   • Detail level: %s" granularity.DetailLevel
        printfn "   • Forecast points: %d" (forecast.Length)
        printfn "   • Sample forecast (first 5 points):"
        forecast
        |> List.truncate 5
        |> List.iter (fun w ->
            let t = w.Timestamp.ToString("MMM dd HH:mm")
            let icon = match w.Condition with | Sunny -> "☀️" | PartlyCloudy -> "⛅" | Cloudy -> "☁️" | Rainy -> "🌧️" | Stormy -> "⛈️" | Snowy -> "❄️" | Foggy -> "🌫️"
            printfn "     %s: %.1f°C %s (%A)" t w.Temperature icon w.Condition)
        printfn ""
    printfn "--- End of Granular Forecasting Demo ---\n"
    
    // --- Region/Spot Specification with Timing Constraints Demo ---
    printfn "📍 Region/Spot Specification with Timing Constraints:\n"
    
    // Create travel locations with specific timing constraints
    let nationalParksTrip = [
        { Name = "Grand Canyon National Park"; 
          Coordinate = { Latitude = 36.0544; Longitude = -112.1401 }; 
          PreferredArrivalDate = Some (DateTime.Now.AddDays(2.0)); 
          PreferredDepartureDate = Some (DateTime.Now.AddDays(4.0)); 
          IsRequired = true }
        { Name = "Zion National Park"; 
          Coordinate = { Latitude = 37.2982; Longitude = -113.0263 }; 
          PreferredArrivalDate = Some (DateTime.Now.AddDays(4.5)); 
          PreferredDepartureDate = Some (DateTime.Now.AddDays(6.0)); 
          IsRequired = true }
        { Name = "Bryce Canyon National Park"; 
          Coordinate = { Latitude = 37.5930; Longitude = -112.1871 }; 
          PreferredArrivalDate = Some (DateTime.Now.AddDays(6.5)); 
          PreferredDepartureDate = Some (DateTime.Now.AddDays(8.0)); 
          IsRequired = false }
    ]
    
    printfn "🏞️ Planning a National Parks Road Trip with Timing Constraints:"
    printfn ""
    
    for location in nationalParksTrip do
        printfn "📌 %s:" location.Name
        match location.PreferredArrivalDate, location.PreferredDepartureDate with
        | Some arrival, Some departure ->
            printfn "   • Preferred arrival: %s" (arrival.ToString("MMM dd, yyyy HH:mm"))
            printfn "   • Preferred departure: %s" (departure.ToString("MMM dd, yyyy HH:mm"))
            printfn "   • Duration: %.1f days" ((departure - arrival).TotalDays)
        | Some arrival, None ->
            printfn "   • Preferred arrival: %s" (arrival.ToString("MMM dd, yyyy HH:mm"))
            printfn "   • Open-ended stay"
        | None, Some departure ->
            printfn "   • Must depart by: %s" (departure.ToString("MMM dd, yyyy HH:mm"))
        | None, None ->
            printfn "   • Flexible timing"
        
        printfn "   • Required stop: %s" (if location.IsRequired then "Yes" else "Optional")
        
        // Get weather forecast for the preferred arrival time
        match location.PreferredArrivalDate with
        | Some arrivalTime ->
            let! forecast = getGranularWeatherForecast location.Coordinate arrivalTime
            if not forecast.IsEmpty then
                let arrivalWeather = forecast.Head
                let icon = match arrivalWeather.Condition with | Sunny -> "☀️" | PartlyCloudy -> "⛅" | Cloudy -> "☁️" | Rainy -> "🌧️" | Stormy -> "⛈️" | Snowy -> "❄️" | Foggy -> "🌫️"
                printfn "   • Expected weather on arrival: %.1f°C %s (%A)" arrivalWeather.Temperature icon arrivalWeather.Condition
                
                // Check if weather meets typical outdoor activity conditions
                let suitableForOutdoor = arrivalWeather.Temperature >= 10.0 && arrivalWeather.Temperature <= 30.0 && arrivalWeather.Condition <> Stormy
                printfn "   • Suitable for outdoor activities: %s" (if suitableForOutdoor then "✅ Yes" else "⚠️  Marginal")
        | None -> ()
        
        printfn ""
    
    // Demonstrate constraint validation
    printfn "🔍 Timing Constraint Analysis:"
    let mutable hasConflicts = false
    
    for i in 0..nationalParksTrip.Length-2 do
        let current = nationalParksTrip.[i]
        let next = nationalParksTrip.[i+1]
        
        match current.PreferredDepartureDate, next.PreferredArrivalDate with
        | Some departure, Some arrival ->
            let travelTime = calculateDistance current.Coordinate next.Coordinate / 80.0 // hours at 80km/h
            let availableTime = (arrival - departure).TotalHours
            
            if availableTime >= travelTime then
                printfn "✅ %s → %s: %.1f hours available, %.1f hours needed" current.Name next.Name availableTime travelTime
            else
                printfn "⚠️  %s → %s: Only %.1f hours available, %.1f hours needed (TIGHT SCHEDULE)" current.Name next.Name availableTime travelTime
                hasConflicts <- true
        | _ -> ()
    
    if not hasConflicts then
        printfn "✅ All timing constraints are feasible!"
    else
        printfn "⚠️  Some timing constraints may be challenging - consider adjusting schedule"
    
    printfn ""
    printfn "🎯 Advanced Constraint Features:"
    printfn "   • Flexible vs. rigid arrival/departure times"
    printfn "   • Required vs. optional stops optimization"
    printfn "   • Multi-day weather window analysis"
    printfn "   • Automatic schedule adjustment recommendations"
    printfn ""
    printfn "--- End of Region/Spot Specification Demo ---\n"
      // --- Google Earth Export Functionality (KML) Demo ---
    printfn "🌍 Google Earth Export Functionality (KML):\n"
    
    // Create a sample route for KML export demo
    let exportRoute = {
        Segments = [
            {
                From = { Name = "Denver, CO"; Coordinate = denverCoord; PreferredArrivalDate = None; PreferredDepartureDate = None; IsRequired = true }
                To = { Name = "Las Vegas, NV"; Coordinate = vegasCoord; PreferredArrivalDate = None; PreferredDepartureDate = None; IsRequired = true }
                Distance = denverToVegas
                EstimatedDrivingTime = TimeSpan.FromHours(denverToVegas / 80.0)
                WeatherForecast = [List.head weatherResults]
            }
            {
                From = { Name = "Las Vegas, NV"; Coordinate = vegasCoord; PreferredArrivalDate = None; PreferredDepartureDate = None; IsRequired = true }
                To = { Name = "Los Angeles, CA"; Coordinate = laCoord; PreferredArrivalDate = None; PreferredDepartureDate = None; IsRequired = true }
                Distance = vegasToLA
                EstimatedDrivingTime = TimeSpan.FromHours(vegasToLA / 80.0)
                WeatherForecast = [weatherResults.[1]]
            }
        ]
        TotalDistance = totalDistance
        TotalTime = TimeSpan.FromHours(totalDistance / 80.0)
        EstimatedFuelCost = totalDistance * 0.12
        WeatherCompliance = true
    }
    
    printfn "📁 Generating KML Export Files:"
    printfn ""
    
    try
        // Generate KML content
        let kmlOptions = { 
            IncludeWeatherData = true
            IncludeAltitudeData = false
            RouteColor = "7f00ffff"
            PointIconScale = 1.2 
        }
        
        let kmlContent = generateKML exportRoute "Weather Traveler Route Demo" kmlOptions
        
        printfn "✅ KML Content Generated Successfully!"
        printfn "   📊 Content size: %d characters" kmlContent.Length
        printfn "   🗺️  Route points: %d locations" (exportRoute.Segments.Length + 1)
        printfn ""
        
        // Show preview of KML structure
        let kmlLines = kmlContent.Split('\n')
        printfn "📄 KML File Preview (first 10 lines):"
        kmlLines 
        |> Array.take (min 10 kmlLines.Length)
        |> Array.iter (fun line -> printfn "   %s" (line.Trim()))
        
        if kmlLines.Length > 10 then
            printfn "   ... (%d more lines)" (kmlLines.Length - 10)
        
        printfn ""
        
        // Demo potential file export (without actually writing to avoid file system issues)
        let potentialFilePath = "weather_traveler_route.kml"
        printfn "💾 Ready to Export:"
        printfn "   📁 Filename: %s" potentialFilePath
        printfn "   🌍 Compatible with: Google Earth, Google Maps, GPS devices"
        printfn "   📋 Features included:"
        printfn "      • Route waypoints with coordinates"
        printfn "      • Distance information for each segment"
        printfn "      • Weather data integration"
        printfn "      • Custom styling and icons"
        printfn ""
        
        // CSV export demo
        let csvContent = generateCSV exportRoute [] defaultCsvOptions
        let csvLines = csvContent.Split('\n')
        printfn "📊 CSV Export Preview (first 5 lines):"
        csvLines 
        |> Array.take (min 5 csvLines.Length)
        |> Array.iter (fun line -> printfn "   %s" (line.Trim()))
        printfn ""
        
        printfn "🎯 Export Features:"
        printfn "   • Customizable KML styling options"
        printfn "   • Weather data integration in placemarks"
        printfn "   • Multiple export formats (KML, CSV, Summary)"
        printfn "   • Compatible with major mapping platforms"
        
    with
    | ex ->
        printfn "❌ Export demo error: %s" ex.Message
    
    printfn ""
    printfn "--- End of Google Earth Export Demo ---\n"
      // --- Gas Price Estimation Demo ---
    printfn "⛽ Gas Price Estimation:\n"
    
    printfn "🛣️ Fuel Cost Analysis for Route:"
    printfn ""
    
    try
        // Vehicle specifications for fuel calculation
        let vehicleFuelEfficiency = 8.5 // liters per 100km (typical car)
        
        // Calculate fuel cost for the route
        let! totalFuelCost = calculateFuelCost exportRoute vehicleFuelEfficiency
        
        printfn "💰 Trip Fuel Cost Breakdown:"
        printfn "   🚗 Vehicle efficiency: %.1f L/100km" vehicleFuelEfficiency
        printfn "   📏 Total distance: %.1f km" exportRoute.TotalDistance
        printfn "   ⛽ Estimated fuel needed: %.1f liters" ((exportRoute.TotalDistance / 100.0) * vehicleFuelEfficiency)
        printfn "   💵 Total fuel cost: $%.2f" totalFuelCost
        printfn ""
        
        // Gas price search demo for key locations
        let searchOptions = {
            SearchRadius = 25.0
            FuelType = "regular"
            MaxResults = 5
            SortBy = "price"
            IncludeAmenities = true
        }
        
        printfn "🔍 Finding Cheapest Gas Stations Along Route:"
        printfn ""
        
        for segment in exportRoute.Segments do
            let locationName = segment.To.Name
            printfn "📍 Near %s:" locationName
            
            let! gasPrices = getGasPricesNearLocation segment.To.Coordinate searchOptions
            
            if not gasPrices.IsEmpty then
                gasPrices
                |> List.take 3  // Show top 3 cheapest
                |> List.iteri (fun i station ->
                    printfn "   %d. %s" (i + 1) station.Name
                    printfn "      💰 $%.2f/gallon ($%.2f/liter)" station.PricePerGallon station.PricePerLiter
                    printfn "      📍 %.1f km away" station.Distance
                    printfn "      🏪 %s" station.Brand
                    if not station.Amenities.IsEmpty then
                        printfn "      🛍️  Amenities: %s" (String.concat ", " station.Amenities)
                    printfn "")
            else
                printfn "   No gas stations found in search radius"
                printfn ""
        
        // Daily fuel budget estimation
        let daysOfTravel = 3
        let! dailyBudget = estimateDailyFuelBudget exportRoute daysOfTravel vehicleFuelEfficiency
        
        printfn "📊 Travel Budget Analysis:"
        printfn "   🗓️  Trip duration: %d days" dailyBudget.DaysOfTravel
        printfn "   💰 Total fuel cost: $%.2f" dailyBudget.TotalFuelCost
        printfn "   📅 Daily fuel budget: $%.2f" dailyBudget.DailyBudget
        printfn "   ⛽ Total fuel consumption: %.1f liters" dailyBudget.EstimatedFuelConsumption
        printfn ""
        
        // Find cheapest stations along route with detour limit
        let maxDetourKm = 10.0
        let! cheapestStations = findCheapestStationsAlongRoute exportRoute maxDetourKm
        
        printfn "🎯 Recommended Fuel Stops (within %.1f km detour):" maxDetourKm
        cheapestStations
        |> List.take 3
        |> List.iteri (fun i station ->
            printfn "   %d. %s - $%.2f/gallon" (i + 1) station.Name station.PricePerGallon
            printfn "      📍 %.1f km detour" station.Distance
            printfn "      💰 Potential savings vs. average: $%.2f" (1.65 - station.PricePerGallon) // Mock average price
            printfn "")
        
        // Best fuel-up timing predictions
        let! fuelUpRecommendations = predictBestFuelUpTimes exportRoute
        
        if not fuelUpRecommendations.IsEmpty then
            printfn "⏰ Optimal Fuel-Up Timing:"
            fuelUpRecommendations
            |> List.iter (fun recommendation -> printfn "   • %s" recommendation)
            printfn ""
        
        printfn "🎯 Advanced Gas Price Features:"
        printfn "   • Real-time price monitoring and alerts"
        printfn "   • Historical price trend analysis"
        printfn "   • Route optimization for fuel savings"
        printfn "   • Brand preference and loyalty program integration"
        printfn "   • Alternative fuel options (EV charging, diesel)"
        
    with
    | ex ->
        printfn "❌ Gas price demo error: %s" ex.Message
    
    printfn ""
    printfn "--- End of Gas Price Estimation Demo ---\n"
    
    return weatherResults
}

[<EntryPoint>]
let main argv =
    match argv with
    | [| "test" |] ->
        printfn "🧪 Running automated preference tests..."
        WeatherTraveler.AutomatedPreferenceTest.runAllTests()
        |> Async.RunSynchronously
        0
    | [| "interactive" |] ->
        printfn "🎛️ Starting interactive preference manager..."
        WeatherTraveler.InteractivePreferenceManager.runInteractivePreferenceManager "default_user"
        |> Async.RunSynchronously
        0
    | [| "demo" |] ->
        try
            printfn "🌟 Starting Enhanced Weather Traveler Demo..."
            let result = demoWeatherTraveler() |> Async.RunSynchronously
            printfn ""
            printfn "✨ Demo completed! Weather data processed successfully."
            printfn "   Processed %d weather locations with enhanced forecasting." result.Length
            0
        with
        | ex ->
            printfn "❌ An error occurred: %s" ex.Message
            1
    | _ ->
        printfn "Weather Traveler v2.0"
        printfn "Usage:"
        printfn "  dotnet run test        - Run automated preference tests"
        printfn "  dotnet run interactive - Start interactive preference manager"
        printfn "  dotnet run demo        - Run weather traveler demo"
        printfn ""
        printfn "Starting interactive preference manager by default..."
        WeatherTraveler.InteractivePreferenceManager.runInteractivePreferenceManager "default_user"
        |> Async.RunSynchronously
        0
