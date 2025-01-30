module Client.Tests

open Fable.Mocha

let client = testList "Client" [
    testCase "Login works" <| fun _ ->
        Expect.equal true true "It works!"
]

let all =
    testList "All"
        [
            client
        ]

[<EntryPoint>]
let main _ = Mocha.runTests all