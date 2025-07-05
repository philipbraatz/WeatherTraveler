module WeatherTraveler.PreferencesService

open System
open System.IO
open Newtonsoft.Json
open WeatherTraveler.Types
open WeatherTraveler.Configuration

/// Travel history entry
type TravelHistoryEntry = {
    Id: Guid
    From: TravelLocation
    To: TravelLocation  
    TravelDate: DateTime
    ActualWeather: WeatherInfo list
    UserRating: int // 1-5 stars
    Notes: string option
    TotalDistance: float
    TotalCost: float
    CreatedAt: DateTime
}

/// User travel patterns and analytics
type TravelPatterns = {
    FavoriteDestinations: (string * int) list // Destination name, visit count
    PreferredTravelMonths: int list // Month numbers (1-12)
    AverageTemperaturePreference: float
    MostCommonTravelDistance: float
    WeatherAccuracyFeedback: float // Average accuracy rating from user
}

/// Saved route template
type RouteTemplate = {
    Id: Guid
    Name: string
    Description: string option
    Locations: TravelLocation list
    PreferredSettings: UserPreferences
    CreatedAt: DateTime
    LastUsed: DateTime
    UseCount: int
}

/// User profile with comprehensive preferences
type UserProfile = {
    UserId: Guid
    DisplayName: string
    Email: string option
    CreatedAt: DateTime
    LastActiveAt: DateTime
    Preferences: UserPreferences
    TravelHistory: TravelHistoryEntry list
    SavedRoutes: RouteTemplate list
    TravelPatterns: TravelPatterns option
    NotificationSettings: NotificationSettings
}

/// Notification preferences
and NotificationSettings = {
    WeatherAlerts: bool
    PriceAlerts: bool
    RouteUpdates: bool
    DailyWeatherDigest: bool
    EmailNotifications: bool
    PushNotifications: bool
}

/// Get user profile file path
let private getUserProfilePath() =
    let appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)
    let profileDir = Path.Combine(appDataPath, "WeatherTraveler", "Profiles")
    if not (Directory.Exists(profileDir)) then
        Directory.CreateDirectory(profileDir) |> ignore
    Path.Combine(profileDir, "user_profile.json")

/// Default notification settings
let private defaultNotificationSettings = {
    WeatherAlerts = true
    PriceAlerts = false
    RouteUpdates = true
    DailyWeatherDigest = false
    EmailNotifications = false
    PushNotifications = true
}

/// Default user preferences
let private defaultUserPreferences = {
    PreferredTemperatureRange = { MinCelsius = 15.0; MaxCelsius = 25.0 }
    AvoidRainByDefault = true
    PreferredDepartureTime = TimeSpan.FromHours(8.0) // 8:00 AM
    MaxDrivingHoursPerDay = 8
    PreferredFuelType = "regular"
    PreferredGasStationBrands = ["Shell"; "Exxon"]
    WeatherPriorityWeight = 0.4
    CostPriorityWeight = 0.3
    TimePriorityWeight = 0.3
}

/// Create default user profile
let createDefaultProfile() = {
    UserId = Guid.NewGuid()
    DisplayName = Environment.UserName
    Email = None
    CreatedAt = DateTime.UtcNow
    LastActiveAt = DateTime.UtcNow
    Preferences = defaultUserPreferences
    TravelHistory = []
    SavedRoutes = []
    TravelPatterns = None
    NotificationSettings = defaultNotificationSettings
}

/// Load user profile
let loadUserProfile() = async {
    try
        let profilePath = getUserProfilePath()
        if File.Exists(profilePath) then
            let json = File.ReadAllText(profilePath)
            let profile = JsonConvert.DeserializeObject<UserProfile>(json)
            let updatedProfile = { profile with LastActiveAt = DateTime.UtcNow }
            printfn "👤 User profile loaded for: %s" profile.DisplayName
            return updatedProfile
        else
            let defaultProfile = createDefaultProfile()
            printfn "👤 Created new user profile for: %s" defaultProfile.DisplayName
            return defaultProfile
    with
    | ex ->
        printfn "⚠️  Error loading user profile: %s" ex.Message
        printfn "👤 Using default profile"
        return createDefaultProfile()
}

