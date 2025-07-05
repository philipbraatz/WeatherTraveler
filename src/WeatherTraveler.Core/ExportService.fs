module WeatherTraveler.ExportService

open System
open System.IO
open System.Text
open WeatherTraveler.Types

/// KML export options
type KmlExportOptions = {
    IncludeWeatherData: bool
    IncludeAltitudeData: bool
    RouteColor: string
    PointIconScale: float
}

/// CSV export options  
type CsvExportOptions = {
    IncludeWeatherDetails: bool
    IncludeFuelCalculations: bool
    DateTimeFormat: string
    TemperatureUnit: string // "C" or "F"
}

/// Default KML export options
let defaultKmlOptions = {
    IncludeWeatherData = true
    IncludeAltitudeData = false
    RouteColor = "7f00ffff" // Semi-transparent yellow
    PointIconScale = 1.2
}

/// Default CSV export options
let defaultCsvOptions = {
    IncludeWeatherDetails = true
    IncludeFuelCalculations = true
    DateTimeFormat = "yyyy-MM-dd HH:mm"
    TemperatureUnit = "C"
}

/// Generate KML for Google Earth export
let generateKML (route: TravelRoute) (title: string) (options: KmlExportOptions) =
    let kml = StringBuilder()
    
    // KML header
    kml.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>") |> ignore
    kml.AppendLine("<kml xmlns=\"http://www.opengis.net/kml/2.2\">") |> ignore
    kml.AppendLine("  <Document>") |> ignore
    kml.AppendLine(sprintf "    <name>%s</name>" title) |> ignore
    
    // Add route folder
    kml.AppendLine("    <Folder>") |> ignore
    kml.AppendLine("      <name>Route</name>") |> ignore
    
    // Add placemarks for each location
    route.Segments |> List.iteri (fun i segment ->
        let fromName = segment.From.Name
        let fromCoord = segment.From.Coordinate
        let toName = segment.To.Name
        let toCoord = segment.To.Coordinate
        
        // Add start point for first segment
        if i = 0 then
            kml.AppendLine("      <Placemark>") |> ignore
            kml.AppendLine(sprintf "        <name>Start: %s</name>" fromName) |> ignore
            kml.AppendLine("        <Point>") |> ignore
            kml.AppendLine(sprintf "          <coordinates>%.6f,%.6f,0</coordinates>" fromCoord.Longitude fromCoord.Latitude) |> ignore
            kml.AppendLine("        </Point>") |> ignore
            kml.AppendLine("      </Placemark>") |> ignore
        
        // Add destination point
        kml.AppendLine("      <Placemark>") |> ignore
        kml.AppendLine(sprintf "        <name>%s</name>" toName) |> ignore
        kml.AppendLine(sprintf "        <description>Distance: %.1f km</description>" segment.Distance) |> ignore
        kml.AppendLine("        <Point>") |> ignore
        kml.AppendLine(sprintf "          <coordinates>%.6f,%.6f,0</coordinates>" toCoord.Longitude toCoord.Latitude) |> ignore
        kml.AppendLine("        </Point>") |> ignore
        kml.AppendLine("      </Placemark>") |> ignore
    )
    
    kml.AppendLine("    </Folder>") |> ignore
    kml.AppendLine("  </Document>") |> ignore
    kml.AppendLine("</kml>") |> ignore
    
    kml.ToString()

/// Export route to KML file
let exportToKML (route: TravelRoute) (filePath: string) (title: string) (options: KmlExportOptions option) = async {
    let exportOptions = defaultArg options defaultKmlOptions
    let kmlContent = generateKML route title exportOptions
    
    do! File.WriteAllTextAsync(filePath, kmlContent) |> Async.AwaitTask
    return filePath
}

/// Generate CSV data for route export
let generateCSV (route: TravelRoute) (schedule: (DateTime * RouteSegment) list) (options: CsvExportOptions) =
    let csv = StringBuilder()
    
    // CSV header
    let headers = ["Date"; "From"; "To"; "Distance_km"]
    csv.AppendLine(String.concat "," headers) |> ignore
    
    // CSV data rows
    schedule |> List.iter (fun (departureTime, segment) ->
        let fromName = segment.From.Name
        let toName = segment.To.Name
        
        let data = [
            departureTime.ToString("yyyy-MM-dd")
            sprintf "\"%s\"" fromName
            sprintf "\"%s\"" toName
            sprintf "%.1f" segment.Distance
        ]
        
        csv.AppendLine(String.concat "," data) |> ignore
    )
    
    csv.ToString()

/// Export route to CSV file
let exportToCSV (route: TravelRoute) (schedule: (DateTime * RouteSegment) list) (filePath: string) (options: CsvExportOptions option) = async {
    let exportOptions = defaultArg options defaultCsvOptions
    let csvContent = generateCSV route schedule exportOptions
    
    do! File.WriteAllTextAsync(filePath, csvContent) |> Async.AwaitTask
    return filePath
}

/// Generate route summary report
let generateRouteSummaryReport (route: TravelRoute) (schedule: (DateTime * RouteSegment) list) (weatherWarnings: string list) =
    let report = StringBuilder()
    
    report.AppendLine("=== WEATHER TRAVELER ROUTE SUMMARY ===") |> ignore
    report.AppendLine(sprintf "Total Distance: %.1f km" route.TotalDistance) |> ignore
    report.AppendLine(sprintf "Estimated Fuel Cost: $%.2f" route.EstimatedFuelCost) |> ignore
    
    schedule |> List.iteri (fun i (departureTime, segment) ->
        let fromName = segment.From.Name
        let toName = segment.To.Name
        
        report.AppendLine(sprintf "%d. %s to %s (%.1f km)" (i + 1) fromName toName segment.Distance) |> ignore
    )
    
    report.ToString()

/// Export route summary to text file
let exportRouteSummary (route: TravelRoute) (schedule: (DateTime * RouteSegment) list) (weatherWarnings: string list) (filePath: string) = async {
    let summaryContent = generateRouteSummaryReport route schedule weatherWarnings
    
    do! File.WriteAllTextAsync(filePath, summaryContent) |> Async.AwaitTask
    return filePath
}
