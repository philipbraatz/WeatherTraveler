module WeatherTraveler.GasPriceService

open System
open WeatherTraveler.Types

/// Gas station information
type GasStation = {
    Name: string
    Location: Coordinate
    Address: string
    PricePerLiter: float
    PricePerGallon: float
    LastUpdated: DateTime
    Distance: float // Distance from query point in km
    Brand: string
    Amenities: string list
}

/// Gas price search options
type GasPriceSearchOptions = {
    SearchRadius: float // km
    FuelType: string // "regular", "midgrade", "premium", "diesel"
    MaxResults: int
    SortBy: string // "price", "distance"
    IncludeAmenities: bool
}

/// Default search options
let defaultSearchOptions = {
    SearchRadius = 25.0
    FuelType = "regular"
    MaxResults = 10
    SortBy = "price"
    IncludeAmenities = true
}

/// Generate realistic mock gas station data
let generateMockGasStation (coordinate: Coordinate) (basePrice: float) (distance: float) (name: string) (brand: string) =
    let random = Random()
    let priceVariation = random.NextDouble() * 0.20 - 0.10 // ±10% price variation
    let pricePerLiter = Math.Max(0.50, basePrice + priceVariation)
    let pricePerGallon = pricePerLiter * 3.78541
    
    let amenities = [
        if random.NextDouble() > 0.3 then yield "Convenience Store"
        if random.NextDouble() > 0.5 then yield "Car Wash"
        if random.NextDouble() > 0.7 then yield "ATM"
        if random.NextDouble() > 0.8 then yield "Restaurant"
        if random.NextDouble() > 0.6 then yield "Restrooms"
        if random.NextDouble() > 0.9 then yield "EV Charging"
    ]
    
    {
        Name = name
        Location = coordinate
        Address = sprintf "%.4f, %.4f" coordinate.Latitude coordinate.Longitude
        PricePerLiter = pricePerLiter
        PricePerGallon = pricePerGallon
        LastUpdated = DateTime.Now.AddHours(-float (random.Next(1, 24)))
        Distance = distance
        Brand = brand
        Amenities = amenities
    }

/// Get gas prices near a location (enhanced mock implementation)
let getGasPricesNearLocation (coordinate: Coordinate) (options: GasPriceSearchOptions) = async {
    try
        // Mock implementation with realistic regional pricing
        let random = Random()
        
        // Base price varies by region (simplified)
        let latitudeFactor = Math.Abs(coordinate.Latitude) / 90.0
        let urbanFactor = if coordinate.Longitude > -100.0 then 0.10 else 0.0 // East coast more expensive
        let basePrice = 1.40 + (latitudeFactor * 0.15) + urbanFactor // USD per liter
        
        // Generate mock gas stations
        let brands = ["Shell"; "Exxon"; "BP"; "Chevron"; "Mobil"; "Sunoco"; "Circle K"; "Speedway"]
        let stations = [
            for i in 0..options.MaxResults-1 do
                let angle = (float i / float options.MaxResults) * 2.0 * Math.PI
                let distance = random.NextDouble() * options.SearchRadius
                let offsetLat = distance * Math.Cos(angle) / 111.0 // Approximate km to degrees
                let offsetLon = distance * Math.Sin(angle) / (111.0 * Math.Cos(coordinate.Latitude * Math.PI / 180.0))
                
                let stationCoord = {
                    Latitude = coordinate.Latitude + offsetLat
                    Longitude = coordinate.Longitude + offsetLon
                }
                
                let brand = brands.[random.Next(brands.Length)]
                let stationName = sprintf "%s #%d" brand (random.Next(1000, 9999))
                
                yield generateMockGasStation stationCoord basePrice distance stationName brand
        ]
        
        // Sort by user preference
        let sortedStations = 
            match options.SortBy.ToLower() with
            | "price" -> stations |> List.sortBy (fun s -> s.PricePerLiter)
            | "distance" -> stations |> List.sortBy (fun s -> s.Distance)
            | _ -> stations
        
        return sortedStations
    with
    | ex -> 
        printfn "Error fetching gas prices: %s" ex.Message
        return []
}