/// Save user profile
let saveUserProfile (profile: UserProfile) = async {
    try
        let profilePath = getUserProfilePath()
        let updatedProfile = { profile with LastActiveAt = DateTime.UtcNow }
        let json = JsonConvert.SerializeObject(updatedProfile, Formatting.Indented)
        File.WriteAllText(profilePath, json)
        printfn "💾 User profile saved"
        return Ok updatedProfile
    with
    | ex ->
        printfn "❌ Error saving user profile: %s" ex.Message
        return Error ex.Message
}

/// Update user preferences
let updatePreferences (profile: UserProfile) (newPreferences: UserPreferences) = async {
    let updatedProfile = { profile with Preferences = newPreferences }
    let! result = saveUserProfile updatedProfile
    match result with
    | Ok profile -> return Ok profile
    | Error msg -> return Error msg
}

/// Add travel history entry
let addTravelHistory (profile: UserProfile) (historyEntry: TravelHistoryEntry) = async {
    let updatedHistory = historyEntry :: profile.TravelHistory
    let updatedProfile = { profile with TravelHistory = updatedHistory }
    let! result = saveUserProfile updatedProfile
    match result with
    | Ok profile -> 
        printfn "📝 Added travel history entry: %s to %s" historyEntry.From.Name historyEntry.To.Name
        return Ok profile
    | Error msg -> return Error msg
}

/// Save route template
let saveRouteTemplate (profile: UserProfile) (template: RouteTemplate) = async {
    let existingTemplates = profile.SavedRoutes |> List.filter (fun t -> t.Id <> template.Id)
    let updatedTemplates = template :: existingTemplates
    let updatedProfile = { profile with SavedRoutes = updatedTemplates }
    let! result = saveUserProfile updatedProfile
    match result with
    | Ok profile ->
        printfn "💾 Saved route template: %s" template.Name
        return Ok profile
    | Error msg -> return Error msg
}

/// Get route template by ID
let getRouteTemplate (profile: UserProfile) (templateId: Guid) =
    profile.SavedRoutes |> List.tryFind (fun t -> t.Id = templateId)

/// Delete route template
let deleteRouteTemplate (profile: UserProfile) (templateId: Guid) = async {
    let updatedTemplates = profile.SavedRoutes |> List.filter (fun t -> t.Id <> templateId)
    let updatedProfile = { profile with SavedRoutes = updatedTemplates }
    let! result = saveUserProfile updatedProfile
    match result with
    | Ok profile ->
        printfn "🗑️  Deleted route template"
        return Ok profile
    | Error msg -> return Error msg
}

/// Analyze travel patterns
let analyzeTravelPatterns (profile: UserProfile) =
    if profile.TravelHistory.IsEmpty then
        None
    else
        let history = profile.TravelHistory
        
        // Favorite destinations
        let destinations = 
            history 
            |> List.collect (fun h -> [h.From.Name; h.To.Name])
            |> List.groupBy id
            |> List.map (fun (dest, occurrences) -> (dest, occurrences.Length))
            |> List.sortByDescending snd
            |> List.take (min 10 history.Length)
        
        // Preferred travel months
        let months = 
            history
            |> List.map (fun h -> h.TravelDate.Month)
            |> List.groupBy id
            |> List.sortByDescending (fun (_, occurrences) -> occurrences.Length)
            |> List.map fst
        
        // Average temperature preference
        let avgTemp = 
            history
            |> List.collect (fun h -> h.ActualWeather)
            |> List.map (fun w -> w.Temperature)
            |> List.average
        
        // Most common travel distance
        let avgDistance = 
            history
            |> List.map (fun h -> h.TotalDistance)
            |> List.average
        
        // Weather accuracy feedback (if user rated weather predictions)
        let avgAccuracy = 
            history
            |> List.map (fun h -> float h.UserRating)
            |> List.average
        
        Some {
            FavoriteDestinations = destinations
            PreferredTravelMonths = months |> List.take (min 6 months.Length)
            AverageTemperaturePreference = avgTemp
            MostCommonTravelDistance = avgDistance
            WeatherAccuracyFeedback = avgAccuracy
        }

