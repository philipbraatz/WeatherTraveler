module WeatherTraveler.Configuration

open System
open System.IO
open Newtonsoft.Json
open FSharp.Data

/// Configuration for weather API services
type WeatherApiConfig = {
    OpenWeatherApiKey: string option
    NoaaApiKey: string option
    WeatherStackApiKey: string option
    UseRealApis: bool
    DefaultCityIds: string list
    CacheExpirationMinutes: int
}

/// Configuration for mapping and routing services
type MappingConfig = {
    GoogleMapsApiKey: string option
    OpenStreetMapEnabled: bool
    DefaultRoutingProvider: string
    MaxRoutingRequests: int
    DistanceUnit: string // "km" or "miles"
}

/// Configuration for gas price services
type GasPriceConfig = {
    GasBuddyApiKey: string option
    DefaultSearchRadiusKm: float
    MaxStationsToSearch: int
    UpdateIntervalHours: int
    PreferredBrands: string list
}

/// User travel preferences
type UserPreferences = {
    PreferredTemperatureRange: WeatherTraveler.Types.TemperatureRange
    AvoidRainByDefault: bool
    PreferredDepartureTime: TimeSpan
    MaxDrivingHoursPerDay: int
    PreferredFuelType: string
    PreferredGasStationBrands: string list
    WeatherPriorityWeight: float // 0.0 - 1.0
    CostPriorityWeight: float // 0.0 - 1.0
    TimePriorityWeight: float // 0.0 - 1.0
}

/// Application settings
type AppSettings = {
    DataCacheDirectory: string
    ExportDirectory: string
    LogLevel: string
    AutoSaveEnabled: bool
    Theme: string
    Language: string
}

/// Main application configuration
type AppConfig = {
    WeatherApi: WeatherApiConfig
    Mapping: MappingConfig
    GasPrice: GasPriceConfig
    UserPreferences: UserPreferences
    AppSettings: AppSettings
    LastUpdated: DateTime
    Version: string
}

/// Default configuration values
let private defaultWeatherConfig = {
    OpenWeatherApiKey = None
    NoaaApiKey = None
    WeatherStackApiKey = None
    UseRealApis = false
    DefaultCityIds = ["5419384"; "5506956"; "5368361"] // Denver, Las Vegas, LA
    CacheExpirationMinutes = 30
}

let private defaultMappingConfig = {
    GoogleMapsApiKey = None
    OpenStreetMapEnabled = true
    DefaultRoutingProvider = "openstreetmap"
    MaxRoutingRequests = 100
    DistanceUnit = "km"
}

let private defaultGasPriceConfig = {
    GasBuddyApiKey = None
    DefaultSearchRadiusKm = 25.0
    MaxStationsToSearch = 10
    UpdateIntervalHours = 2
    PreferredBrands = ["Shell"; "Exxon"; "BP"; "Chevron"]
}

let private defaultUserPreferences = {
    PreferredTemperatureRange = { MinCelsius = 15.0; MaxCelsius = 25.0 }
    AvoidRainByDefault = true
    PreferredDepartureTime = TimeSpan.FromHours(8.0) // 8:00 AM
    MaxDrivingHoursPerDay = 8
    PreferredFuelType = "regular"
    PreferredGasStationBrands = ["Shell"; "Exxon"]
    WeatherPriorityWeight = 0.4
    CostPriorityWeight = 0.3
    TimePriorityWeight = 0.3
}

let private defaultAppSettings = {
    DataCacheDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "WeatherTraveler", "Cache")
    ExportDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "WeatherTraveler", "Exports")
    LogLevel = "Info"
    AutoSaveEnabled = true
    Theme = "Default"
    Language = "en-US"
}

/// Create default application configuration
let createDefaultConfig() = {
    WeatherApi = defaultWeatherConfig
    Mapping = defaultMappingConfig
    GasPrice = defaultGasPriceConfig
    UserPreferences = defaultUserPreferences
    AppSettings = defaultAppSettings
    LastUpdated = DateTime.UtcNow
    Version = "1.0.0"
}

/// Configuration file path
let private getConfigFilePath() =
    let appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)
    let configDir = Path.Combine(appDataPath, "WeatherTraveler")
    if not (Directory.Exists(configDir)) then
        Directory.CreateDirectory(configDir) |> ignore
    Path.Combine(configDir, "config.json")

