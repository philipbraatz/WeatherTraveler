module WeatherTraveler.RouteService

open System
open WeatherTraveler.Types
open WeatherTraveler.LocationService
open WeatherTraveler.WeatherService

/// Route optimization preferences
type RouteOptimization =
    | FastestRoute
    | ShortestDistance
    | BestWeather
    | AvoidRain
    | LowestFuelCost

/// Travel request with preferences
type TravelPlanRequest = {
    StartLocation: string * Coordinate
    Destinations: (string * Coordinate) list
    TemperatureRange: TemperatureRange
    AvoidRain: bool
    MaxDrivingHoursPerDay: int
    StartDate: DateTime
    Optimization: RouteOptimization
    FuelBudget: float option
}

/// Route segment between two points
type RouteSegment = {
    From: string * Coordinate
    To: string * Coordinate
    Distance: float
    EstimatedDrivingTime: float // hours
    FuelCost: float
    WeatherForecast: WeatherInfo list
    RecommendedDepartureTime: DateTime option
}

/// Complete travel route
type TravelRoute = {
    Segments: RouteSegment list
    TotalDistance: float
    TotalDrivingTime: float
    EstimatedFuelCost: float
    WeatherCompliance: bool
    OverallWeatherRating: float // 0-10 scale
}

/// Route planning result
type RouteResult = {
    RecommendedRoute: TravelRoute
    AlternativeRoutes: TravelRoute list
    WeatherWarnings: string list
    TravelSchedule: (DateTime * RouteSegment) list
}

/// Calculate fuel cost for distance
let calculateFuelCost (distance: float) =
    let fuelConsumptionPer100km = 8.5 // liters per 100km (average)
    let fuelPricePerLiter = 1.50 // USD per liter (average)
    (distance / 100.0) * fuelConsumptionPer100km * fuelPricePerLiter

/// Calculate driving time considering traffic and road types
let calculateDrivingTime (distance: float) =
    let averageSpeed = 80.0 // km/h highway speed
    distance / averageSpeed

/// Rate weather conditions on 0-10 scale for travel
let rateWeatherForTravel (weather: WeatherInfo) (preferences: TravelPlanRequest) =
    let tempScore = 
        if meetsTemperatureRange weather preferences.TemperatureRange then 3.0 else 0.0
    
    let conditionScore = 
        match weather.Condition with
        | Sunny -> 4.0
        | PartlyCloudy -> 3.5
        | Cloudy -> 2.5
        | Foggy -> 1.5
        | Rainy when preferences.AvoidRain -> 0.5
        | Rainy -> 2.0
        | Stormy -> 0.0
        | Snowy -> 1.0
    
    let windScore = 
        if weather.WindSpeed < 20.0 then 3.0
        elif weather.WindSpeed < 40.0 then 2.0
        else 1.0
    
    tempScore + conditionScore + windScore

/// Create a route segment between two locations
let createRouteSegment (from: string * Coordinate) (destination: string * Coordinate) (preferences: TravelPlanRequest) = async {
    let (fromName, fromCoord) = from
    let (destName, destCoord) = destination
    
    let distance = calculateDistance fromCoord destCoord
    let drivingTime = calculateDrivingTime distance
    let fuelCost = calculateFuelCost distance
      // Get weather forecast for destination
    let! weatherForecast = getWeatherForecastCompat destCoord
    
    return {
        From = from
        To = destination
        Distance = distance
        EstimatedDrivingTime = drivingTime
        FuelCost = fuelCost
        WeatherForecast = weatherForecast
        RecommendedDepartureTime = None // Will be optimized later
    }
}

/// Find the best weather window for a route segment
let findOptimalDepartureTime (segment: RouteSegment) (preferences: TravelPlanRequest) =
    let arrivalTimes = 
        segment.WeatherForecast
        |> List.map (fun weather -> 
            let rating = rateWeatherForTravel weather preferences
            (weather.Timestamp, rating, weather))
        |> List.sortByDescending (fun (_, rating, _) -> rating)
    
    match arrivalTimes with
    | (arrivalTime, rating, weather) :: _ when rating >= 6.0 ->
        // Calculate departure time by subtracting driving time
        let departureTime = arrivalTime.AddHours(-segment.EstimatedDrivingTime)
        Some departureTime
    | _ -> None

