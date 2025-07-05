module WeatherTraveler.CacheService

open System
open System.IO
open System.Collections.Concurrent
open Newtonsoft.Json
open WeatherTraveler.Types
open WeatherTraveler.Configuration

/// Cache entry with expiration
type CacheEntry<'T> = {
    Data: 'T
    Timestamp: DateTime
    ExpiresAt: DateTime
}

/// Cache categories
type CacheCategory =
    | WeatherData
    | RouteData
    | GasPriceData
    | LocationData

/// In-memory cache storage
let private memoryCache = ConcurrentDictionary<string, obj>()

/// Generate cache key for weather data
let private weatherCacheKey (coordinate: Coordinate) (forecastType: string) =
    sprintf "weather_%s_%.4f_%.4f" forecastType coordinate.Latitude coordinate.Longitude

/// Generate cache key for route data
let private routeCacheKey (fromCoord: Coordinate) (toCoord: Coordinate) =
    sprintf "route_%.4f_%.4f_%.4f_%.4f" fromCoord.Latitude fromCoord.Longitude toCoord.Latitude toCoord.Longitude

/// Generate cache key for gas price data
let private gasPriceCacheKey (coordinate: Coordinate) (radius: float) =
    sprintf "gasprice_%.4f_%.4f_%.1f" coordinate.Latitude coordinate.Longitude radius

/// Get cache file path
let private getCacheFilePath (config: AppConfig) (category: CacheCategory) (key: string) =
    let categoryDir = 
        match category with
        | WeatherData -> "weather"
        | RouteData -> "routes"
        | GasPriceData -> "gasprices"
        | LocationData -> "locations"
    
    let cacheDir = Path.Combine(config.AppSettings.DataCacheDirectory, categoryDir)
    if not (Directory.Exists(cacheDir)) then
        Directory.CreateDirectory(cacheDir) |> ignore
    
    let safeKey = key.Replace(".", "_").Replace("/", "_")
    Path.Combine(cacheDir, sprintf "%s.json" safeKey)

/// Create cache entry
let private createCacheEntry<'T> (data: 'T) (expirationMinutes: int) = {
    Data = data
    Timestamp = DateTime.UtcNow
    ExpiresAt = DateTime.UtcNow.AddMinutes(float expirationMinutes)
}

/// Check if cache entry is valid
let private isValidCacheEntry (entry: CacheEntry<'T>) =
    DateTime.UtcNow <= entry.ExpiresAt

/// Store data in memory cache
let storeInMemory<'T> (key: string) (data: 'T) (expirationMinutes: int) =
    let entry = createCacheEntry data expirationMinutes
    memoryCache.[key] <- box entry

/// Retrieve data from memory cache
let getFromMemory<'T> (key: string) : 'T option =
    match memoryCache.TryGetValue(key) with
    | (true, obj) ->
        try
            let entry = unbox<CacheEntry<'T>> obj
            if isValidCacheEntry entry then
                Some entry.Data
            else
                memoryCache.TryRemove(key) |> ignore
                None
        with
        | _ -> None
    | _ -> None

/// Store data in file cache
let storeInFile<'T> (config: AppConfig) (category: CacheCategory) (key: string) (data: 'T) (expirationMinutes: int) = async {
    try
        let entry = createCacheEntry data expirationMinutes
        let filePath = getCacheFilePath config category key
        let json = JsonConvert.SerializeObject(entry, Formatting.Indented)
        File.WriteAllText(filePath, json)
        return Ok ()
    with
    | ex ->
        printfn "⚠️  Failed to cache data to file: %s" ex.Message
        return Error ex.Message
}

/// Retrieve data from file cache
let getFromFile<'T> (config: AppConfig) (category: CacheCategory) (key: string) : 'T option =
    try
        let filePath = getCacheFilePath config category key
        if File.Exists(filePath) then
            let json = File.ReadAllText(filePath)
            let entry = JsonConvert.DeserializeObject<CacheEntry<'T>>(json)
            if isValidCacheEntry entry then
                Some entry.Data
            else
                File.Delete(filePath) // Clean up expired cache
                None
        else
            None
    with
    | ex ->
        printfn "⚠️  Failed to read cached data: %s" ex.Message
        None

