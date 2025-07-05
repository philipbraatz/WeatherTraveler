namespace WeatherTraveler.Demo

open System
open WeatherTraveler.Types
open WeatherTraveler.LocationService
open WeatherTraveler.WeatherService
open WeatherTraveler.Configuration

module SimpleDemoProgram =

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
    printfn "   • Location and distance calculation services ✅ IMPLEMENTED" 
    printfn "   • Weather data integration ✅ IMPLEMENTED"
    printfn "   • User preference management ✅ IMPLEMENTED"
    printfn "   • Interactive console application ✅ IMPLEMENTED"
    printfn ""
    
    // Demo basic functionality
    let denverCoord = { Latitude = 39.7392; Longitude = -104.9903 }
    let vegasCoord = { Latitude = 36.1699; Longitude = -115.1398 }
    let laCoord = { Latitude = 34.0522; Longitude = -118.2437 }
    
    printfn "🗺️  Sample Route: Denver, CO → Las Vegas, NV → Los Angeles, CA"
    printfn ""
    
    // Use distance calculations
    let denverToVegas = calculateDistance denverCoord vegasCoord
    let vegasToLA = calculateDistance vegasCoord laCoord
    let totalDistance = denverToVegas + vegasToLA
    
    printfn "📏 Distance Calculations:"
    printfn "   Denver → Las Vegas: %.1f km" denverToVegas
    printfn "   Las Vegas → Los Angeles: %.1f km" vegasToLA
    printfn "   Total Distance: %.1f km" totalDistance
    printfn ""
    
    printfn "🎯 Demo completed successfully!"
    printfn ""
    printfn "To explore more features:"
    printfn "   • Run the Console application for interactive preference management"
    printfn "   • Check the Tests project for API usage examples"
    printfn ""
    
    return []
}
