module WeatherTraveler.SimplePreferenceDemo

open System
open WeatherTraveler.PreferencesService
open WeatherTraveler.Configuration

/// Simple preference update demonstration
let demonstratePreferenceManagement() = async {
    printfn "╔══════════════════════════════════════════════════════════════════════════════╗"
    printfn "║                🛠️  USER PREFERENCE MANAGEMENT DEMO  🛠️                       ║"
    printfn "║                      Weather Traveler v2.0                                  ║"
    printfn "╚══════════════════════════════════════════════════════════════════════════════╝"
    printfn ""
    
    // Load user profile
    let! profile = loadUserProfile()
    
    printfn "📋 Original User Preferences:"
    printProfileSummary profile
    
    printfn "🔧 Demonstrating Preference Updates..."
    printfn ""
    
    // Demo 1: Quick temperature range update
    printfn "1️⃣  Updating temperature range to 18°C - 28°C..."
    let! tempResult = quickUpdateTemperatureRange profile 18.0 28.0
    match tempResult with
    | Ok updatedProfile ->
        printfn "✅ Temperature range updated successfully!"
        printfn "   New range: %.1f°C - %.1f°C" 
            updatedProfile.Preferences.PreferredTemperatureRange.MinCelsius 
            updatedProfile.Preferences.PreferredTemperatureRange.MaxCelsius
        
        // Demo 2: Toggle rain avoidance
        printfn ""
        printfn "2️⃣  Toggling rain avoidance preference..."
        let! rainResult = quickToggleRainAvoidance updatedProfile
        match rainResult with
        | Ok finalProfile ->
            printfn "✅ Rain preference toggled successfully!"
            printfn "   Avoid rain: %b" finalProfile.Preferences.AvoidRainByDefault
            
            // Demo 3: Update departure time
            printfn ""
            printfn "3️⃣  Setting preferred departure time to 9:30 AM..."
            let! timeResult = quickUpdateDepartureTime finalProfile 9 30
            match timeResult with
            | Ok finalProfile2 ->
                printfn "✅ Departure time updated successfully!"
                printfn "   New departure time: %s" 
                    (finalProfile2.Preferences.PreferredDepartureTime.ToString(@"hh\:mm"))
                
                printfn ""
                printfn "📊 Final User Preferences:"
                printProfileSummary finalProfile2
                
                return finalProfile2
            | Error msg ->
                printfn "❌ Error updating departure time: %s" msg
                return finalProfile
        | Error msg ->
            printfn "❌ Error toggling rain preference: %s" msg
            return updatedProfile
    | Error msg ->
        printfn "❌ Error updating temperature range: %s" msg
        return profile
}

/// Demonstrate preference validation
let demonstratePreferenceValidation() = async {
    printfn ""
    printfn "🔍 Preference Validation Demo:"
    printfn ""
    
    let! profile = loadUserProfile()
    
    let validationErrors = validatePreferences profile.Preferences
    if validationErrors.IsEmpty then
        printfn "✅ All preferences are valid!"
    else
        printfn "⚠️  Validation errors found:"
        validationErrors |> List.iter (fun error -> printfn "   • %s" error)
    
    return profile
}

/// Demonstrate preference reset
let demonstratePreferenceReset() = async {
    printfn ""
    printfn "🔄 Preference Reset Demo:"
    printfn ""
    
    let! profile = loadUserProfile()
    
    printfn "Resetting temperature range to defaults..."
    let! result = deleteTemperatureRange profile
    match result with
    | Ok updatedProfile ->
        printfn "✅ Temperature range reset to defaults"
        return updatedProfile
    | Error msg ->
        printfn "❌ Error resetting temperature range: %s" msg
        return profile
}

/// Display available preference options
let showPreferenceOptions() =
    printfn ""
    printfn "🎛️  Available Preference Management Options:"
    printfn ""
    printfn "📏 Quick Updates:"
    printfn "   • quickUpdateTemperatureRange: Change min/max temperature"
    printfn "   • quickToggleRainAvoidance: Toggle rain avoidance setting"
    printfn "   • quickUpdateDepartureTime: Set preferred departure time"
    printfn ""
    printfn "🗑️  Preference Deletion (Reset to Defaults):"
    printfn "   • deleteTemperatureRange: Reset temperature range"
    printfn "   • deleteGasStationPreferences: Reset fuel preferences"
    printfn "   • deletePriorityWeights: Reset priority weights"
    printfn ""
    printfn "🔍 Utility Functions:"
    printfn "   • validatePreferences: Check preference validity"
    printfn "   • getPreferenceSummary: Get text summary of preferences"
    printfn "   • printProfileSummary: Display full profile summary"
    printfn ""

[<EntryPoint>]
let main argv =
    try
        printfn "🚀 Starting User Preference Management Demo..."
        printfn ""
        
        // Show available options
        showPreferenceOptions()
        
        // Run demonstration
        let result = demonstratePreferenceManagement() |> Async.RunSynchronously
        
        // Validate preferences
        let _ = demonstratePreferenceValidation() |> Async.RunSynchronously
        
        // Show reset demo
        let _ = demonstratePreferenceReset() |> Async.RunSynchronously
        
        printfn ""
        printfn "🎉 User Preference Management Demo Completed Successfully!"
        printfn ""
        printfn "💡 Key Features Demonstrated:"
        printfn "   ✅ Loading and saving user profiles"
        printfn "   ✅ Quick preference updates (temperature, rain, departure time)"
        printfn "   ✅ Preference validation"
        printfn "   ✅ Preference reset to defaults"
        printfn "   ✅ Profile summary display"
        printfn ""
        printfn "🔧 Ready for Integration:"
        printfn "   • Interactive menu system available (manageUserPreferences)"
        printfn "   • Full CRUD operations for all preference types"
        printfn "   • Persistent storage with JSON serialization"
        printfn "   • Input validation and error handling"
        printfn ""
        
        0
    with
    | ex ->
        printfn "❌ Demo failed with error: %s" ex.Message
        printfn "Stack trace: %s" ex.StackTrace
        1
