open System
open System.Threading
open WeatherTraveler.Types
open WeatherTraveler.Configuration
open WeatherTraveler.CacheService
open WeatherTraveler.PreferencesService
open WeatherTraveler.LocationService
open WeatherTraveler.WeatherService

let printHeader() =
    Console.Clear()
    printfn "╔══════════════════════════════════════════════════════════════════════════════╗"
    printfn "║                      🌤️  WEATHER TRAVELER v2.0  🚗                          ║"
    printfn "║                   Smart Weather-Based Route Planning                         ║"
    printfn "║                      With Configuration Management                           ║"
    printfn "╚══════════════════════════════════════════════════════════════════════════════╝"
    printfn ""

let demoBasicFeatures (config: AppConfig) (profile: UserProfile) = async {
    printfn "🌟 Weather Traveler v2.0 Demo"
    printfn ""
    printfn "📍 Enhanced Features:"
    printfn "   • Configuration management with user preferences ✅ NEW"
    printfn "   • Intelligent caching system ✅ NEW"
    printfn "   • User profile and travel history ✅ NEW"
    printfn "   • Current weather detection and location services ✅"
    printfn "   • Temperature range-based route planning ✅" 
    printfn "   • Granular weather forecasting ✅"
    printfn "   • Google Earth export functionality (KML) ✅"
    printfn "   • Gas price estimation ✅"
    printfn ""

    // Demo basic weather fetching with caching
    let testCoord = { Latitude = 39.7392; Longitude = -104.9903 }
    printfn "🌍 Fetching Weather for Denver, CO (using cache if available):"
    
    let! weather = getCurrentWeather config testCoord
    match weather with
    | Some w ->
        let icon = match w.Condition with | Sunny -> "☀️" | PartlyCloudy -> "⛅" | Cloudy -> "☁️" | Rainy -> "🌧️" | Stormy -> "⛈️" | Snowy -> "❄️" | Foggy -> "🌫️"
        printfn "   Denver: %.1f°C %s (%A) | Wind: %.1f km/h | Humidity: %d%%" w.Temperature icon w.Condition w.WindSpeed w.Humidity
    | None ->
        printfn "   Weather data unavailable"
    
    printfn ""
    printfn "🎯 User Preferences:"
    printfn "   Temperature Range: %.1f°C - %.1f°C" 
        profile.Preferences.PreferredTemperatureRange.MinCelsius 
        profile.Preferences.PreferredTemperatureRange.MaxCelsius
    printfn "   Avoid Rain: %b" profile.Preferences.AvoidRainByDefault
    printfn "   Max Driving Hours: %d/day" profile.Preferences.MaxDrivingHoursPerDay
    printfn ""
    
    // Cache statistics
    let! cacheStats = getCacheStats config
    printfn "💾 Cache Performance:"
    printfn "   Cache Entries: %d files, %d in memory" cacheStats.FileCacheEntries cacheStats.MemoryCacheEntries
    printfn "   Cache Size: %.2f MB" cacheStats.TotalSizeMB
    printfn ""
    
    return match weather with | Some w -> [w] | None -> []
}

let showConfigurationSummary (config: AppConfig) = async {
    printfn "⚙️  Configuration Summary:"
    printConfigSummary config
    
    let warnings = validateConfig config
    if not warnings.IsEmpty then
        printfn "⚠️  Configuration Warnings:"
        warnings |> List.iter (fun warning -> printfn "   • %s" warning)
        printfn ""
    
    return config
}

let showUserProfile (profile: UserProfile) = async {
    printfn "👤 User Profile Summary:"
    printProfileSummary profile
    
    let recommendations = getPersonalizedRecommendations profile
    if not recommendations.IsEmpty then
        printfn "💡 Personalized Recommendations:"
        recommendations |> List.iter (fun recommendation -> printfn "   • %s" recommendation)
        printfn ""
    
    return profile
}

let runBasicDemo() = async {
    try
        printHeader()
        
        // Initialize configuration and user profile
        printfn "🚀 Initializing Weather Traveler v2.0..."
        let! config = loadConfig()
        let! profile = loadUserProfile()
        
        // Initialize directories
        let! initResult = initializeDirectories config
        match initResult with
        | Ok _ -> printfn "✅ Application directories initialized"
        | Error msg -> printfn "⚠️  Directory initialization warning: %s" msg
        
        printfn ""
        printfn "🎉 Welcome, %s!" profile.DisplayName
        if profile.TravelHistory.Length > 0 then
            printfn "   Travel history: %d trips" profile.TravelHistory.Length
        printfn ""
        
        // Run basic feature demo
        let! _ = demoBasicFeatures config profile
        
        printfn "Press Enter to see configuration details..."
        Console.ReadLine() |> ignore
        printHeader()
        
        let! _ = showConfigurationSummary config
        
        printfn "Press Enter to see user profile details..."
        Console.ReadLine() |> ignore
        printHeader()
        
        let! _ = showUserProfile profile
        
        printfn "Press Enter to see cache management..."
        Console.ReadLine() |> ignore
        printHeader()
        
        let! cacheStats = getCacheStats config
        printfn "💾 Cache Management:"
        printfn "   File Cache Entries: %d" cacheStats.FileCacheEntries
        printfn "   Memory Cache Entries: %d" cacheStats.MemoryCacheEntries
        printfn "   Total Size: %.2f MB" cacheStats.TotalSizeMB
        printfn "   Cache Directory: %s" cacheStats.CacheDirectory
        printfn ""
        
        printfn "🧹 Cache Operations:"
        printfn "   1. Clean expired cache"
        printfn "   2. Clear all cache"
        printfn "   0. Continue"
        printf "Select option: "
        
        let input = Console.ReadLine()
        match input with
        | "1" ->
            let! cleanupResult = cleanupExpiredCache config
            match cleanupResult with
            | Ok cleaned -> printfn "🧹 Cleaned %d expired cache entries" cleaned
            | Error msg -> printfn "❌ Error during cleanup: %s" msg
        | "2" ->
            let! clearResult = clearAllCache config
            match clearResult with
            | Ok _ -> printfn "🧹 All cache cleared"
            | Error msg -> printfn "❌ Error clearing cache: %s" msg
        | _ -> ()
        
        printfn ""
        printfn "✨ Demo completed successfully!"
        printfn ""
        printfn "🔧 Configuration System Features Demonstrated:"
        printfn "   • Automatic configuration loading and saving"
        printfn "   • User preference management"
        printfn "   • Intelligent caching with cleanup"
        printfn "   • Directory initialization"
        printfn "   • Configuration validation and warnings"
        printfn ""
        printfn "💡 Next Steps:"
        printfn "   • Configure API keys for real weather data"
        printfn "   • Customize user preferences"
        printfn "   • Plan actual trips with personalized settings"
        printfn ""
        
        // Save configuration and profile
        let! _ = saveConfig config
        let! _ = saveUserProfile profile
        printfn "💾 Configuration and profile saved"
        
        return 0
    with
    | ex ->
        printfn "❌ Application error: %s" ex.Message
        return 1
}

[<EntryPoint>]
let main argv =
    try
        let result = runBasicDemo() |> Async.RunSynchronously
        result
    with
    | ex ->
        printfn "💥 Fatal error: %s" ex.Message
        1
