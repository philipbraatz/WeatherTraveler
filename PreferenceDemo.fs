module WeatherTraveler.PreferenceDemo

open System
open WeatherTraveler.PreferencesService
open WeatherTraveler.Configuration

let printHeader() =
    printfn "╔══════════════════════════════════════════════════════════════════════════════╗"
    printfn "║                🌤️  USER PREFERENCE MANAGEMENT DEMO  🚗                      ║"
    printfn "║                      Weather Traveler v2.0                                  ║"
    printfn "╚══════════════════════════════════════════════════════════════════════════════╝"

let runPreferenceDemo() = async {
    printHeader()
    
    // Load configuration and user profile
    let! config = loadConfig()
    let! profile = loadUserProfile()
    
    printfn "🎉 Welcome to the Preference Management System!"
    printfn ""
    
    // Show current preferences
    printProfileSummary profile
    
    printfn "🎮 Starting Interactive Preference Management..."
    printfn "Press Enter to continue..."
    Console.ReadLine() |> ignore
    
    // Run the preference management system
    let! finalProfile = manageUserPreferences profile
    
    printfn ""
    printfn "✅ Preference management session completed!"
    printfn "Final preferences:"
    printProfileSummary finalProfile
    
    return finalProfile
}

[<EntryPoint>]
let main argv =
    try
        let result = runPreferenceDemo() |> Async.RunSynchronously
        printfn "🎯 Demo completed successfully!"
        0
    with
    | ex ->
        printfn "❌ Error: %s" ex.Message
        1
