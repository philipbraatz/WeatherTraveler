namespace WeatherTraveler.Tests

open System
open Xunit

module BasicTests =
    [<Fact>]
    let ``Basic test passes`` () =
        Assert.True(true)
        
    [<Fact>]
    let ``Temperature range test`` () =
        let minTemp = 15.0
        let maxTemp = 25.0
        Assert.True(minTemp < maxTemp)