/// Plan optimal route with weather considerations
let rec planOptimalRoute (request: TravelPlanRequest) = async {
    let (startName, startCoord) = request.StartLocation
    let allLocations = request.StartLocation :: request.Destinations
    
    // Create route segments using nearest neighbor approach (simplified)
    let mutable currentLocation = request.StartLocation
    let mutable remainingDestinations = request.Destinations
    let mutable segments = []
    
    while not remainingDestinations.IsEmpty do
        let (_, currentCoord) = currentLocation
        
        // Find nearest unvisited destination based on optimization preference
        let nextDestination = 
            match request.Optimization with
            | ShortestDistance | FastestRoute ->
                remainingDestinations
                |> List.minBy (fun (_, coord) -> calculateDistance currentCoord coord)
            | BestWeather | AvoidRain ->
                // More complex selection considering weather (simplified here)
                remainingDestinations
                |> List.minBy (fun (_, coord) -> calculateDistance currentCoord coord)
            | LowestFuelCost ->
                remainingDestinations
                |> List.minBy (fun (_, coord) -> 
                    let distance = calculateDistance currentCoord coord
                    calculateFuelCost distance)
        
        // Create segment
        let! segment = createRouteSegment currentLocation nextDestination request
        
        // Optimize departure time
        let optimizedSegment = 
            match findOptimalDepartureTime segment request with
            | Some departureTime -> 
                { segment with RecommendedDepartureTime = Some departureTime }
            | None -> segment
        
        segments <- optimizedSegment :: segments
        currentLocation <- nextDestination
        remainingDestinations <- remainingDestinations |> List.filter (fun dest -> dest <> nextDestination)
    
    let segments = List.rev segments
    
    // Calculate totals
    let totalDistance = segments |> List.sumBy (fun s -> s.Distance)
    let totalDrivingTime = segments |> List.sumBy (fun s -> s.EstimatedDrivingTime)
    let estimatedFuelCost = segments |> List.sumBy (fun s -> s.FuelCost)
    
    // Check weather compliance
    let weatherCompliance = 
        segments
        |> List.forall (fun segment ->
            segment.WeatherForecast
            |> List.exists (fun w -> meetsTemperatureRange w request.TemperatureRange))
    
    // Calculate overall weather rating
    let overallWeatherRating = 
        segments
        |> List.collect (fun s -> s.WeatherForecast)
        |> List.map (fun w -> rateWeatherForTravel w request)
        |> List.average
    
    // Generate weather warnings
    let weatherWarnings = 
        segments
        |> List.collect (fun segment ->
            let (destName, _) = segment.To
            let warnings = []
            
            // Check temperature compliance
            let tempWarnings = 
                if not (segment.WeatherForecast |> List.exists (fun w -> meetsTemperatureRange w request.TemperatureRange))
                then [sprintf "⚠️ %s: Temperatures may be outside preferred range" destName]
                else []
            
            // Check rain conditions
            let rainWarnings = 
                if request.AvoidRain then
                    let rainyPeriods = segment.WeatherForecast |> List.filter (fun w -> w.Condition = Rainy)
                    if not rainyPeriods.IsEmpty then
                        [sprintf "🌧️ %s: Rain expected during visit period" destName]
                    else []
                else []
            
            tempWarnings @ rainWarnings)
    
    let route = {
        Segments = segments
        TotalDistance = totalDistance
        TotalDrivingTime = totalDrivingTime
        EstimatedFuelCost = estimatedFuelCost
        WeatherCompliance = weatherCompliance
        OverallWeatherRating = overallWeatherRating
    }
    
    // Create travel schedule
    let travelSchedule = createTravelSchedule route request
    
    return {
        RecommendedRoute = route
        AlternativeRoutes = [] // Could generate alternatives here
        WeatherWarnings = weatherWarnings
        TravelSchedule = travelSchedule
    }
}

/// Create a daily travel schedule respecting daily driving limits
and createTravelSchedule (route: TravelRoute) (request: TravelPlanRequest) =
    let maxDailyHours = float request.MaxDrivingHoursPerDay
    let mutable currentDate = request.StartDate
    let mutable dailyDrivingTime = 0.0
    let mutable schedule = []
    
    for segment in route.Segments do
        // Check if we can fit this segment in the current day
        if dailyDrivingTime + segment.EstimatedDrivingTime <= maxDailyHours then
            // Add to current day
            let departureTime = 
                match segment.RecommendedDepartureTime with
                | Some time -> time
                | None -> currentDate.AddHours(8.0) // Default 8 AM start
            
            schedule <- (departureTime, segment) :: schedule
            dailyDrivingTime <- dailyDrivingTime + segment.EstimatedDrivingTime
        else
            // Move to next day
            currentDate <- currentDate.AddDays(1.0)
            dailyDrivingTime <- segment.EstimatedDrivingTime
            
            let departureTime = 
                match segment.RecommendedDepartureTime with
                | Some time -> time
                | None -> currentDate.AddHours(8.0)
            
            schedule <- (departureTime, segment) :: schedule
    
    List.rev schedule

