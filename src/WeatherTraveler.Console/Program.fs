open System
open WeatherTraveler.InteractivePreferenceManager

/// Console application entry point for Weather Traveler
[<EntryPoint>]
let main argv =
    match argv with
    | [| "interactive" |] ->
        printfn "🎛️ Starting interactive preference manager..."
        runInteractivePreferenceManager()
        |> Async.RunSynchronously
        |> ignore
        0
    | _ ->
        printfn "Weather Traveler Console v2.0"
        printfn "Usage:"
        printfn "  dotnet run interactive - Start interactive preference manager"
        printfn ""
        printfn "Starting interactive preference manager by default..."
        runInteractivePreferenceManager()
        |> Async.RunSynchronously
        |> ignore
        0
