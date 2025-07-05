namespace WeatherTraveler

open System
open System.IO
open WeatherTraveler.Configuration
open WeatherTraveler.PreferencesService
open WeatherTraveler.InteractivePreferenceManager

/// Automated test suite for preference management system
module AutomatedPreferenceTest =
    
    let private testUserId = "automated_test_user"
    
    let private logTest (testName: string) (result: string) =
        printfn "✅ TEST: %s - %s" testName result
    
    let private logError (testName: string) (error: string) =
        printfn "❌ ERROR: %s - %s" testName error
    
    /// Test 1: Temperature Range Updates
    let testTemperatureUpdates () =
        async {
            try
                // Test quick update
                do! updateTemperatureRange testUserId 10.0 30.0
                let! prefs = loadUserPreferences testUserId
                
                match prefs with
                | Some p when p.TemperatureRange.Min = 10.0 && p.TemperatureRange.Max = 30.0 ->
                    logTest "Temperature Range Update" "PASSED - Range updated correctly"
                | Some p ->
                    logError "Temperature Range Update" $"Expected 10-30°C, got {p.TemperatureRange.Min}-{p.TemperatureRange.Max}°C"
                | None ->
                    logError "Temperature Range Update" "Failed to load preferences"
            with
            | ex -> logError "Temperature Range Update" ex.Message
        }
    
    /// Test 2: Rain Avoidance Updates
    let testRainUpdates () =
        async {
            try
                // Test rain avoidance toggle
                do! updateRainAvoidance testUserId false
                let! prefs = loadUserPreferences testUserId
                
                match prefs with
                | Some p when not p.AvoidRain ->
                    logTest "Rain Avoidance Update" "PASSED - Rain avoidance disabled"
                | Some p ->
                    logError "Rain Avoidance Update" $"Expected false, got {p.AvoidRain}"
                | None ->
                    logError "Rain Avoidance Update" "Failed to load preferences"
            with
            | ex -> logError "Rain Avoidance Update" ex.Message
        }
    
    /// Test 3: Departure Time Updates
    let testDepartureTimeUpdates () =
        async {
            try
                let newTime = TimeSpan(14, 30, 0) // 2:30 PM
                do! updateDepartureTime testUserId newTime
                let! prefs = loadUserPreferences testUserId
                
                match prefs with
                | Some p when p.PreferredDepartureTime = newTime ->
                    logTest "Departure Time Update" "PASSED - Departure time updated correctly"
                | Some p ->
                    logError "Departure Time Update" $"Expected 14:30, got {p.PreferredDepartureTime}"
                | None ->
                    logError "Departure Time Update" "Failed to load preferences"
            with
            | ex -> logError "Departure Time Update" ex.Message
        }
    
    /// Test 4: Driving Hours Updates
    let testDrivingHoursUpdates () =
        async {
            try
                do! updateMaxDrivingHours testUserId 12
                let! prefs = loadUserPreferences testUserId
                
                match prefs with
                | Some p when p.MaxDrivingHoursPerDay = 12 ->
                    logTest "Driving Hours Update" "PASSED - Max driving hours updated correctly"
                | Some p ->
                    logError "Driving Hours Update" $"Expected 12, got {p.MaxDrivingHoursPerDay}"
                | None ->
                    logError "Driving Hours Update" "Failed to load preferences"
            with
            | ex -> logError "Driving Hours Update" ex.Message
        }
    
    /// Test 5: Fuel Preferences Updates
    let testFuelPreferencesUpdates () =
        async {
            try
                let newFuelPrefs = {
                    FuelType = Diesel
                    PreferredBrands = ["Shell"; "BP"]
                    MaxDeviationMiles = 10.0
                }
                do! updateFuelPreferences testUserId newFuelPrefs
                let! prefs = loadUserPreferences testUserId
                
                match prefs with
                | Some p when p.FuelPreferences.FuelType = Diesel ->
                    logTest "Fuel Preferences Update" "PASSED - Fuel preferences updated correctly"
                | Some p ->
                    logError "Fuel Preferences Update" $"Expected Diesel, got {p.FuelPreferences.FuelType}"
                | None ->
                    logError "Fuel Preferences Update" "Failed to load preferences"
            with
            | ex -> logError "Fuel Preferences Update" ex.Message
        }
    
    /// Test 6: Priority Weights Updates
    let testPriorityWeightsUpdates () =
        async {
            try
                let newWeights = {
                    WeatherWeight = 0.5
                    CostWeight = 0.3
                    TimeWeight = 0.2
                }
                do! updatePriorityWeights testUserId newWeights
                let! prefs = loadUserPreferences testUserId
                
                match prefs with
                | Some p when p.PriorityWeights.WeatherWeight = 0.5 ->
                    logTest "Priority Weights Update" "PASSED - Priority weights updated correctly"
                | Some p ->
                    logError "Priority Weights Update" $"Expected 0.5, got {p.PriorityWeights.WeatherWeight}"
                | None ->
                    logError "Priority Weights Update" "Failed to load preferences"
            with
            | ex -> logError "Priority Weights Update" ex.Message
        }
    
    /// Test 7: Notification Settings Updates
    let testNotificationUpdates () =
        async {
            try
                let newNotifications = {
                    EnableWeatherAlerts = false
                    EnablePriceAlerts = true
                    EnableRouteUpdates = false
                    EmailNotifications = true
                    PushNotifications = false
                }
                do! updateNotificationSettings testUserId newNotifications
                let! prefs = loadUserPreferences testUserId
                
                match prefs with
                | Some p when not p.NotificationSettings.EnableWeatherAlerts ->
                    logTest "Notification Settings Update" "PASSED - Notification settings updated correctly"
                | Some p ->
                    logError "Notification Settings Update" $"Expected false for weather alerts, got {p.NotificationSettings.EnableWeatherAlerts}"
                | None ->
                    logError "Notification Settings Update" "Failed to load preferences"
            with
            | ex -> logError "Notification Settings Update" ex.Message
        }
    
    /// Test 8: Reset All Preferences
    let testResetAllPreferences () =
        async {
            try
                do! resetAllPreferences testUserId
                let! prefs = loadUserPreferences testUserId
                let defaults = createDefaultPreferences ()
                
                match prefs with
                | Some p when p.TemperatureRange.Min = defaults.TemperatureRange.Min &&
                             p.TemperatureRange.Max = defaults.TemperatureRange.Max ->
                    logTest "Reset All Preferences" "PASSED - All preferences reset to defaults"
                | Some p ->
                    logError "Reset All Preferences" "Preferences not reset correctly"
                | None ->
                    logError "Reset All Preferences" "Failed to load preferences after reset"
            with
            | ex -> logError "Reset All Preferences" ex.Message
        }
    
    /// Test 9: Preference Validation
    let testPreferenceValidation () =
        async {
            try
                // Create invalid preferences
                let invalidWeights = {
                    WeatherWeight = 0.7
                    CostWeight = 0.5  // Total > 1.0
                    TimeWeight = 0.3
                }
                do! updatePriorityWeights testUserId invalidWeights
                
                let! prefs = loadUserPreferences testUserId
                match prefs with
                | Some p ->
                    let validationResult = validatePreferences p
                    match validationResult with
                    | Error errors when List.length errors > 0 ->
                        logTest "Preference Validation" "PASSED - Invalid preferences detected correctly"
                    | Ok _ ->
                        logError "Preference Validation" "Should have detected invalid preferences"
                | None ->
                    logError "Preference Validation" "Failed to load preferences"
            with
            | ex -> logError "Preference Validation" ex.Message
        }
    
    /// Run all tests
    let runAllTests () =
        async {
            printfn "🧪 Starting Automated Preference Management Tests..."
            printfn "═══════════════════════════════════════════════════════════════"
            
            // Initialize test user
            do! ensureUserProfile testUserId |> Async.Ignore
            
            // Run all tests
            do! testTemperatureUpdates ()
            do! testRainUpdates ()
            do! testDepartureTimeUpdates ()
            do! testDrivingHoursUpdates ()
            do! testFuelPreferencesUpdates ()
            do! testPriorityWeightsUpdates ()
            do! testNotificationUpdates ()
            do! testResetAllPreferences ()
            do! testPreferenceValidation ()
            
            printfn "═══════════════════════════════════════════════════════════════"
            printfn "🎯 Automated testing completed!"
            printfn ""
            
            // Clean up test user data
            let testFile = Path.Combine("profiles", $"{testUserId}.json")
            if File.Exists(testFile) then
                File.Delete(testFile)
                printfn "🧹 Test data cleaned up"
        }

// Program runner - call this module from main program
module AutomatedPreferenceTest =
