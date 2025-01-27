module Server

open SAFE
open Saturn
open Shared
open Users
open Security
open Microsoft.AspNetCore.Authentication.JwtBearer
open Microsoft.IdentityModel.Tokens

module Storage =
    let todos =
        ResizeArray [
            Todo.create "Create new SAFE project"
            Todo.create "Write your app"
            Todo.create "Ship it!!!"
        ]

    let users = ResizeArray<User>()

    let addTodo todo =
        if Todo.isValid todo.Description then
            todos.Add todo
            Ok()
        else
            Error "Invalid todo"

    let registerUser (user: User) =
        if users |> Seq.exists (fun u -> u.Email = user.Email) then
            Error "Email already registered"
        else
            users.Add user
            Ok()

    let loginUser (email: string) (password: string) =
        users 
        |> Seq.tryFind (fun u -> u.Email = email && u.Password = password)
        |> function
            | Some user -> Ok user
            | None -> Error "Invalid email or password"

let todosApi ctx = {
    getTodos = fun () -> async { return Storage.todos |> List.ofSeq }
    addTodo =
        fun todo -> async {
            return
                match Storage.addTodo todo with
                | Ok() -> Storage.todos |> List.ofSeq
                | Error e -> failwith e
        }
}

let webApp = 
    router {
        forward "/api/todos" (Api.make todosApi)
        forward "/api/users" userApi
    }

let app = application {
    use_router webApp
    memory_cache
    use_static "public"
    use_gzip
    use_jwt_authentication Security.jwtSecret Security.issuer
}

[<EntryPoint>]
let main _ =
    run app
    0