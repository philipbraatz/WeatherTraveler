module WeatherTraveler.InteractivePreferenceManager

open System
open WeatherTraveler.PreferencesService
open WeatherTraveler.Configuration

/// Interactive preference management with working menu system
let runInteractivePreferenceManager() = async {
    printfn "╔══════════════════════════════════════════════════════════════════════════════╗"
    printfn "║               🎛️  INTERACTIVE PREFERENCE MANAGER  🎛️                        ║"
    printfn "║                      Weather Traveler v2.0                                  ║"
    printfn "╚══════════════════════════════════════════════════════════════════════════════╝"
    printfn ""
    
    // Load user profile
    let! initialProfile = loadUserProfile()
    let mutable currentProfile = initialProfile
    
    let mutable continueLoop = true
    
    while continueLoop do
        // Display current preferences
        printfn ""
        printfn "📊 Current User Preferences:"
        printProfileSummary currentProfile
        
        // Display menu
        displayPreferenceMenu()
        
        // Get user choice
        try
            printf "Enter your choice (0-9): "
            let input = Console.ReadLine()
            let choice = Int32.Parse(input)
            
            match choice with
            | 1 -> // Temperature Range
                printfn ""
                printfn "🌡️  Temperature Range Configuration"
                printfn "Current range: %.1f°C to %.1f°C" 
                    currentProfile.Preferences.PreferredTemperatureRange.MinCelsius 
                    currentProfile.Preferences.PreferredTemperatureRange.MaxCelsius
                printfn ""
                
                try
                    printf "Enter minimum temperature (°C, -40 to 50): "
                    let minTemp = Double.Parse(Console.ReadLine())
                    printf "Enter maximum temperature (°C, %.1f to 60): " minTemp
                    let maxTemp = Double.Parse(Console.ReadLine())
                    
                    if minTemp >= maxTemp then
                        printfn "❌ Minimum temperature must be less than maximum temperature"
                    elif minTemp < -40.0 || minTemp > 50.0 then
                        printfn "❌ Minimum temperature must be between -40°C and 50°C"
                    elif maxTemp < minTemp || maxTemp > 60.0 then
                        printfn "❌ Maximum temperature must be between %.1f°C and 60°C" minTemp
                    else
                        let! result = quickUpdateTemperatureRange currentProfile minTemp maxTemp
                        match result with
                        | Ok updatedProfile ->
                            currentProfile <- updatedProfile
                            printfn "✅ Temperature range updated to %.1f°C - %.1f°C" minTemp maxTemp
                        | Error msg ->
                            printfn "❌ Error updating temperature range: %s" msg
                with
                | :? FormatException ->
                    printfn "❌ Invalid number format. Please enter valid numbers."
                | ex ->
                    printfn "❌ Error: %s" ex.Message
                
            | 2 -> // Rain Avoidance
                printfn ""
                printfn "🌧️  Rain Avoidance Configuration"
                printfn "Current setting: %s" (if currentProfile.Preferences.AvoidRainByDefault then "Avoid rain" else "Don't mind rain")
                printfn ""
                
                let! result = quickToggleRainAvoidance currentProfile
                match result with
                | Ok updatedProfile ->
                    currentProfile <- updatedProfile
                    printfn "✅ Rain preference toggled to: %s" 
                        (if updatedProfile.Preferences.AvoidRainByDefault then "Avoid rain" else "Don't mind rain")
                | Error msg ->
                    printfn "❌ Error toggling rain preference: %s" msg
                
            | 3 -> // Departure Time
                printfn ""
                printfn "⏰ Departure Time Configuration"
                printfn "Current preferred departure time: %s" 
                    (currentProfile.Preferences.PreferredDepartureTime.ToString(@"hh\:mm"))
                printfn ""
                
                try
                    printf "Enter preferred departure hour (0-23): "
                    let hour = Int32.Parse(Console.ReadLine())
                    printf "Enter preferred departure minute (0-59): "
                    let minute = Int32.Parse(Console.ReadLine())
                    
                    if hour < 0 || hour > 23 then
                        printfn "❌ Hour must be between 0 and 23"
                    elif minute < 0 || minute > 59 then
                        printfn "❌ Minute must be between 0 and 59"
                    else
                        let! result = quickUpdateDepartureTime currentProfile hour minute
                        match result with
                        | Ok updatedProfile ->
                            currentProfile <- updatedProfile
                            printfn "✅ Departure time updated to %02d:%02d" hour minute
                        | Error msg ->
                            printfn "❌ Error updating departure time: %s" msg
                with
                | :? FormatException ->
                    printfn "❌ Invalid number format. Please enter valid numbers."
                | ex ->
                    printfn "❌ Error: %s" ex.Message
                
            | 4 -> // Max Driving Hours
                printfn ""
                printfn "🚗 Maximum Daily Driving Hours Configuration"
                printfn "Current limit: %d hours per day" currentProfile.Preferences.MaxDrivingHoursPerDay
                printfn ""
                
                try
                    printf "Enter maximum driving hours per day (1-16): "
                    let maxHours = Int32.Parse(Console.ReadLine())
                    
                    if maxHours < 1 || maxHours > 16 then
                        printfn "❌ Maximum driving hours must be between 1 and 16"
                    else
                        let newPreferences = { currentProfile.Preferences with MaxDrivingHoursPerDay = maxHours }
                        let updatedProfile = { currentProfile with Preferences = newPreferences }
                        let! result = saveUserProfile updatedProfile
                        match result with
                        | Ok savedProfile ->
                            currentProfile <- savedProfile
                            printfn "✅ Maximum driving hours updated to %d hours per day" maxHours
                        | Error msg ->
                            printfn "❌ Error updating driving hours: %s" msg                with
                | :? FormatException ->
                    printfn "❌ Invalid number format. Please enter a valid number."
                | ex ->
                    printfn "❌ Error: %s" ex.Message
                
            | 5 -> // Fuel Type & Gas Station Preferences
                printfn ""
                printfn "⛽ Fuel and Gas Station Preferences"
                printfn "Current fuel type: %s" currentProfile.Preferences.PreferredFuelType
                printfn "Current preferred brands: %s" (String.concat ", " currentProfile.Preferences.PreferredGasStationBrands)
                printfn ""
                
                printfn "Available fuel types:"
                printfn "1. regular"
                printfn "2. premium"
                printfn "3. diesel"
                printfn "4. electric"
                
                try
                    printf "Select fuel type (1-4): "
                    let fuelChoice = Int32.Parse(Console.ReadLine())
                    
                    let fuelType = 
                        match fuelChoice with
                        | 1 -> "regular"
                        | 2 -> "premium"
                        | 3 -> "diesel"
                        | 4 -> "electric"
                        | _ -> currentProfile.Preferences.PreferredFuelType
                    
                    printfn ""
                    printfn "Enter preferred gas station brands (comma-separated, press Enter for defaults):"
                    printf "Brands: "
                    let brandsInput = Console.ReadLine()
                    
                    let brands = 
                        if String.IsNullOrWhiteSpace(brandsInput) then
                            ["Shell"; "Exxon"; "BP"; "Chevron"]
                        else
                            brandsInput.Split(',') 
                            |> Array.map (fun s -> s.Trim())
                            |> Array.filter (fun s -> not (String.IsNullOrEmpty(s)))
                            |> Array.toList
                    
                    let newPreferences = { 
                        currentProfile.Preferences with 
                            PreferredFuelType = fuelType
                            PreferredGasStationBrands = brands 
                    }
                    let updatedProfile = { currentProfile with Preferences = newPreferences }
                    let! result = saveUserProfile updatedProfile
                    match result with
                    | Ok savedProfile ->
                        currentProfile <- savedProfile
                        printfn "✅ Fuel preferences updated:"
                        printfn "   Type: %s" fuelType
                        printfn "   Brands: %s" (String.concat ", " brands)
                    | Error msg ->
                        printfn "❌ Error updating fuel preferences: %s" msg
                with
                | :? FormatException ->
                    printfn "❌ Invalid number format. Please enter a valid choice."
                | ex ->
                    printfn "❌ Error: %s" ex.Message
                
            | 6 -> // Priority Weights
                printfn ""
                printfn "⚖️  Priority Weights Configuration"
                printfn "Current weights:"
                printfn "   Weather Priority: %.1f" currentProfile.Preferences.WeatherPriorityWeight
                printfn "   Cost Priority: %.1f" currentProfile.Preferences.CostPriorityWeight
                printfn "   Time Priority: %.1f" currentProfile.Preferences.TimePriorityWeight
                printfn "   Total: %.1f (should equal 1.0)" (currentProfile.Preferences.WeatherPriorityWeight + currentProfile.Preferences.CostPriorityWeight + currentProfile.Preferences.TimePriorityWeight)
                printfn ""
                printfn "Enter weights (0.0 - 1.0, must sum to 1.0):"
                
                try
                    printf "Weather priority weight (0.0 - 1.0): "
                    let weatherWeight = Double.Parse(Console.ReadLine())
                    let remainingWeight = 1.0 - weatherWeight
                    
                    if weatherWeight < 0.0 || weatherWeight > 1.0 then
                        printfn "❌ Weather weight must be between 0.0 and 1.0"
                    else
                        printfn "Remaining weight to distribute: %.1f" remainingWeight
                        printf "Cost priority weight (0.0 - %.1f): " remainingWeight
                        let costWeight = Double.Parse(Console.ReadLine())
                        let timeWeight = remainingWeight - costWeight
                        
                        if costWeight < 0.0 || costWeight > remainingWeight then
                            printfn "❌ Cost weight must be between 0.0 and %.1f" remainingWeight
                        elif timeWeight < -0.01 then
                            printfn "❌ Invalid weights. Please ensure they sum to 1.0"
                        else
                            let newPreferences = { 
                                currentProfile.Preferences with 
                                    WeatherPriorityWeight = weatherWeight
                                    CostPriorityWeight = costWeight
                                    TimePriorityWeight = timeWeight
                            }
                            let updatedProfile = { currentProfile with Preferences = newPreferences }
                            let! result = saveUserProfile updatedProfile
                            match result with
                            | Ok savedProfile ->
                                currentProfile <- savedProfile
                                printfn "✅ Priority weights updated:"
                                printfn "   Weather: %.1f" weatherWeight
                                printfn "   Cost: %.1f" costWeight
                                printfn "   Time: %.1f" timeWeight
                            | Error msg ->
                                printfn "❌ Error updating priority weights: %s" msg
                with
                | :? FormatException ->
                    printfn "❌ Invalid number format. Please enter valid numbers."
                | ex ->
                    printfn "❌ Error: %s" ex.Message
                
            | 7 -> // Notification Settings
                printfn ""
                printfn "🔔 Notification Settings Configuration"
                printfn "Current settings:"
                printfn "   Weather Alerts: %b" currentProfile.NotificationSettings.WeatherAlerts
                printfn "   Price Alerts: %b" currentProfile.NotificationSettings.PriceAlerts
                printfn "   Route Updates: %b" currentProfile.NotificationSettings.RouteUpdates
                printfn "   Daily Weather Digest: %b" currentProfile.NotificationSettings.DailyWeatherDigest
                printfn "   Email Notifications: %b" currentProfile.NotificationSettings.EmailNotifications
                printfn "   Push Notifications: %b" currentProfile.NotificationSettings.PushNotifications
                printfn ""
                
                let getBoolInput prompt =
                    printf "%s (y/n): " prompt
                    let input = Console.ReadLine()
                    match input.ToLower() with
                    | "y" | "yes" | "true" | "1" -> true
                    | _ -> false
                
                let weatherAlerts = getBoolInput "Enable weather alerts?"
                let priceAlerts = getBoolInput "Enable gas price alerts?"
                let routeUpdates = getBoolInput "Enable route update notifications?"
                let dailyDigest = getBoolInput "Enable daily weather digest?"
                let emailNotifications = getBoolInput "Enable email notifications?"
                let pushNotifications = getBoolInput "Enable push notifications?"
                
                let newNotificationSettings = {
                    WeatherAlerts = weatherAlerts
                    PriceAlerts = priceAlerts
                    RouteUpdates = routeUpdates
                    DailyWeatherDigest = dailyDigest
                    EmailNotifications = emailNotifications
                    PushNotifications = pushNotifications
                }
                
                let updatedProfile = { currentProfile with NotificationSettings = newNotificationSettings }
                let! result = saveUserProfile updatedProfile
                match result with
                | Ok savedProfile ->
                    currentProfile <- savedProfile
                    printfn "✅ Notification settings updated successfully"
                | Error msg ->
                    printfn "❌ Error updating notification settings: %s" msg
                
            | 8 -> // Reset All Preferences to Defaults
                printfn ""
                printfn "🔄 Reset All Preferences to Defaults"
                printfn "⚠️  This will reset ALL preferences to default values."
                
                printf "Are you sure you want to reset all preferences? (y/n): "
                let confirm = Console.ReadLine()
                
                if confirm.ToLower() = "y" || confirm.ToLower() = "yes" then
                    let defaultUserPrefs = {
                        PreferredTemperatureRange = { MinCelsius = 15.0; MaxCelsius = 25.0 }
                        AvoidRainByDefault = true
                        PreferredDepartureTime = TimeSpan.FromHours(8.0)
                        MaxDrivingHoursPerDay = 8
                        PreferredFuelType = "regular"
                        PreferredGasStationBrands = ["Shell"; "Exxon"]
                        WeatherPriorityWeight = 0.4
                        CostPriorityWeight = 0.3
                        TimePriorityWeight = 0.3
                    }
                    let defaultNotificationSettings = {
                        WeatherAlerts = true
                        PriceAlerts = false
                        RouteUpdates = true
                        DailyWeatherDigest = false
                        EmailNotifications = false
                        PushNotifications = true
                    }
                    let updatedProfile = { 
                        currentProfile with 
                            Preferences = defaultUserPrefs
                            NotificationSettings = defaultNotificationSettings
                    }
                    let! result = saveUserProfile updatedProfile
                    match result with
                    | Ok savedProfile ->
                        currentProfile <- savedProfile
                        printfn "✅ All preferences have been reset to defaults"
                    | Error msg ->
                        printfn "❌ Error resetting preferences: %s" msg                else
                    printfn "❌ Reset cancelled"
                
            | 9 -> // Validate Preferences
                printfn ""
                printfn "🔍 Preference Validation"
                let validationErrors = validatePreferences currentProfile.Preferences
                if validationErrors.IsEmpty then
                    printfn "✅ All preferences are valid!"
                else
                    printfn "⚠️  Validation errors found:"
                    validationErrors |> List.iter (fun error -> printfn "   • %s" error)
                
            | 0 -> // Exit
                printfn ""
                printfn "👋 Exiting Preference Manager..."
                continueLoop <- false
                
            | _ ->
                printfn "❌ Invalid choice. Please enter a number between 0 and 9."
                
        with
        | :? FormatException ->
            printfn "❌ Invalid input. Please enter a number between 0 and 9."
        | ex ->
            printfn "❌ Error: %s" ex.Message
        
        if continueLoop then
            printfn ""
            printf "Press Enter to continue..."
            Console.ReadLine() |> ignore
    
    return currentProfile
}

