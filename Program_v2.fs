open System
open System.Threading
open WeatherTraveler.Types
open WeatherTraveler.Configuration
open WeatherTraveler.CacheService
open WeatherTraveler.PreferencesService
open WeatherTraveler.LocationService
open WeatherTraveler.WeatherService
open WeatherTraveler.ExportService
open WeatherTraveler.GasPriceService

let printHeader() =
    Console.Clear()
    printfn "╔══════════════════════════════════════════════════════════════════════════════╗"
    printfn "║                      🌤️  WEATHER TRAVELER v2.0  🚗                          ║"
    printfn "║                   Smart Weather-Based Route Planning                         ║"
    printfn "║                      With Configuration Management                           ║"
    printfn "╚══════════════════════════════════════════════════════════════════════════════╝"
    printfn ""

let printMainMenu() =
    printfn "🎯 Main Menu:"
    printfn "   1. Demo All Features (Quick Tour)"
    printfn "   2. Plan a Trip"
    printfn "   3. Configuration & Settings"
    printfn "   4. User Profile & Preferences"
    printfn "   5. Cache Management"
    printfn "   6. Export Data"
    printfn "   7. About & Help"
    printfn "   0. Exit"
    printfn ""
    printf "Select an option (0-7): "

let demoAllFeatures (config: AppConfig) (profile: UserProfile) = async {
    printfn ""
    printfn "🌟 Welcome to Enhanced Weather Traveler Demo v2.0!"
    printfn ""
    printfn "This demonstration showcases all implemented features with the new"
    printfn "configuration management and user preferences system."
    printfn ""
    
    printfn "📍 Enhanced Features:"
    printfn "   • Configuration management with user preferences ✅ NEW"
    printfn "   • Intelligent caching system ✅ NEW"
    printfn "   • User profile and travel history ✅ NEW"
    printfn "   • Current weather detection and location services ✅"
    printfn "   • Temperature range-based route planning ✅" 
    printfn "   • Granular weather forecasting ✅"
    printfn "   • Region/spot specification with timing constraints ✅"
    printfn "   • Rain storm avoidance/targeting capabilities ✅"
    printfn "   • Google Earth export functionality (KML) ✅"
    printfn "   • Gas price estimation ✅"
    printfn ""

    // Demo with user's preferred locations
    let locations = [
        ("Denver, CO", { Latitude = 39.7392; Longitude = -104.9903 })
        ("Las Vegas, NV", { Latitude = 36.1699; Longitude = -115.1398 })
        ("Los Angeles, CA", { Latitude = 34.0522; Longitude = -118.2437 })
    ]
    
    printfn "🌍 Fetching Current Weather (using cache if available):"
    let! weatherResults = 
        locations 
        |> List.map (fun (name, coord) -> async {
            let! weather = getCurrentWeather config coord
            return (name, weather)
        })
        |> Async.Parallel
    
    for (location, weatherOpt) in weatherResults do
        match weatherOpt with
        | Some weather ->
            let conditionIcon = 
                match weather.Condition with
                | Sunny -> "☀️" | PartlyCloudy -> "⛅" | Cloudy -> "☁️"
                | Rainy -> "🌧️" | Stormy -> "⛈️" | Snowy -> "❄️" | Foggy -> "🌫️"
            printfn "   %s: %.1f°C %s (%A) | Wind: %.1f km/h | Humidity: %d%%" 
                    location weather.Temperature conditionIcon weather.Condition weather.WindSpeed weather.Humidity
        | None ->
            printfn "   %s: Weather data unavailable" location
    
    printfn ""
    printfn "🎯 User Preferences Applied:"
    printfn "   Temperature Range: %.1f°C - %.1f°C" 
        profile.Preferences.PreferredTemperatureRange.MinCelsius 
        profile.Preferences.PreferredTemperatureRange.MaxCelsius
    printfn "   Avoid Rain: %b" profile.Preferences.AvoidRainByDefault
    printfn "   Max Driving Hours: %d/day" profile.Preferences.MaxDrivingHoursPerDay
    printfn "   Preferred Departure: %s" (profile.Preferences.PreferredDepartureTime.ToString(@"hh\:mm"))
    printfn ""
    
    // Cache statistics
    let! cacheStats = getCacheStats config
    printfn "💾 Cache Performance:"
    printfn "   Cache Entries: %d files, %d in memory" cacheStats.FileCacheEntries cacheStats.MemoryCacheEntries
    printfn "   Cache Size: %.2f MB" cacheStats.TotalSizeMB
    printfn "   Cache Directory: %s" cacheStats.CacheDirectory
    printfn ""
    
    return Array.choose snd weatherResults |> Array.toList
}