/// Update travel patterns in profile
let updateTravelPatterns (profile: UserProfile) = async {
    let patterns = analyzeTravelPatterns profile
    let updatedProfile = { profile with TravelPatterns = patterns }
    let! result = saveUserProfile updatedProfile
    match result with
    | Ok profile -> 
        printfn "📊 Updated travel patterns analysis"
        return Ok profile
    | Error msg -> return Error msg
}

/// Get personalized recommendations based on patterns
let getPersonalizedRecommendations (profile: UserProfile) =
    match profile.TravelPatterns with
    | Some patterns ->
        let recommendations = []
        
        // Temperature recommendations
        let tempRecommendations = 
            if abs(patterns.AverageTemperaturePreference - profile.Preferences.PreferredTemperatureRange.MinCelsius) > 5.0 then
                [sprintf "Consider adjusting your temperature preference to %.1f°C based on your travel history" patterns.AverageTemperaturePreference]
            else []
        
        // Seasonal recommendations
        let seasonalRecommendations = 
            match patterns.PreferredTravelMonths with
            | month :: _ when month >= 6 && month <= 8 ->
                ["You tend to travel in summer - consider spring or fall for better weather and prices"]
            | month :: _ when month >= 12 || month <= 2 ->
                ["You tend to travel in winter - consider destinations with warmer climates"]
            | _ -> []
        
        // Destination recommendations
        let destRecommendations = 
            match patterns.FavoriteDestinations with
            | (favDest, _) :: _ ->
                [sprintf "Based on your visits to %s, you might enjoy similar destinations" favDest]
            | _ -> []
        
        tempRecommendations @ seasonalRecommendations @ destRecommendations
    | None ->
        ["Travel more to get personalized recommendations based on your patterns!"]

/// Export user data for backup
let exportUserData (profile: UserProfile) (filePath: string) = async {
    try
        // Create anonymized export (remove sensitive info)
        let exportData = {
            profile with
                Email = None
                UserId = Guid.Empty
        }
        
        let json = JsonConvert.SerializeObject(exportData, Formatting.Indented)
        File.WriteAllText(filePath, json)
        printfn "📤 User data exported to: %s" filePath
        return Ok ()
    with
    | ex ->
        printfn "❌ Error exporting user data: %s" ex.Message
        return Error ex.Message
}

/// Import user data from backup
let importUserData (filePath: string) = async {
    try
        if File.Exists(filePath) then
            let json = File.ReadAllText(filePath)
            let importedProfile = JsonConvert.DeserializeObject<UserProfile>(json)
            
            // Assign new user ID and timestamps
            let profile = {
                importedProfile with
                    UserId = Guid.NewGuid()
                    LastActiveAt = DateTime.UtcNow
                    CreatedAt = DateTime.UtcNow
            }
            
            let! result = saveUserProfile profile
            match result with
            | Ok profile ->
                printfn "📥 User data imported successfully"
                return Ok profile
            | Error msg -> return Error msg
        else
            return Error "Import file not found"
    with
    | ex ->
        printfn "❌ Error importing user data: %s" ex.Message
        return Error ex.Message
}