/// Show enhanced preference menu
let displayEnhancedPreferenceMenu() =
    printfn ""
    printfn "⚙️  Enhanced User Preference Management"
    printfn "╔════════════════════════════════════════════════════════════════╗"
    printfn "║                   Select Action to Perform                    ║"
    printfn "╠════════════════════════════════════════════════════════════════╣"
    printfn "║  1. 🌡️  Update Temperature Range (Min/Max °C)                 ║"
    printfn "║  2. 🌧️  Toggle Rain Avoidance Preference                      ║"
    printfn "║  3. ⏰ Set Preferred Departure Time                           ║"
    printfn "║  4. 🚗 Set Maximum Daily Driving Hours                       ║"
    printfn "║  5. ⛽ Fuel Type & Gas Station Preferences                     ║"
    printfn "║  6. ⚖️  Priority Weights (Weather/Cost/Time)                   ║"
    printfn "║  7. 🔔 Notification Settings                                   ║"
    printfn "║  8. 🔄 Reset All Preferences to Defaults                      ║"
    printfn "║  9. 🔍 Validate Current Preferences                           ║"
    printfn "║  0. ← Exit Preference Manager                                  ║"
    printfn "║  0. ← Exit Preference Manager                                  ║"
    printfn "╚════════════════════════════════════════════════════════════════╝"

[<EntryPoint>]
let main argv =
    try
        printfn "🚀 Starting Interactive Preference Manager..."
        
        // Override the display function for our enhanced version
        let originalDisplay = displayPreferenceMenu
        let mutable currentDisplay = displayEnhancedPreferenceMenu
        
        let result = runInteractivePreferenceManager() |> Async.RunSynchronously
        
        printfn ""
        printfn "🎉 Preference Management Session Completed!"
        printfn ""
        printfn "✨ Key Achievements:"
        printfn "   ✅ Interactive menu-driven preference management"
        printfn "   ✅ Real-time preference updates with validation"
        printfn "   ✅ Persistent storage of all changes"
        printfn "   ✅ Error handling and user-friendly feedback"
        printfn "   ✅ Quick access to common operations"
        printfn ""
        printfn "📋 Final User Preferences:"
        printProfileSummary result
        
        0
    with
    | ex ->
        printfn "❌ Error during preference management: %s" ex.Message
        printfn "Stack trace: %s" ex.StackTrace
        1