let planTrip (config: AppConfig) (profile: UserProfile) = async {
    printfn ""
    printfn "🎯 Trip Planning Assistant"
    printfn ""
    
    // Use saved route templates if available
    if not profile.SavedRoutes.IsEmpty then
        printfn "💾 Your Saved Route Templates:"
        profile.SavedRoutes
        |> List.take (min 3 profile.SavedRoutes.Length)
        |> List.iteri (fun i template ->
            printfn "   %d. %s (used %d times)" (i + 1) template.Name template.UseCount
        )
        printfn ""
    
    // Demo trip planning with user preferences
    printfn "🗺️  Planning a route with your preferences..."
    let sampleRoute = [
        { Name = "Start: Denver, CO"; Coordinate = { Latitude = 39.7392; Longitude = -104.9903 }; PreferredArrivalDate = None; PreferredDepartureDate = Some DateTime.Now; IsRequired = true }
        { Name = "Stop: Grand Canyon, AZ"; Coordinate = { Latitude = 36.1069; Longitude = -112.1129 }; PreferredArrivalDate = Some (DateTime.Now.AddHours(8.0)); PreferredDepartureDate = Some (DateTime.Now.AddHours(32.0)); IsRequired = true }
        { Name = "End: Las Vegas, NV"; Coordinate = { Latitude = 36.1699; Longitude = -115.1398 }; PreferredArrivalDate = Some (DateTime.Now.AddHours(36.0)); PreferredDepartureDate = None; IsRequired = true }
    ]
    
    for location in sampleRoute do
        printfn "📍 %s" location.Name
          // Get weather forecast for arrival time using user's configuration
        match location.PreferredArrivalDate with
        | Some arrivalTime ->
            let! forecast = getGranularWeatherForecast config location.Coordinate arrivalTime
            if not forecast.IsEmpty then
                let arrivalWeather = forecast.Head
                let icon = match arrivalWeather.Condition with | Sunny -> "☀️" | PartlyCloudy -> "⛅" | Cloudy -> "☁️" | Rainy -> "🌧️" | Stormy -> "⛈️" | Snowy -> "❄️" | Foggy -> "🌫️"
                printfn "   Expected weather: %.1f°C %s (%A)" arrivalWeather.Temperature icon arrivalWeather.Condition
                
                // Check against user preferences
                let inTempRange = (arrivalWeather.Temperature >= profile.Preferences.PreferredTemperatureRange.MinCelsius && arrivalWeather.Temperature <= profile.Preferences.PreferredTemperatureRange.MaxCelsius)
                let rainWarning = profile.Preferences.AvoidRainByDefault && (arrivalWeather.Condition = Rainy || arrivalWeather.Condition = Stormy)
                
                printfn "   Temperature match: %s" (if inTempRange then "✅ Within your preferred range" else "⚠️  Outside preferred range")
                if rainWarning then
                    printfn "   Rain warning: ⚠️  Rain expected (you prefer to avoid rain)"
        | None ->
            printfn "   Flexible arrival time"
        printfn ""
    
    printfn "📊 Route Analysis (personalized for you):"
    let totalDistance = (calculateDistance sampleRoute.[0].Coordinate sampleRoute.[1].Coordinate + calculateDistance sampleRoute.[1].Coordinate sampleRoute.[2].Coordinate)
    printfn "   Total Distance: %.1f km" totalDistance
    printfn "   Estimated Driving Time: %.1f hours" (totalDistance / 80.0)
    printfn "   Estimated Fuel Cost: $%.2f" (totalDistance * 0.12)
    printfn "   Fits Daily Driving Limit: %s" (if (totalDistance / 80.0) <= float profile.Preferences.MaxDrivingHoursPerDay then "✅ Yes" else "⚠️  Exceeds your limit")
    printfn ""
}

