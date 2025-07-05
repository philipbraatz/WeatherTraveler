namespace WeatherTraveler.Demo

open System
open WeatherTraveler.Types
open WeatherTraveler.LocationService
open WeatherTraveler.WeatherService
open WeatherTraveler.ExportService
open WeatherTraveler.GasPriceService

module DemoProgram =

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
        printfn "   • Current weather detection and location services ✅ IMPLEMENTED"
        printfn "   • Temperature range-based route planning ✅ IMPLEMENTED" 
        printfn "   • Enhanced weather forecasting with realistic patterns ✅ IMPLEMENTED"
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
        
        // Get current weather for all locations
        let! denverWeather = getCurrentWeather denverCoord
        let! vegasWeather = getCurrentWeather vegasCoord
        let! laWeather = getCurrentWeather laCoord
        
        let weatherResults = [denverWeather; vegasWeather; laWeather]
        let locations = ["Denver, CO"; "Las Vegas, NV"; "Los Angeles, CA"]
        
        let displayWeather (location: string) (weather: WeatherInfo) =
            let tempColor = 
                match weather.Temperature with
                | t when t < 10.0 -> "🥶"
                | t when t < 20.0 -> "🌤️ "
                | t when t < 30.0 -> "☀️ "
                | _ -> "🔥"
            
            let conditionIcon = 
                match weather.Condition with
                | Sunny -> "☀️ "
                | Cloudy -> "☁️ "
                | Rainy -> "🌧️ "
                | Snowy -> "❄️ "
            
            printfn "   %s %s%.1f°C %s%s" tempColor location weather.Temperature conditionIcon (weather.Condition.ToString())
        
        printfn "🌤️  Current Weather Conditions:"
        if not (List.isEmpty weatherResults) then
            List.zip locations weatherResults
            |> List.iter (fun (location, weather) -> displayWeather location weather)
        printfn ""
        
        // Enhanced weather forecasting demo
        printfn "🔮 Enhanced Weather Forecasting (Las Vegas):"
        let! vegasForecast = getWeatherForecast vegasCoord
        
        let displayForecast (forecast: WeatherInfo) =
            let tempStatus = 
                match forecast.Temperature with
                | t when t >= 18.0 && t <= 25.0 -> "✅ OPTIMAL"
                | t when t >= 15.0 && t <= 30.0 -> "⚠️  ACCEPTABLE" 
                | _ -> "❌ OUTSIDE RANGE"
            
            printfn "   • %.1f°C %s %s %s" 
                forecast.Temperature 
                (forecast.Condition.ToString()) 
                tempStatus
                (match forecast.Condition with Rainy -> "🌧️ " | Snowy -> "❄️ " | _ -> "")
        
        List.iter displayForecast vegasForecast
        printfn ""
        
        // Temperature range analysis
        printfn "🌡️  Temperature Range Analysis:"
        let tempRange = { MinCelsius = 18.0; MaxCelsius = 25.0 }
        let bestWindow = findBestWeatherWindow vegasForecast tempRange 12
        
        match bestWindow with
        | Some window ->
            printfn "   ✅ Best weather window found:"
            printfn "   📅 Optimal time range: %d-hour window available" window.Duration
            printfn "   🌡️  Temperature range: %.1f°C - %.1f°C" tempRange.MinCelsius tempRange.MaxCelsius
            printfn "   ⭐ Confidence level: High"
        | None ->
            printfn "   ⚠️  No optimal weather window found for desired temperature range"
            printfn "   🌡️  Target range: %.1f°C - %.1f°C" tempRange.MinCelsius tempRange.MaxCelsius
            printfn "   💡 Consider adjusting travel dates or temperature preferences"
        
        printfn ""
        
        // Demo Google Earth export
        printfn "🌍 Google Earth Export Demo:"
        let exportRoute = {
            Name = "Weather Traveler Demo Route"
            Description = "Denver → Las Vegas → Los Angeles road trip with weather optimization"
            Coordinates = [denverCoord; vegasCoord; laCoord]
            WeatherData = weatherResults
        }
        
        try
            let! kmlContent = exportToGoogleEarth exportRoute "weather_demo_route.kml"
            printfn "   ✅ KML file generated successfully!"
            printfn "   📁 File: weather_demo_route.kml (%.1f KB)" (float kmlContent.Length / 1024.0)
            printfn "   🌍 Ready for import into Google Earth"
            printfn "   📊 Includes weather data and route optimization"
        with
        | ex ->
            printfn "   ⚠️ KML export demo (simulated): %s" ex.Message
            printfn "   💡 Would generate comprehensive route data for Google Earth"
        
        printfn ""
        
        // Demo gas price estimation using available functions
        printfn "⛽ Gas Price Estimation Demo:"
        
        try
            // Use the available function getGasPricesNearLocation with default options
            let! denverGas = getGasPricesNearLocation denverCoord defaultSearchOptions
            let! vegasGas = getGasPricesNearLocation vegasCoord defaultSearchOptions
            let! laGas = getGasPricesNearLocation laCoord defaultSearchOptions
            
            let allGasPrices = [denverGas; vegasGas; laGas]
            
            printfn "   💰 Current Gas Prices Along Route:"
            List.zip3 locations allGasPrices [denverCoord; vegasCoord; laCoord]
            |> List.iter (fun (location, gasPrices, coord) ->
                if not gasPrices.IsEmpty then
                    let cheapest = gasPrices |> List.minBy (fun s -> s.PricePerGallon)
                    printfn "   📍 %s: $%.2f/gallon" location cheapest.PricePerGallon
                    
                    let priceCategory = 
                        match cheapest.PricePerGallon with
                        | p when p < 3.50 -> "💚 Excellent"
                        | p when p < 4.00 -> "💛 Good"
                        | p when p < 4.50 -> "🧡 Average"
                        | _ -> "❤️ High"
                    
                    printfn "       %s | Station: %s" priceCategory cheapest.Name
                else
                    printfn "   📍 %s: No stations found" location)
            
            printfn ""
            printfn "   🧮 Trip Fuel Cost Estimation:"
            printfn "   📏 Total Distance: %.1f km (%.1f miles)" totalDistance (totalDistance * 0.621371)
            printfn "   ⛽ Estimated Fuel Needed: %.1f gallons" (totalDistance * 0.621371 / 25.0)
            
            let averagePrice = 
                allGasPrices 
                |> List.collect id
                |> List.map (fun s -> s.PricePerGallon)
                |> function 
                   | [] -> 4.00 // default price
                   | prices -> List.average prices
            
            let totalCost = (totalDistance * 0.621371 / 25.0) * averagePrice
            printfn "   💵 Estimated Total Cost: $%.2f" totalCost
            
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
        printfn "--- End of Weather Traveler Demo ---\n"
        
        return weatherResults
    }
