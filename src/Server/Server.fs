module Server

open SAFE
open Saturn
open Shared
open Users
open Security
open Microsoft.AspNetCore.Authentication.JwtBearer
open Microsoft.IdentityModel.Tokens
open Fable.Remoting.Server
open Fable.Remoting.Giraffe

let webApp =
    Remoting.createApi()
    |> Remoting.withRouteBuilder Route.builder
    |> Remoting.fromValue UserService.userApi
    |> Remoting.buildHttpHandler

let app =
    application {
        url "http://0.0.0.0:5000"
        use_router webApp
        memory_cache
        use_static "public"
        use_gzip
    }

[<EntryPoint>]
let main _ =
    run app
    0