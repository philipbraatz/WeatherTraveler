namespace WeatherTraveler.Tests

open System
open System.IO
open Xunit
open WeatherTraveler.Configuration
open WeatherTraveler.PreferencesService

/// Basic tests for the WeatherTraveler preference management system
type PreferenceTests() =
    
    [<Fact>]
    member __.``Should load default user profile`` () =
        async {
            // Act
            let! profile = loadUserProfile()
            
            // Assert
            Assert.NotNull(profile)
            Assert.True(profile.Preferences.PreferredTemperatureRange.MinCelsius > 0.0)
            Assert.True(profile.Preferences.PreferredTemperatureRange.MaxCelsius > profile.Preferences.PreferredTemperatureRange.MinCelsius)
        } |> Async.RunSynchronously
    
    [<Fact>]
    member __.``Should update temperature range with quick function`` () =
        async {
            // Arrange
            let! profile = loadUserProfile()
            let newMin = 12.0
            let newMax = 28.0
            
            // Act
            let! result = quickUpdateTemperatureRange profile newMin newMax
            
            // Assert
            match result with
            | Ok updatedProfile ->
                Assert.Equal(newMin, updatedProfile.Preferences.PreferredTemperatureRange.MinCelsius)
                Assert.Equal(newMax, updatedProfile.Preferences.PreferredTemperatureRange.MaxCelsius)
            | Error msg ->
                Assert.True(false, $"Expected success but got error: {msg}")
        } |> Async.RunSynchronously
    
    [<Fact>]
    member __.``Should validate preferences correctly`` () =
        // Arrange
        let invalidPrefs = {
            PreferredTemperatureRange = { MinCelsius = 30.0; MaxCelsius = 10.0 } // Invalid: min > max
            AvoidRainByDefault = true
            PreferredDepartureTime = TimeSpan(8, 0, 0)
            MaxDrivingHoursPerDay = 8
            PreferredFuelType = "regular"
            PreferredGasStationBrands = ["Shell"]
            WeatherPriorityWeight = 0.7
            CostPriorityWeight = 0.5  // Total > 1.0, invalid
            TimePriorityWeight = 0.3
        }
        
        // Act
        let validationErrors = validatePreferences invalidPrefs
        
        // Assert
        Assert.True(List.length validationErrors > 0, "Should have validation errors")
    
    [<Fact>]
    member __.``Should toggle rain avoidance`` () =
        async {
            // Arrange
            let! profile = loadUserProfile()
            let originalSetting = profile.Preferences.AvoidRainByDefault
            
            // Act
            let! result = quickToggleRainAvoidance profile
            
            // Assert
            match result with
            | Ok updatedProfile ->
                Assert.NotEqual(originalSetting, updatedProfile.Preferences.AvoidRainByDefault)
            | Error msg ->
                Assert.True(false, $"Expected success but got error: {msg}")
        } |> Async.RunSynchronously
