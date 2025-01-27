module Server.Tests

open Expecto

open Shared
open Server

let server =
    testList "Server" [
        testCase "Adding valid Todo"
        <| fun _ ->
            let validTodo = Todo.create "TODO"
            let expectedResult = Ok()

            let result = Storage.addTodo validTodo

            Expect.equal result expectedResult "Result should be ok"
            Expect.contains Storage.todos validTodo "Storage should contain new todo"
    ]

// Combine all test lists
let all = 
    testList "All" [ 
        Shared.Tests.shared
        server
        UserTests.tests  // Add the UserTests
    ]

[<EntryPoint>]
let main _ = runTestsWithCLIArgs [] [||] all