let configurationMenu (config: AppConfig) = async {
    printfn ""
    printfn "⚙️  Configuration & Settings"
    printfn ""
    
    // Print current configuration summary
    printConfigSummary config
    
    // Configuration validation
    let warnings = validateConfig config
    if not warnings.IsEmpty then
        printfn "⚠️  Configuration Warnings:"
        warnings |> List.iter (fun warning -> printfn "   • %s" warning)
        printfn ""
    
    printfn "🔧 Configuration Options:"
    printfn "   1. Update API Keys"
    printfn "   2. Change Cache Settings"
    printfn "   3. Update Directories"
    printfn "   4. Export Configuration"
    printfn "   5. Reset to Defaults"
    printfn "   0. Back to Main Menu"
    printfn ""
    
    return config
}

let userProfileMenu (profile: UserProfile) = async {
    printfn ""
    printfn "👤 User Profile & Preferences"
    printfn ""
    
    // Print profile summary
    printProfileSummary profile
    
    // Show personalized recommendations if available
    let recommendations = getPersonalizedRecommendations profile
    if not recommendations.IsEmpty then
        printfn "💡 Personalized Recommendations:"
        recommendations |> List.iter (fun rec -> printfn "   • %s" rec)
        printfn ""
    
    printfn "👤 Profile Options:"
    printfn "   1. Update Preferences"
    printfn "   2. View Travel History"
    printfn "   3. Manage Saved Routes"
    printfn "   4. Update Travel Patterns"
    printfn "   5. Export Profile Data"
    printfn "   6. Notification Settings"
    printfn "   0. Back to Main Menu"
    printfn ""
    
    return profile
}

let cacheManagementMenu (config: AppConfig) = async {
    printfn ""
    printfn "💾 Cache Management"
    printfn ""
    
    let! cacheStats = getCacheStats config
    printfn "📊 Cache Statistics:"
    printfn "   File Cache Entries: %d" cacheStats.FileCacheEntries
    printfn "   Memory Cache Entries: %d" cacheStats.MemoryCacheEntries
    printfn "   Total Size: %.2f MB" cacheStats.TotalSizeMB
    printfn "   Cache Directory: %s" cacheStats.CacheDirectory
    printfn ""
    
    printfn "🧹 Cache Management Options:"
    printfn "   1. Clean Expired Cache"
    printfn "   2. Clear All Cache"
    printfn "   3. View Cache Details"
    printfn "   4. Change Cache Settings"
    printfn "   0. Back to Main Menu"
    printfn ""
}

let exportDataMenu (config: AppConfig) (profile: UserProfile) = async {
    printfn ""
    printfn "📤 Export Data"
    printfn ""
    
    printfn "📁 Export Options:"
    printfn "   1. Export User Profile (anonymized)"
    printfn "   2. Export Configuration (sanitized)"
    printfn "   3. Export Travel History"
    printfn "   4. Export Route Templates"
    printfn "   5. Generate Full Backup"
    printfn "   0. Back to Main Menu"
    printfn ""
}

let showAboutAndHelp() =
    printfn ""
    printfn "ℹ️  About Weather Traveler v2.0"
    printfn ""
    printfn "🌟 New in Version 2.0:"
    printfn "   • Complete configuration management system"
    printfn "   • Intelligent caching for improved performance"
    printfn "   • User profiles and travel history tracking"
    printfn "   • Personalized recommendations"
    printfn "   • Enhanced data export capabilities"
    printfn ""
    printfn "🏗️  Architecture:"
    printfn "   • F# functional programming approach"
    printfn "   • Immutable data structures"
    printfn "   • Async computation expressions"
    printfn "   • Modular service-based design"
    printfn ""
    printfn "🔑 Features:"
    printfn "   • Weather-aware route planning"
    printfn "   • Temperature range compliance"
    printfn "   • Granular weather forecasting"
    printfn "   • Gas price optimization"
    printfn "   • Google Earth KML export"
    printfn "   • User preference learning"
    printfn ""
    printfn "📧 Support:"
    printfn "   • Version: 2.0.0"
    printfn "   • Built with F# and .NET 9"
    printfn "   • Open source weather travel planning"
    printfn ""