/// Store weather data in cache
let cacheWeatherData (config: AppConfig) (coordinate: Coordinate) (forecastType: string) (data: WeatherInfo list) = async {
    let key = weatherCacheKey coordinate forecastType
    
    // Store in memory
    storeInMemory key data config.WeatherApi.CacheExpirationMinutes
    
    // Store in file
    let! result = storeInFile config WeatherData key data config.WeatherApi.CacheExpirationMinutes
    match result with
    | Ok _ -> printfn "💾 Cached weather data for %.4f, %.4f" coordinate.Latitude coordinate.Longitude
    | Error _ -> ()
}

/// Retrieve weather data from cache
let getCachedWeatherData (config: AppConfig) (coordinate: Coordinate) (forecastType: string) : WeatherInfo list option =
    let key = weatherCacheKey coordinate forecastType
    
    // Try memory cache first
    match getFromMemory<WeatherInfo list> key with
    | Some data -> 
        printfn "⚡ Retrieved weather data from memory cache"
        Some data
    | None ->
        // Try file cache
        match getFromFile<WeatherInfo list> config WeatherData key with
        | Some data ->
            // Restore to memory cache
            storeInMemory key data config.WeatherApi.CacheExpirationMinutes
            printfn "📁 Retrieved weather data from file cache"
            Some data
        | None -> None

/// Store route data in cache
let cacheRouteData (config: AppConfig) (fromCoord: Coordinate) (toCoord: Coordinate) (routeInfo: RouteSegment) = async {
    let key = routeCacheKey fromCoord toCoord
    
    // Cache routes for 24 hours since they rarely change
    let expirationMinutes = 24 * 60
    
    storeInMemory key routeInfo expirationMinutes
    let! result = storeInFile config RouteData key routeInfo expirationMinutes
    match result with
    | Ok _ -> printfn "🛣️  Cached route data"
    | Error _ -> ()
}

/// Retrieve route data from cache
let getCachedRouteData (config: AppConfig) (fromCoord: Coordinate) (toCoord: Coordinate) : RouteSegment option =
    let key = routeCacheKey fromCoord toCoord
    
    match getFromMemory<RouteSegment> key with
    | Some data -> 
        printfn "⚡ Retrieved route data from memory cache"
        Some data
    | None ->
        match getFromFile<RouteSegment> config RouteData key with
        | Some data ->
            storeInMemory key data (24 * 60) // 24 hours
            printfn "📁 Retrieved route data from file cache"
            Some data
        | None -> None

/// Store gas price data in cache  
let cacheGasPriceData (config: AppConfig) (coordinate: Coordinate) (radius: float) (data: obj list) = async {
    let key = gasPriceCacheKey coordinate radius
    
    // Cache gas prices for the configured update interval
    let expirationMinutes = config.GasPrice.UpdateIntervalHours * 60
    
    storeInMemory key data expirationMinutes
    let! result = storeInFile config GasPriceData key data expirationMinutes
    match result with
    | Ok _ -> printfn "⛽ Cached gas price data"
    | Error _ -> ()
}

/// Retrieve gas price data from cache
let getCachedGasPriceData (config: AppConfig) (coordinate: Coordinate) (radius: float) : obj list option =
    let key = gasPriceCacheKey coordinate radius
    
    match getFromMemory<obj list> key with
    | Some data -> 
        printfn "⚡ Retrieved gas price data from memory cache"
        Some data
    | None ->
        match getFromFile<obj list> config GasPriceData key with
        | Some data ->
            storeInMemory key data (config.GasPrice.UpdateIntervalHours * 60)
            printfn "📁 Retrieved gas price data from file cache"
            Some data
        | None -> None