/// Load configuration from file or create default
let loadConfig() = async {
    try
        let configPath = getConfigFilePath()
        if File.Exists(configPath) then
            let json = File.ReadAllText(configPath)
            let config = JsonConvert.DeserializeObject<AppConfig>(json)
            printfn "✅ Configuration loaded from: %s" configPath
            return config
        else
            let defaultConfig = createDefaultConfig()
            printfn "🔧 Created default configuration at: %s" configPath
            return defaultConfig
    with
    | ex ->
        printfn "⚠️  Error loading configuration: %s" ex.Message
        printfn "🔧 Using default configuration"
        return createDefaultConfig()
}

/// Save configuration to file
let saveConfig (config: AppConfig) = async {
    try
        let configPath = getConfigFilePath()
        let updatedConfig = { config with LastUpdated = DateTime.UtcNow }
        let json = JsonConvert.SerializeObject(updatedConfig, Formatting.Indented)
        File.WriteAllText(configPath, json)
        printfn "💾 Configuration saved to: %s" configPath
        return Ok ()
    with
    | ex ->
        printfn "❌ Error saving configuration: %s" ex.Message
        return Error ex.Message
}

/// Update user preferences
let updateUserPreferences (config: AppConfig) (newPreferences: UserPreferences) = async {
    let updatedConfig = { config with UserPreferences = newPreferences }
    let! result = saveConfig updatedConfig
    match result with
    | Ok _ -> return Ok updatedConfig
    | Error msg -> return Error msg
}

/// Update API key for weather service
let updateWeatherApiKey (config: AppConfig) (provider: string) (apiKey: string) = async {
    let updatedWeatherConfig = 
        match provider.ToLower() with
        | "openweather" -> { config.WeatherApi with OpenWeatherApiKey = Some apiKey; UseRealApis = true }
        | "noaa" -> { config.WeatherApi with NoaaApiKey = Some apiKey; UseRealApis = true }
        | "weatherstack" -> { config.WeatherApi with WeatherStackApiKey = Some apiKey; UseRealApis = true }
        | _ -> config.WeatherApi
    
    let updatedConfig = { config with WeatherApi = updatedWeatherConfig }
    let! result = saveConfig updatedConfig
    match result with
    | Ok _ -> 
        printfn "🔑 Updated %s API key" provider
        return Ok updatedConfig
    | Error msg -> return Error msg
}

/// Update mapping API key
let updateMappingApiKey (config: AppConfig) (apiKey: string) = async {
    let updatedMappingConfig = { config.Mapping with GoogleMapsApiKey = Some apiKey }
    let updatedConfig = { config with Mapping = updatedMappingConfig }
    let! result = saveConfig updatedConfig
    match result with
    | Ok _ -> 
        printfn "🗺️  Updated Google Maps API key"
        return Ok updatedConfig
    | Error msg -> return Error msg
}

/// Validate configuration
let validateConfig (config: AppConfig) =
    let warnings = []
    
    let warnings = 
        if config.WeatherApi.UseRealApis && config.WeatherApi.OpenWeatherApiKey.IsNone && 
           config.WeatherApi.NoaaApiKey.IsNone && config.WeatherApi.WeatherStackApiKey.IsNone then
            "No weather API keys configured but real APIs are enabled" :: warnings
        else warnings
    
    let warnings = 
        if config.Mapping.GoogleMapsApiKey.IsNone then
            "Google Maps API key not configured - using mock data" :: warnings
        else warnings
        
    let warnings = 
        if config.GasPrice.GasBuddyApiKey.IsNone then
            "Gas Buddy API key not configured - using mock data" :: warnings
        else warnings
        
    let warnings = 
        if not (Directory.Exists(config.AppSettings.DataCacheDirectory)) then
            sprintf "Cache directory does not exist: %s" config.AppSettings.DataCacheDirectory :: warnings
        else warnings
    
    warnings

