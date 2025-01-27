module Users

open Shared
open Saturn
open Giraffe
open System.Text.RegularExpressions

// Email validation function
let isValidEmail email = 
    let pattern = @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$"
    if System.String.IsNullOrWhiteSpace(email) then false
    else 
        let email = email.Trim()
        not (email.StartsWith(".")) && // Don't allow starting with dot
        not (email.EndsWith(".")) &&   // Don't allow ending with dot
        Regex.IsMatch(email, pattern)

// Database user type
type UserDb = {
    Email: string
    PasswordHash: string
    Salt: string
}

module Storage =
    let users = ResizeArray<UserDb>()

    let registerUser (user: User) =
        if not (isValidEmail user.Email) then
            Error "Invalid email format"
        elif users |> Seq.exists (fun u -> u.Email = user.Email) then
            Error "Email already registered"
        else
            let (hash, salt) = Security.createPasswordHash user.Password
            let userDb = {
                Email = user.Email
                PasswordHash = hash
                Salt = salt
            }
            users.Add userDb
            Ok()

    let loginUser (email: string) (password: string) =
        if not (isValidEmail email) then
            Error "Invalid email format"
        else
            users 
            |> Seq.tryFind (fun u -> u.Email = email)
            |> function
                | Some user -> 
                    if Security.verifyPassword password user.PasswordHash user.Salt then
                        let token = Security.generateToken user.Email
                        Ok { Email = user.Email; Password = ""; Token = Some token }
                    else
                        Error "Invalid email or password"
                | None -> Error "Invalid email or password"

let userRouter = router {
    post "/register" (fun next ctx ->
        task {
            let! user = ctx.BindJsonAsync<User>()
            match Storage.registerUser user with
            | Ok() -> 
                let result = {| Ok = { user with Password = ""; Token = None } |}
                return! json result next ctx
            | Error e -> 
                let result = {| Error = e |}
                return! json result next ctx
        })
        
    post "/login" (fun next ctx ->
        task {
            let! (email, password) = ctx.BindJsonAsync<string * string>()
            match Storage.loginUser email password with
            | Ok user -> 
                let result = {| Ok = user |}
                return! json result next ctx
            | Error e -> 
                let result = {| Error = e |}
                return! json result next ctx
        })
}

let userApi = router {
    forward "" userRouter
} 