/// Clear expired cache entries
let cleanupExpiredCache (config: AppConfig) = async {
    try
        let categories = [WeatherData; RouteData; GasPriceData; LocationData]
        let mutable totalCleaned = 0
        
        for category in categories do
            let categoryName = 
                match category with
                | WeatherData -> "weather"
                | RouteData -> "routes" 
                | GasPriceData -> "gasprices"
                | LocationData -> "locations"
            
            let categoryDir = Path.Combine(config.AppSettings.DataCacheDirectory, categoryName)
            if Directory.Exists(categoryDir) then
                let files = Directory.GetFiles(categoryDir, "*.json")
                for file in files do
                    try
                        let json = File.ReadAllText(file)
                        // Try to parse as generic cache entry to check expiration
                        let doc = JsonConvert.DeserializeObject<Newtonsoft.Json.Linq.JObject>(json)
                        let expiresAtStr = doc.["ExpiresAt"].ToString()
                        let expiresAt = DateTime.Parse(expiresAtStr)
                        
                        if DateTime.UtcNow > expiresAt then
                            File.Delete(file)
                            totalCleaned <- totalCleaned + 1
                    with
                    | _ -> () // Skip malformed files
        
        // Clean memory cache
        let expiredKeys = 
            memoryCache.Keys
            |> Seq.filter (fun key ->
                match memoryCache.TryGetValue(key) with
                | (true, obj) ->
                    try
                        // This is a bit hacky but works for cleanup
                        let objType = obj.GetType()
                        if objType.IsGenericType && objType.GetGenericTypeDefinition() = typedefof<CacheEntry<_>> then
                            let expiresAtProp = objType.GetProperty("ExpiresAt")
                            let expiresAt = expiresAtProp.GetValue(obj) :?> DateTime
                            DateTime.UtcNow > expiresAt
                        else false
                    with
                    | _ -> false
                | _ -> false)
            |> Seq.toList
        
        for key in expiredKeys do
            memoryCache.TryRemove(key) |> ignore
        
        totalCleaned <- totalCleaned + expiredKeys.Length
        
        if totalCleaned > 0 then
            printfn "🧹 Cleaned up %d expired cache entries" totalCleaned
        
        return Ok totalCleaned
    with
    | ex ->
        printfn "❌ Error during cache cleanup: %s" ex.Message
        return Error ex.Message
}

/// Get cache statistics
let getCacheStats (config: AppConfig) = async {
    try
        let categories = [WeatherData; RouteData; GasPriceData; LocationData]
        let mutable totalFiles = 0
        let mutable totalSizeBytes = 0L
        
        for category in categories do
            let categoryName = 
                match category with
                | WeatherData -> "weather"
                | RouteData -> "routes"
                | GasPriceData -> "gasprices" 
                | LocationData -> "locations"
            
            let categoryDir = Path.Combine(config.AppSettings.DataCacheDirectory, categoryName)
            if Directory.Exists(categoryDir) then
                let files = Directory.GetFiles(categoryDir, "*.json")
                totalFiles <- totalFiles + files.Length
                for file in files do
                    let fileInfo = FileInfo(file)
                    totalSizeBytes <- totalSizeBytes + fileInfo.Length
        
        let memoryEntries = memoryCache.Count
        
        return {|
            FileCacheEntries = totalFiles
            MemoryCacheEntries = memoryEntries
            TotalSizeBytes = totalSizeBytes
            TotalSizeMB = float totalSizeBytes / (1024.0 * 1024.0)
            CacheDirectory = config.AppSettings.DataCacheDirectory
        |}
    with
    | ex ->
        printfn "❌ Error getting cache stats: %s" ex.Message
        return {|
            FileCacheEntries = 0
            MemoryCacheEntries = 0
            TotalSizeBytes = 0L
            TotalSizeMB = 0.0
            CacheDirectory = config.AppSettings.DataCacheDirectory
        |}
}

/// Clear all cache data
let clearAllCache (config: AppConfig) = async {
    try
        // Clear memory cache
        memoryCache.Clear()
        
        // Clear file cache
        if Directory.Exists(config.AppSettings.DataCacheDirectory) then
            let categories = ["weather"; "routes"; "gasprices"; "locations"]
            for category in categories do
                let categoryDir = Path.Combine(config.AppSettings.DataCacheDirectory, category)
                if Directory.Exists(categoryDir) then
                    let files = Directory.GetFiles(categoryDir, "*.json")
                    for file in files do
                        File.Delete(file)
        
        printfn "🧹 All cache data cleared"
        return Ok ()
    with
    | ex ->
        printfn "❌ Error clearing cache: %s" ex.Message
        return Error ex.Message
}