/// Generate alternative routes with different optimizations
let generateAlternativeRoutes (request: TravelPlanRequest) = async {
    let alternativeOptimizations = 
        [FastestRoute; ShortestDistance; BestWeather; LowestFuelCost]
        |> List.filter (fun opt -> opt <> request.Optimization)
    
    let! alternatives = 
        alternativeOptimizations
        |> List.map (fun opt -> 
            planOptimalRoute { request with Optimization = opt })
        |> Async.Parallel
    
    return alternatives |> Array.map (fun result -> result.RecommendedRoute) |> Array.toList
}

/// Analyze route feasibility considering constraints
let analyzeRouteFeasibility (request: TravelPlanRequest) = async {
    let! routeResult = planOptimalRoute request
    
    let feasibilityScore = 
        let weatherScore = if routeResult.RecommendedRoute.WeatherCompliance then 30.0 else 10.0
        let budgetScore = 
            match request.FuelBudget with
            | Some budget when routeResult.RecommendedRoute.EstimatedFuelCost <= budget -> 25.0
            | Some _ -> 10.0
            | None -> 20.0
        let weatherRatingScore = routeResult.RecommendedRoute.OverallWeatherRating * 4.5
        let warningsScore = 
            let warningCount = float routeResult.WeatherWarnings.Length
            max 0.0 (20.0 - warningCount * 5.0)
        
        weatherScore + budgetScore + weatherRatingScore + warningsScore
    
    let recommendation = 
        if feasibilityScore >= 80.0 then "Excellent route - highly recommended"
        elif feasibilityScore >= 60.0 then "Good route with minor considerations"
        elif feasibilityScore >= 40.0 then "Acceptable route but consider alternatives"
        else "Route has significant challenges - consider adjusting preferences"
    
    return {|
        FeasibilityScore = feasibilityScore
        Recommendation = recommendation
        RouteResult = routeResult
    |}
}

/// Bridge function to convert between Types.fs and RouteService.fs type systems
let planRoute (request: WeatherTraveler.Types.TravelPlanRequest) = async {
    // Convert Types.fs format to RouteService.fs format
    let convertedRequest = {
        StartLocation = (request.StartLocation.Name, request.StartLocation.Coordinate)
        Destinations = request.DestinationLocations |> List.map (fun loc -> (loc.Name, loc.Coordinate))
        TemperatureRange = request.TemperatureRange
        AvoidRain = request.AvoidRain
        MaxDrivingHoursPerDay = request.MaxDrivingHoursPerDay
        StartDate = request.StartDate
        Optimization = BestWeather // Default optimization
        FuelBudget = None // Not specified in Types.fs version
    }
    
    let! routeResult = planOptimalRoute convertedRequest
    
    // Convert back to Types.fs format
    let convertedRoute = {
        Segments = routeResult.RecommendedRoute.Segments |> List.map (fun seg ->
            {
                From = { Name = fst seg.From; Coordinate = snd seg.From; PreferredArrivalDate = None; PreferredDepartureDate = None; IsRequired = true }
                To = { Name = fst seg.To; Coordinate = snd seg.To; PreferredArrivalDate = None; PreferredDepartureDate = None; IsRequired = true }
                Distance = seg.Distance
                EstimatedDrivingTime = TimeSpan.FromHours(seg.EstimatedDrivingTime)
                WeatherForecast = seg.WeatherForecast
            })
        TotalDistance = routeResult.RecommendedRoute.TotalDistance
        TotalTime = TimeSpan.FromHours(routeResult.RecommendedRoute.TotalDrivingTime)
        EstimatedFuelCost = routeResult.RecommendedRoute.EstimatedFuelCost
        WeatherCompliance = routeResult.RecommendedRoute.WeatherCompliance
    }
    
    return {
        RecommendedRoute = convertedRoute
        AlternativeRoutes = [] // Simplified for now
        WeatherWarnings = routeResult.WeatherWarnings
        EstimatedCosts = {| Fuel = convertedRoute.EstimatedFuelCost; Accommodation = None |}
    }
}
