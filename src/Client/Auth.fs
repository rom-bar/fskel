module Auth

open Shared
open Fable.Remoting.Client
open Fable.Core
open Fable.Core.JsInterop
open Browser.Types

// API definition
let userApi =
    Remoting.createApi()
    |> Remoting.withRouteBuilder Route.builder
    |> Remoting.buildProxy<IUserApi>

// Types for form state
type LoginForm = {
    Email: string
    Password: string
}

type RegisterForm = {
    Email: string
    Password: string
    ConfirmPassword: string
}

let emptyLoginForm = {
    Email = ""
    Password = ""
}

let emptyRegisterForm = {
    Email = ""
    Password = ""
    ConfirmPassword = ""
} 