/// Calculate fuel cost for a route
let calculateFuelCost (route: TravelRoute) (vehicleFuelEfficiency: float) = async {
    // vehicleFuelEfficiency is in L/100km
    let totalFuelNeeded = (route.TotalDistance / 100.0) * vehicleFuelEfficiency
    
    // Get average gas price along the route
    let mutable totalPriceSum = 0.0
    let mutable priceCount = 0
    
    for segment in route.Segments do
        let! gasPrices = getGasPricesNearLocation segment.To.Coordinate defaultSearchOptions
        if not gasPrices.IsEmpty then
            let avgPrice = gasPrices |> List.map (fun gp -> gp.PricePerLiter) |> List.average
            totalPriceSum <- totalPriceSum + avgPrice
            priceCount <- priceCount + 1
    
    let averagePrice = 
        if priceCount > 0 then 
            totalPriceSum / float priceCount 
        else 
            1.50 // Default price per liter
    
    return totalFuelNeeded * averagePrice
}

/// Find cheapest gas stations along route
let findCheapestStationsAlongRoute (route: TravelRoute) (maxDetourKm: float) = async {
    let mutable cheapStations = []
    
    for segment in route.Segments do
        let! nearbyPrices = getGasPricesNearLocation segment.To.Coordinate defaultSearchOptions
        
        // Filter by detour distance and find cheapest
        let affordableStations = 
            nearbyPrices
            |> List.filter (fun station -> 
                LocationService.calculateDistance segment.To.Coordinate station.Location <= maxDetourKm)
            |> List.sortBy (fun station -> station.PricePerLiter)
            |> List.take (min 3 nearbyPrices.Length) // Top 3 cheapest
        
        cheapStations <- affordableStations @ cheapStations
    
    return cheapStations |> List.distinctBy (fun station -> station.Location)
}

/// Estimate daily fuel budget for trip
let estimateDailyFuelBudget (route: TravelRoute) (daysOfTravel: int) (vehicleFuelEfficiency: float) = async {
    let! totalFuelCost = calculateFuelCost route vehicleFuelEfficiency
    let dailyBudget = totalFuelCost / float daysOfTravel
    
    return {|
        TotalFuelCost = totalFuelCost
        DailyBudget = dailyBudget
        DaysOfTravel = daysOfTravel
        EstimatedFuelConsumption = (route.TotalDistance / 100.0) * vehicleFuelEfficiency
    |}
}

/// Get fuel price trends (mock implementation)
let getFuelPriceTrends (coordinate: Coordinate) (daysBack: int) = async {
    // Mock implementation - in reality you'd query historical price data
    let random = Random()
    let basePrice = 1.45
    
    return [
        for i in daysBack .. -1 .. 0 do
            let date = DateTime.Now.AddDays(float i)
            let dailyVariation = random.NextDouble() * 0.20 - 0.10
            yield (date, basePrice + dailyVariation)
    ]
}

/// Predict best time to fuel up based on price trends
let predictBestFuelUpTimes (route: TravelRoute) = async {
    let mutable recommendations = []
    
    // Simplified logic: recommend fueling at locations with historically lower prices
    // In reality, you'd analyze price patterns and predict optimal timing
    
    for segment in route.Segments do
        let! priceTrends = getFuelPriceTrends segment.To.Coordinate 7
        let avgPrice = priceTrends |> List.averageBy snd
        let lastPrice = priceTrends |> List.last |> snd
        
        if lastPrice < avgPrice * 0.95 then // If current price is 5% below average
            recommendations <- (sprintf "Good time to fuel up at %s - price below recent average" segment.To.Name) :: recommendations

    return List.rev recommendations
}