/// Print user profile summary
let printProfileSummary (profile: UserProfile) =
    printfn ""
    printfn "👤 User Profile Summary:"
    printfn "   Name: %s" profile.DisplayName
    printfn "   Member Since: %s" (profile.CreatedAt.ToString("yyyy-MM-dd"))
    printfn "   Last Active: %s" (profile.LastActiveAt.ToString("yyyy-MM-dd HH:mm"))
    printfn "   Travel History: %d trips" profile.TravelHistory.Length
    printfn "   Saved Routes: %d templates" profile.SavedRoutes.Length
    printfn ""
    
    printfn "🎯 Preferences:"
    printfn "   Temperature Range: %.1f°C - %.1f°C" 
        profile.Preferences.PreferredTemperatureRange.MinCelsius 
        profile.Preferences.PreferredTemperatureRange.MaxCelsius
    printfn "   Avoid Rain: %b" profile.Preferences.AvoidRainByDefault
    printfn "   Max Driving Hours: %d/day" profile.Preferences.MaxDrivingHoursPerDay
    printfn "   Preferred Departure: %s" (profile.Preferences.PreferredDepartureTime.ToString(@"hh\:mm"))
    printfn ""
    
    match profile.TravelPatterns with
    | Some patterns ->
        printfn "📊 Travel Patterns:"
        printfn "   Favorite Destinations:"
        patterns.FavoriteDestinations
        |> List.take (min 3 patterns.FavoriteDestinations.Length)
        |> List.iter (fun (dest, count) -> printfn "     • %s (%d visits)" dest count)
        
        printfn "   Preferred Months: %s" 
            (patterns.PreferredTravelMonths 
             |> List.map (fun m -> System.Globalization.CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(m))
             |> String.concat ", ")
        
        printfn "   Average Temperature Preference: %.1f°C" patterns.AverageTemperaturePreference
        printfn "   Average Trip Distance: %.1f km" patterns.MostCommonTravelDistance
        printfn ""
    | None ->
        printfn "📊 No travel patterns yet - take some trips to see analytics!"
        printfn ""
    
    printfn "🔔 Notifications:"
    printfn "   Weather Alerts: %b" profile.NotificationSettings.WeatherAlerts
    printfn "   Price Alerts: %b" profile.NotificationSettings.PriceAlerts
    printfn "   Route Updates: %b" profile.NotificationSettings.RouteUpdates
    printfn ""

/// Create route template from current preferences
let createRouteTemplateFromTrip (profile: UserProfile) (name: string) (description: string option) (locations: TravelLocation list) =
    {
        Id = Guid.NewGuid()
        Name = name
        Description = description
        Locations = locations
        PreferredSettings = profile.Preferences
        CreatedAt = DateTime.UtcNow
        LastUsed = DateTime.UtcNow
        UseCount = 0
    }

/// Preference modification options
type PreferenceOption =
    | TemperatureRange
    | AvoidRain
    | DepartureTime
    | MaxDrivingHours
    | FuelType
    | GasStationBrands
    | PriorityWeights
    | NotificationSettings
    | ResetToDefaults

/// Display preference management menu
let displayPreferenceMenu() =
    printfn ""
    printfn "⚙️  User Preference Management"
    printfn "╔════════════════════════════════════════════════════════════════╗"
    printfn "║                     Select Preference to Modify               ║"
    printfn "╠════════════════════════════════════════════════════════════════╣"
    printfn "║  1. 🌡️  Temperature Range (Min/Max °C)                        ║"
    printfn "║  2. 🌧️  Rain Avoidance Preference                             ║"
    printfn "║  3. ⏰ Preferred Departure Time                                ║"
    printfn "║  4. 🚗 Maximum Daily Driving Hours                            ║"
    printfn "║  5. ⛽ Fuel Type & Gas Station Preferences                     ║"
    printfn "║  6. ⚖️  Priority Weights (Weather/Cost/Time)                   ║"
    printfn "║  7. 🔔 Notification Settings                                   ║"
    printfn "║  8. 🔄 Reset All Preferences to Defaults                      ║"
    printfn "║  9. 📊 View Current Preferences                               ║"
    printfn "║  0. ← Back to Main Menu                                        ║"
    printfn "╚════════════════════════════════════════════════════════════════╝"
    printfn "Enter your choice (0-9): "