let runApplication() = async {
    try
        printHeader()
        
        // Initialize configuration and user profile
        let! config = loadConfig()
        let! profile = loadUserProfile()
        
        // Initialize directories
        let! initResult = initializeDirectories config
        match initResult with
        | Ok _ -> printfn "✅ Application directories initialized"
        | Error msg -> printfn "⚠️  Directory initialization warning: %s" msg
        
        // Clean up expired cache on startup
        let! cleanupResult = cleanupExpiredCache config
        match cleanupResult with
        | Ok cleaned when cleaned > 0 -> printfn "🧹 Cleaned %d expired cache entries" cleaned
        | _ -> ()
        
        printfn ""
        printfn "🎉 Welcome back, %s!" profile.DisplayName
        printfn "   Member since: %s" (profile.CreatedAt.ToString("yyyy-MM-dd"))
        if profile.TravelHistory.Length > 0 then
            printfn "   Travel history: %d trips" profile.TravelHistory.Length
        printfn ""
        
        let mutable continueApp = true
        let mutable currentConfig = config
        let mutable currentProfile = profile
        
        while continueApp do
            printMainMenu()
            let input = Console.ReadLine()
            
            match input with
            | "1" ->
                let! _ = demoAllFeatures currentConfig currentProfile
                printfn ""
                printfn "Press any key to continue..."
                Console.ReadKey() |> ignore
                printHeader()
                
            | "2" ->
                let! _ = planTrip currentConfig currentProfile
                printfn ""
                printfn "Press any key to continue..."
                Console.ReadKey() |> ignore
                printHeader()
                
            | "3" ->
                let! newConfig = configurationMenu currentConfig
                currentConfig <- newConfig
                printfn ""
                printfn "Press any key to continue..."
                Console.ReadKey() |> ignore
                printHeader()
                
            | "4" ->
                let! newProfile = userProfileMenu currentProfile
                currentProfile <- newProfile
                printfn ""
                printfn "Press any key to continue..."
                Console.ReadKey() |> ignore
                printHeader()
                
            | "5" ->
                let! _ = cacheManagementMenu currentConfig
                printfn ""
                printfn "Press any key to continue..."
                Console.ReadKey() |> ignore
                printHeader()
                
            | "6" ->
                let! _ = exportDataMenu currentConfig currentProfile
                printfn ""
                printfn "Press any key to continue..."
                Console.ReadKey() |> ignore
                printHeader()
                
            | "7" ->
                showAboutAndHelp()
                printfn ""
                printfn "Press any key to continue..."
                Console.ReadKey() |> ignore
                printHeader()
                
            | "0" ->
                // Save configuration and profile before exit
                let! _ = saveConfig currentConfig
                let! _ = saveUserProfile currentProfile
                printfn ""
                printfn "👋 Thank you for using Weather Traveler!"
                printfn "   Configuration and profile saved successfully."
                printfn ""
                continueApp <- false
                
            | _ ->
                printfn ""
                printfn "❌ Invalid option. Please select 0-7."
                printfn ""
                Threading.Thread.Sleep(1000)
                printHeader()
        
        return 0
    with
    | ex ->
        printfn "❌ Application error: %s" ex.Message
        printfn ""
        printfn "Press any key to exit..."
        Console.ReadKey() |> ignore
        return 1
}

[<EntryPoint>]
let main argv =
    try
        printfn "🚀 Starting Weather Traveler v2.0..."
        let result = runApplication() |> Async.RunSynchronously
        result
    with
    | ex ->
        printfn "💥 Fatal error: %s" ex.Message
        1
