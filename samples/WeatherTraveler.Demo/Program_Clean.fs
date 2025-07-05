module WeatherTraveler.Demo.Program

open System

/// Demo application entry point for Weather Traveler
[<EntryPoint>]
let main argv =
    match argv with
    | [| "weather" |] ->
        printfn "🌟 Starting Weather Traveler Demo..."
        DemoProgram.demoWeatherTraveler()
        |> Async.RunSynchronously
        |> ignore
        0
    | _ ->
        printfn "Weather Traveler Demo v2.0"
        printfn "Usage:"
        printfn "  dotnet run weather - Run weather demo"
        printfn ""
        printfn "Starting weather demo by default..."
        DemoProgram.demoWeatherTraveler()
        |> Async.RunSynchronously
        |> ignore
        0