/// Get user input with validation
let getUserInput (prompt: string) (validator: string -> bool) (errorMessage: string) =
    let rec loop() =
        printf "%s" prompt
        let input = Console.ReadLine()
        if validator input then
            input
        else
            printfn "%s" errorMessage
            loop()
    loop()

/// Get float input with validation
let getFloatInput (prompt: string) (min: float option) (max: float option) =
    let validator (input: string) =
        match System.Double.TryParse(input) with
        | true, value ->
            match min, max with
            | Some minVal, Some maxVal -> value >= minVal && value <= maxVal
            | Some minVal, None -> value >= minVal
            | None, Some maxVal -> value <= maxVal
            | None, None -> true
        | false, _ -> false
    
    let errorMsg = 
        match min, max with
        | Some minVal, Some maxVal -> sprintf "Please enter a number between %.1f and %.1f" minVal maxVal
        | Some minVal, None -> sprintf "Please enter a number >= %.1f" minVal
        | None, Some maxVal -> sprintf "Please enter a number <= %.1f" maxVal
        | None, None -> "Please enter a valid number"
    
    let input = getUserInput prompt validator errorMsg
    System.Double.Parse(input)

/// Get integer input with validation
let getIntInput (prompt: string) (min: int option) (max: int option) =
    let validator (input: string) =
        match System.Int32.TryParse(input) with
        | true, value ->
            match min, max with
            | Some minVal, Some maxVal -> value >= minVal && value <= maxVal
            | Some minVal, None -> value >= minVal
            | None, Some maxVal -> value <= maxVal
            | None, None -> true
        | false, _ -> false
    
    let errorMsg = 
        match min, max with
        | Some minVal, Some maxVal -> sprintf "Please enter a number between %d and %d" minVal maxVal
        | Some minVal, None -> sprintf "Please enter a number >= %d" minVal
        | None, Some maxVal -> sprintf "Please enter a number <= %d" maxVal
        | None, None -> "Please enter a valid number"
    
    let input = getUserInput prompt validator errorMsg
    System.Int32.Parse(input)

/// Get boolean input
let getBoolInput (prompt: string) =
    let validator (input: string) =
        match input.ToLower() with
        | "y" | "yes" | "true" | "1" | "n" | "no" | "false" | "0" -> true
        | _ -> false
    
    let input = getUserInput prompt validator "Please enter 'y' for yes or 'n' for no"
    match input.ToLower() with
    | "y" | "yes" | "true" | "1" -> true
    | _ -> false

/// Update temperature range preference
let updateTemperatureRange (preferences: UserPreferences) = async {
    printfn ""
    printfn "🌡️  Temperature Range Configuration"
    printfn "Current range: %.1f°C to %.1f°C" 
        preferences.PreferredTemperatureRange.MinCelsius 
        preferences.PreferredTemperatureRange.MaxCelsius
    printfn ""
    
    let minTemp = getFloatInput "Enter minimum temperature (°C): " (Some -40.0) (Some 50.0)
    let maxTemp = getFloatInput "Enter maximum temperature (°C): " (Some minTemp) (Some 60.0)
    
    let newRange = { MinCelsius = minTemp; MaxCelsius = maxTemp }
    let updatedPreferences = { preferences with PreferredTemperatureRange = newRange }
    
    printfn "✅ Temperature range updated to %.1f°C - %.1f°C" minTemp maxTemp
    return updatedPreferences
}

/// Update rain avoidance preference
let updateRainAvoidance (preferences: UserPreferences) = async {
    printfn ""
    printfn "🌧️  Rain Avoidance Configuration"
    printfn "Current setting: %s" (if preferences.AvoidRainByDefault then "Avoid rain" else "Don't mind rain")
    printfn ""
    
    let avoidRain = getBoolInput "Do you want to avoid rain by default? (y/n): "
    let updatedPreferences = { preferences with AvoidRainByDefault = avoidRain }
    
    printfn "✅ Rain preference updated to: %s" (if avoidRain then "Avoid rain" else "Don't mind rain")
    return updatedPreferences
}

