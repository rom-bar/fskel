module Server.Tests

open Expecto

open Shared
open Server

// Combine all test lists
let all =
    testList "All" [
        UserTests.tests  // Add the UserTests
    ]

[<EntryPoint>]
let main _ = runTestsWithCLIArgs [] [||] all