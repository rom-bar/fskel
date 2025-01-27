namespace Shared

open System

type Todo = { Id: Guid; Description: string }

module Todo =
    let isValid (description: string) =
        String.IsNullOrWhiteSpace description |> not

    let create (description: string) = {
        Id = Guid.NewGuid()
        Description = description
    }

type ITodosApi = {
    getTodos: unit -> Async<Todo list>
    addTodo: Todo -> Async<Todo list>
}

type User = {
    Email: string
    Password: string
    Token: string option
}

// type IAuthApi = {
//     login: string * string -> Async<User>
//     register: User -> Async<User>
// }

type IUserApi = {
    login: string * string -> Async<Result<User, string>>
    register: User -> Async<Result<User, string>>
}

module Route =
    let builder typeName methodName =
        sprintf "/api/%s/%s" typeName methodName