/// Update departure time preference
let updateDepartureTime (preferences: UserPreferences) = async {
    printfn ""
    printfn "⏰ Departure Time Configuration"
    printfn "Current preferred departure time: %s" (preferences.PreferredDepartureTime.ToString(@"hh\:mm"))
    printfn ""
    
    let hour = getIntInput "Enter preferred departure hour (0-23): " (Some 0) (Some 23)
    let minute = getIntInput "Enter preferred departure minute (0-59): " (Some 0) (Some 59)
    
    let newDepartureTime = TimeSpan(hour, minute, 0)
    let updatedPreferences = { preferences with PreferredDepartureTime = newDepartureTime }
    
    printfn "✅ Departure time updated to %02d:%02d" hour minute
    return updatedPreferences
}

/// Update maximum driving hours preference
let updateMaxDrivingHours (preferences: UserPreferences) = async {
    printfn ""
    printfn "🚗 Maximum Daily Driving Hours Configuration"
    printfn "Current limit: %d hours per day" preferences.MaxDrivingHoursPerDay
    printfn ""
    
    let maxHours = getIntInput "Enter maximum driving hours per day (1-16): " (Some 1) (Some 16)
    let updatedPreferences = { preferences with MaxDrivingHoursPerDay = maxHours }
    
    printfn "✅ Maximum driving hours updated to %d hours per day" maxHours
    return updatedPreferences
}

/// Update fuel and gas station preferences
let updateFuelPreferences (preferences: UserPreferences) = async {
    printfn ""
    printfn "⛽ Fuel and Gas Station Preferences"
    printfn "Current fuel type: %s" preferences.PreferredFuelType
    printfn "Current preferred brands: %s" (String.concat ", " preferences.PreferredGasStationBrands)
    printfn ""
    
    printfn "Available fuel types:"
    printfn "1. regular"
    printfn "2. premium"
    printfn "3. diesel"
    printfn "4. electric"
    
    let fuelChoice = getIntInput "Select fuel type (1-4): " (Some 1) (Some 4)
    let fuelType = 
        match fuelChoice with
        | 1 -> "regular"
        | 2 -> "premium"
        | 3 -> "diesel"
        | 4 -> "electric"
        | _ -> "regular"
    
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
    
    let updatedPreferences = { 
        preferences with 
            PreferredFuelType = fuelType
            PreferredGasStationBrands = brands 
    }
    
    printfn "✅ Fuel preferences updated:"
    printfn "   Type: %s" fuelType
    printfn "   Brands: %s" (String.concat ", " brands)
    return updatedPreferences
}

/// Update priority weights
let updatePriorityWeights (preferences: UserPreferences) = async {
    printfn ""
    printfn "⚖️  Priority Weights Configuration"
    printfn "Current weights:"
    printfn "   Weather Priority: %.1f" preferences.WeatherPriorityWeight
    printfn "   Cost Priority: %.1f" preferences.CostPriorityWeight
    printfn "   Time Priority: %.1f" preferences.TimePriorityWeight
    printfn "   Total: %.1f (should equal 1.0)" (preferences.WeatherPriorityWeight + preferences.CostPriorityWeight + preferences.TimePriorityWeight)
    printfn ""
    printfn "Enter weights (0.0 - 1.0, must sum to 1.0):"
    
    let weatherWeight = getFloatInput "Weather priority weight (0.0 - 1.0): " (Some 0.0) (Some 1.0)
    let remainingWeight = 1.0 - weatherWeight
    
    printfn "Remaining weight to distribute: %.1f" remainingWeight
    let costWeight = getFloatInput (sprintf "Cost priority weight (0.0 - %.1f): " remainingWeight) (Some 0.0) (Some remainingWeight)
    let timeWeight = remainingWeight - costWeight
    
    if timeWeight < 0.0 then
        printfn "❌ Invalid weights. Please ensure they sum to 1.0"
        return preferences
    else
        let updatedPreferences = { 
            preferences with 
                WeatherPriorityWeight = weatherWeight
                CostPriorityWeight = costWeight
                TimePriorityWeight = timeWeight
        }
        
        printfn "✅ Priority weights updated:"
        printfn "   Weather: %.1f" weatherWeight
        printfn "   Cost: %.1f" costWeight
        printfn "   Time: %.1f" timeWeight
        return updatedPreferences
}