/// Create necessary directories
let initializeDirectories (config: AppConfig) = async {
    try
        let directories = [
            config.AppSettings.DataCacheDirectory
            config.AppSettings.ExportDirectory
        ]
        
        for dir in directories do
            if not (Directory.Exists(dir)) then
                Directory.CreateDirectory(dir) |> ignore
                printfn "📁 Created directory: %s" dir
        
        return Ok ()
    with
    | ex ->
        printfn "❌ Error creating directories: %s" ex.Message
        return Error ex.Message
}

/// Export configuration to file (for backup/sharing)
let exportConfig (config: AppConfig) (filePath: string) = async {
    try
        // Remove sensitive information before export
        let sanitizedConfig = {
            config with
                WeatherApi = { 
                    config.WeatherApi with 
                        OpenWeatherApiKey = if config.WeatherApi.OpenWeatherApiKey.IsSome then Some "***CONFIGURED***" else None
                        NoaaApiKey = if config.WeatherApi.NoaaApiKey.IsSome then Some "***CONFIGURED***" else None
                        WeatherStackApiKey = if config.WeatherApi.WeatherStackApiKey.IsSome then Some "***CONFIGURED***" else None 
                }
                Mapping = { 
                    config.Mapping with 
                        GoogleMapsApiKey = if config.Mapping.GoogleMapsApiKey.IsSome then Some "***CONFIGURED***" else None 
                }
                GasPrice = { 
                    config.GasPrice with 
                        GasBuddyApiKey = if config.GasPrice.GasBuddyApiKey.IsSome then Some "***CONFIGURED***" else None 
                }
        }
        
        let json = JsonConvert.SerializeObject(sanitizedConfig, Formatting.Indented)
        File.WriteAllText(filePath, json)
        printfn "📤 Configuration exported to: %s" filePath
        return Ok ()
    with
    | ex ->
        printfn "❌ Error exporting configuration: %s" ex.Message
        return Error ex.Message
}

/// Print configuration summary
let printConfigSummary (config: AppConfig) =
    printfn ""
    printfn "⚙️  Configuration Summary:"
    printfn "   Version: %s" config.Version
    printfn "   Last Updated: %s" (config.LastUpdated.ToString("yyyy-MM-dd HH:mm:ss"))
    printfn ""
    printfn "🌤️  Weather API:"
    printfn "   Real APIs Enabled: %b" config.WeatherApi.UseRealApis
    printfn "   OpenWeather: %s" (if config.WeatherApi.OpenWeatherApiKey.IsSome then "✅ Configured" else "❌ Not configured")
    printfn "   NOAA: %s" (if config.WeatherApi.NoaaApiKey.IsSome then "✅ Configured" else "❌ Not configured")
    printfn "   Cache Expiration: %d minutes" config.WeatherApi.CacheExpirationMinutes
    printfn ""
    printfn "🗺️  Mapping:"
    printfn "   Google Maps: %s" (if config.Mapping.GoogleMapsApiKey.IsSome then "✅ Configured" else "❌ Not configured")
    printfn "   Distance Unit: %s" config.Mapping.DistanceUnit
    printfn "   Default Provider: %s" config.Mapping.DefaultRoutingProvider
    printfn ""
    printfn "⛽ Gas Prices:"
    printfn "   Gas Buddy API: %s" (if config.GasPrice.GasBuddyApiKey.IsSome then "✅ Configured" else "❌ Not configured")
    printfn "   Search Radius: %.1f km" config.GasPrice.DefaultSearchRadiusKm
    printfn "   Max Stations: %d" config.GasPrice.MaxStationsToSearch
    printfn ""
    printfn "👤 User Preferences:"
    printfn "   Temperature Range: %.1f°C - %.1f°C" config.UserPreferences.PreferredTemperatureRange.MinCelsius config.UserPreferences.PreferredTemperatureRange.MaxCelsius
    printfn "   Avoid Rain: %b" config.UserPreferences.AvoidRainByDefault
    printfn "   Max Driving Hours: %d hours/day" config.UserPreferences.MaxDrivingHoursPerDay
    printfn "   Preferred Departure: %s" (config.UserPreferences.PreferredDepartureTime.ToString(@"hh\:mm"))
    printfn ""
    printfn "📁 Directories:"
    printfn "   Cache: %s" config.AppSettings.DataCacheDirectory
    printfn "   Exports: %s" config.AppSettings.ExportDirectory
    printfn ""
