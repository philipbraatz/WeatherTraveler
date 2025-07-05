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
        
        printfn "🌟 Welcome to Enhanced Weather Traveler Demo!"
        printfn ""
        printfn "This F# application demonstrates weather-aware travel planning features:"
        printfn ""
        
        printfn "📍 Key Features:"
        printfn "   • Current weather detection and location services ✅ IMPLEMENTED"
        printfn "   • Temperature range-based route planning (e.g., 12°C - 20°C) ✅ IMPLEMENTED" 
        printfn "   • Enhanced weather forecasting with realistic patterns ✅ IMPLEMENTED"
        printfn "   • Granular weather forecasting as travel time approaches ✅ IMPLEMENTED"
        printfn "   • Region/spot specification with timing constraints ✅ IMPLEMENTED"
        printfn "   • Rain storm avoidance/targeting capabilities ✅ IMPLEMENTED"
        printfn "   • Google Earth export functionality (KML) ✅ IMPLEMENTED"
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
        
        // Overall temperature compliance check
        printfn "🎯 Route Temperature Compliance:"
        let overallTempRange = { MinCelsius = 12.0; MaxCelsius = 30.0 }
        let temperatures = weatherResults |> List.map (fun w -> w.Temperature)
        let compliant = temperatures |> List.forall (fun t -> t >= overallTempRange.MinCelsius && t <= overallTempRange.MaxCelsius)
        let rainyLocations = weatherResults |> List.filter (fun w -> w.Condition = Rainy)
        
        if compliant then
            printfn "   ✅ All locations within acceptable temperature range (%.1f°C - %.1f°C)" overallTempRange.MinCelsius overallTempRange.MaxCelsius
        else
            printfn "   ⚠️  Some locations outside preferred temperature range"
            temperatures 
            |> List.iteri (fun i temp ->
                let location = locations.[i]
                if temp < overallTempRange.MinCelsius || temp > overallTempRange.MaxCelsius then
                    printfn "   🌡️  %s: %.1f°C (outside range)" location temp)
        
        if not (List.isEmpty rainyLocations) then
            printfn "   🌧️  Rain expected in %d location(s)" rainyLocations.Length
        else
            printfn "   ☀️  No rain expected along route"
        
        printfn ""
        
        // Demo granular weather forecasting as travel approaches
        printfn "⏰ Granular Weather Forecasting (Approaching Las Vegas):"
        let vegasName = "Las Vegas, NV"
        let vegasDemoTimes = [
            ("24 hours before arrival", 24)
            ("12 hours before arrival", 12)
            ("6 hours before arrival", 6)
            ("2 hours before arrival", 2)
            ("Arrival time", 0)
        ]
        
        for (label, travelTime) in vegasDemoTimes do
            let! specificForecast = getGranularForecast vegasCoord travelTime
            let accuracy = 
                match travelTime with
                | t when t <= 2 -> "Very High (±1°C)"
                | t when t <= 6 -> "High (±2°C)"
                | t when t <= 12 -> "Good (±3°C)"
                | _ -> "Moderate (±5°C)"
            
            printfn "   📅 %s: %.1f°C %s (Accuracy: %s)" 
                label 
                specificForecast.Temperature 
                (specificForecast.Condition.ToString()) 
                accuracy
        
        printfn ""
        
        // Demo timing constraints
        printfn "🕐 Timing Constraints Demo (Multi-Day National Parks Trip):"
        let nationalParksTrip = [
            { Name = "Zion National Park"; Coordinate = { Latitude = 37.2982; Longitude = -113.0263 }; PreferredArrivalHour = 8; MaxStayHours = 6 }
            { Name = "Bryce Canyon National Park"; Coordinate = { Latitude = 37.5930; Longitude = -112.1871 }; PreferredArrivalHour = 14; MaxStayHours = 4 }
            { Name = "Arches National Park"; Coordinate = { Latitude = 38.7331; Longitude = -109.5925 }; PreferredArrivalHour = 9; MaxStayHours = 8 }
            { Name = "Canyonlands National Park"; Coordinate = { Latitude = 38.2619; Longitude = -109.8378 }; PreferredArrivalHour = 15; MaxStayHours = 3 }
        ]
        
        for location in nationalParksTrip do
            let! weather = getCurrentWeather location.Coordinate
            let arrivalTime = sprintf "%02d:00" location.PreferredArrivalHour
            let departureTime = sprintf "%02d:00" ((location.PreferredArrivalHour + location.MaxStayHours) % 24)
            
            printfn "   🏞️  %s:" location.Name
            printfn "       📍 Coordinates: %.4f°, %.4f°" location.Coordinate.Latitude location.Coordinate.Longitude
            printfn "       🕐 Arrival: %s | Departure: %s (%dh visit)" arrivalTime departureTime location.MaxStayHours
            printfn "       🌡️  Weather: %.1f°C %s" weather.Temperature (weather.Condition.ToString())
            
            let weatherSuitability = 
                match weather.Condition, weather.Temperature with
                | Sunny, t when t >= 15.0 && t <= 25.0 -> "🌟 Perfect for hiking"
                | Sunny, t when t > 25.0 -> "☀️ Great but bring water"
                | Cloudy, t when t >= 10.0 && t <= 20.0 -> "☁️ Good for photography"
                | Rainy, _ -> "🌧️ Consider indoor activities"
                | Snowy, _ -> "❄️ Winter sports conditions"
                | _ -> "⚠️ Check current conditions"
            
            printfn "       💡 %s" weatherSuitability
            printfn ""
        
        // Demo schedule conflict detection
        printfn "⚠️  Schedule Conflict Detection:"
        let mutable hasConflicts = false
        
        for i in 0..nationalParksTrip.Length-2 do
            let current = nationalParksTrip.[i]
            let next = nationalParksTrip.[i+1]
            let distance = calculateDistance current.Coordinate next.Coordinate
            let estimatedTravelTime = distance / 80.0 // Assume 80 km/h average speed
            let currentDeparture = current.PreferredArrivalHour + current.MaxStayHours
            let travelBuffer = next.PreferredArrivalHour - currentDeparture
            
            if estimatedTravelTime > float travelBuffer then
                hasConflicts <- true
                printfn "   ⚠️  Potential conflict: %s → %s" current.Name next.Name
                printfn "       🚗 Distance: %.1f km (%.1fh travel time)" distance estimatedTravelTime
                printfn "       ⏰ Available time: %dh | Needed: %.1fh" travelBuffer estimatedTravelTime
                printfn "       💡 Suggestion: Extend departure time or reduce visit duration"
                printfn ""
        
        if not hasConflicts then
            printfn "   ✅ No schedule conflicts detected! All timing constraints are feasible."
            printfn "   🎯 Trip is well-planned with adequate travel buffers."
        
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
        
        // Demo gas price estimation
        printfn "⛽ Gas Price Estimation Demo:"
        
        try
            let! gasPrices = getGasPricesAlongRoute [denverCoord; vegasCoord; laCoord]
            
            printfn "   💰 Current Gas Prices Along Route:"
            gasPrices
            |> List.iteri (fun i price ->
                let location = locations.[i]
                printfn "   📍 %s: $%.2f/gallon" location price.PricePerGallon
                
                let priceCategory = 
                    match price.PricePerGallon with
                    | p when p < 3.50 -> "💚 Excellent"
                    | p when p < 4.00 -> "💛 Good"
                    | p when p < 4.50 -> "🧡 Average"
                    | _ -> "❤️ High"
                
                printfn "       %s | Station: %s" priceCategory price.StationName)
            
            let totalCost = estimateFuelCost totalDistance 25.0 gasPrices // Assume 25 MPG
            printfn ""
            printfn "   🧮 Trip Fuel Cost Estimation:"
            printfn "   📏 Total Distance: %.1f km (%.1f miles)" totalDistance (totalDistance * 0.621371)
            printfn "   ⛽ Estimated Fuel Needed: %.1f gallons" (totalDistance * 0.621371 / 25.0)
            printfn "   💵 Estimated Total Cost: $%.2f" totalCost
            
            let cheapestStation = gasPrices |> List.minBy (fun p -> p.PricePerGallon)
            let mostExpensiveStation = gasPrices |> List.maxBy (fun p -> p.PricePerGallon)
            let savings = (mostExpensiveStation.PricePerGallon - cheapestStation.PricePerGallon) * (totalDistance * 0.621371 / 25.0)
            
            printfn "   💡 Potential Savings: $%.2f by choosing optimal stations" savings
            
            let fuelUpRecommendations = [
                sprintf "Fill up in %s (lowest price: $%.2f)" (locations.[gasPrices |> List.findIndex (fun p -> p.PricePerGallon = cheapestStation.PricePerGallon)]) cheapestStation.PricePerGallon
                sprintf "Avoid filling up in %s unless necessary" (locations.[gasPrices |> List.findIndex (fun p -> p.PricePerGallon = mostExpensiveStation.PricePerGallon)])
            ]
            
            if not fuelUpRecommendations.IsEmpty then
                printfn "⏰ Optimal Fuel-Up Timing:"
                fuelUpRecommendations
                |> List.iter (fun recommendation -> printfn "   • %s" recommendation)
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
        printfn "--- End of Gas Price Estimation Demo ---\n"
        
        return weatherResults
    }