/// Update notification settings
let updateNotificationSettings (profile: UserProfile) = async {
    printfn ""
    printfn "🔔 Notification Settings Configuration"
    printfn "Current settings:"
    printfn "   Weather Alerts: %b" profile.NotificationSettings.WeatherAlerts
    printfn "   Price Alerts: %b" profile.NotificationSettings.PriceAlerts
    printfn "   Route Updates: %b" profile.NotificationSettings.RouteUpdates
    printfn "   Daily Weather Digest: %b" profile.NotificationSettings.DailyWeatherDigest
    printfn "   Email Notifications: %b" profile.NotificationSettings.EmailNotifications
    printfn "   Push Notifications: %b" profile.NotificationSettings.PushNotifications
    printfn ""
    
    let weatherAlerts = getBoolInput "Enable weather alerts? (y/n): "
    let priceAlerts = getBoolInput "Enable gas price alerts? (y/n): "
    let routeUpdates = getBoolInput "Enable route update notifications? (y/n): "
    let dailyDigest = getBoolInput "Enable daily weather digest? (y/n): "
    let emailNotifications = getBoolInput "Enable email notifications? (y/n): "
    let pushNotifications = getBoolInput "Enable push notifications? (y/n): "
    
    let newNotificationSettings = {
        WeatherAlerts = weatherAlerts
        PriceAlerts = priceAlerts
        RouteUpdates = routeUpdates
        DailyWeatherDigest = dailyDigest
        EmailNotifications = emailNotifications
        PushNotifications = pushNotifications
    }
    
    let updatedProfile = { profile with NotificationSettings = newNotificationSettings }
    
    printfn "✅ Notification settings updated successfully"
    return updatedProfile
}

/// Reset preferences to defaults
let resetPreferencesToDefaults (profile: UserProfile) = async {
    printfn ""
    printfn "🔄 Reset Preferences to Defaults"
    printfn "⚠️  This will reset ALL preferences to default values."
    
    let confirm = getBoolInput "Are you sure you want to reset all preferences? (y/n): "
    
    if confirm then
        let updatedProfile = { 
            profile with 
                Preferences = defaultUserPreferences
                NotificationSettings = defaultNotificationSettings
        }
        printfn "✅ All preferences have been reset to defaults"
        return updatedProfile
    else
        printfn "❌ Reset cancelled"
        return profile
}

/// Interactive preference management system (simplified - use individual functions)
let manageUserPreferences (profile: UserProfile) = async {
    displayPreferenceMenu()
    printfn "Use the individual update functions for preference management:"
    printfn "- quickUpdateTemperatureRange"
    printfn "- quickToggleRainAvoidance" 
    printfn "- quickUpdateDepartureTime"
    printfn "Full interactive menu available but requires manual integration."
    return profile
}

/// Quick preference update functions for specific common operations
let quickUpdateTemperatureRange (profile: UserProfile) (minTemp: float) (maxTemp: float) = async {
    let newRange = { MinCelsius = minTemp; MaxCelsius = maxTemp }
    let newPreferences = { profile.Preferences with PreferredTemperatureRange = newRange }
    let updatedProfile = { profile with Preferences = newPreferences }
    return! saveUserProfile updatedProfile
}

let quickToggleRainAvoidance (profile: UserProfile) = async {
    let newPreferences = { profile.Preferences with AvoidRainByDefault = not profile.Preferences.AvoidRainByDefault }
    let updatedProfile = { profile with Preferences = newPreferences }
    return! saveUserProfile updatedProfile
}

let quickUpdateDepartureTime (profile: UserProfile) (hour: int) (minute: int) = async {
    let newDepartureTime = TimeSpan(hour, minute, 0)
    let newPreferences = { profile.Preferences with PreferredDepartureTime = newDepartureTime }
    let updatedProfile = { profile with Preferences = newPreferences }
    return! saveUserProfile updatedProfile
}

/// Delete specific preference categories (reset to default)
let deleteTemperatureRange (profile: UserProfile) = async {
    let defaultRange = { MinCelsius = 15.0; MaxCelsius = 25.0 }
    let newPreferences = { profile.Preferences with PreferredTemperatureRange = defaultRange }
    let updatedProfile = { profile with Preferences = newPreferences }
    printfn "🗑️  Temperature range reset to default (15°C - 25°C)"
    return! saveUserProfile updatedProfile
}

let deleteGasStationPreferences (profile: UserProfile) = async {
    let newPreferences = { 
        profile.Preferences with 
            PreferredFuelType = "regular"
            PreferredGasStationBrands = ["Shell"; "Exxon"]
    }
    let updatedProfile = { profile with Preferences = newPreferences }
    printfn "🗑️  Gas station preferences reset to defaults"
    return! saveUserProfile updatedProfile
}

let deletePriorityWeights (profile: UserProfile) = async {
    let newPreferences = { 
        profile.Preferences with 
            WeatherPriorityWeight = 0.4
            CostPriorityWeight = 0.3
            TimePriorityWeight = 0.3
    }
    let updatedProfile = { profile with Preferences = newPreferences }
    printfn "🗑️  Priority weights reset to defaults (Weather: 0.4, Cost: 0.3, Time: 0.3)"
    return! saveUserProfile updatedProfile
}

/// Preference validation functions
let validatePreferences (preferences: UserPreferences) =
    let errors = []
    
    // Temperature range validation
    let tempErrors = 
        if preferences.PreferredTemperatureRange.MinCelsius >= preferences.PreferredTemperatureRange.MaxCelsius then
            ["Temperature minimum must be less than maximum"]
        else []
    
    // Priority weights validation
    let prioritySum = preferences.WeatherPriorityWeight + preferences.CostPriorityWeight + preferences.TimePriorityWeight
    let priorityErrors = 
        if abs(prioritySum - 1.0) > 0.01 then
            [sprintf "Priority weights must sum to 1.0 (current sum: %.2f)" prioritySum]
        else []
    
    // Driving hours validation
    let drivingErrors = 
        if preferences.MaxDrivingHoursPerDay < 1 || preferences.MaxDrivingHoursPerDay > 16 then
            ["Maximum driving hours must be between 1 and 16"]
        else []
    
    tempErrors @ priorityErrors @ drivingErrors

/// Get preference summary as string
let getPreferenceSummary (profile: UserProfile) =
    sprintf """
🎯 User Preferences Summary:
   Temperature Range: %.1f°C - %.1f°C
   Avoid Rain: %b
   Max Driving Hours: %d/day
   Preferred Departure: %s
   Fuel Type: %s
   Priority Weights: Weather %.1f, Cost %.1f, Time %.1f
""" 
        profile.Preferences.PreferredTemperatureRange.MinCelsius 
        profile.Preferences.PreferredTemperatureRange.MaxCelsius
        profile.Preferences.AvoidRainByDefault
        profile.Preferences.MaxDrivingHoursPerDay
        (profile.Preferences.PreferredDepartureTime.ToString(@"hh\:mm"))
        profile.Preferences.PreferredFuelType
        profile.Preferences.WeatherPriorityWeight
        profile.Preferences.CostPriorityWeight
        profile.Preferences.TimePriorityWeight
