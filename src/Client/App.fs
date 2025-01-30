module App

open Elmish
open Elmish.React
open Feliz
open Feliz.Router
open Fable.Core.JsInterop
open Browser.Types
open Browser.Dom

importSideEffects "./index.css"

#if DEBUG
open Elmish.HMR
#endif

type Page =
    | Home
    | Login
    | Register
    | Profile

let parsePath = function
    | [ ] -> Login
    | [ "login" ] -> Login
    | [ "register" ] -> Register
    | [ "profile" ] -> Profile
    | _ -> Login

let HomePage = React.functionComponent(fun () ->
    let isLoggedIn = window.localStorage.getItem("user") |> isNull |> not

    Html.div [
        Html.h1 "Home"
        Html.div [
            if isLoggedIn then
                Html.a [
                    prop.href "#/profile"
                    prop.text "Profile"
                ]
                Html.text " | "
                Html.a [
                    prop.href "#"
                    prop.onClick (fun _ ->
                        window.localStorage.removeItem("user")
                        window.location.reload()
                    )
                    prop.text "Logout"
                ]
            else
                Html.a [
                    prop.href "#/login"
                    prop.text "Login"
                ]
                Html.text " | "
                Html.a [
                    prop.href "#/register"
                    prop.text "Register"
                ]
        ]
    ]
)

[<ReactComponent>]
let App() =
    let currentUrl, updateUrl = React.useState(Router.currentUrl())
    let currentPage = parsePath currentUrl

    printfn "Current URL: %A" currentUrl
    printfn "Current Page: %A" currentPage

    React.useEffectOnce(fun () ->
        let handler = fun (e: Event) ->
            let newUrl = Router.currentUrl()
            printfn "URL changed to: %A" newUrl
            updateUrl(newUrl)
        window.addEventListener("hashchange", handler)
        React.createDisposable(fun () ->
            window.removeEventListener("hashchange", handler)
        )
    )

    Html.div [
        prop.className "container"
        prop.style [style.padding 20]
        prop.children [
            match currentPage with
            | Home ->
                printfn "Rendering Home"
                HomePage()
            | Login ->
                printfn "Rendering Login"
                Login.LoginView()
            | Register ->
                printfn "Rendering Register"
                Register.RegisterView()
            | Profile ->
                printfn "Rendering Profile"
                Profile.ProfileView()
        ]
    ]

// Start the app
Program.mkProgram (fun _ -> (), []) (fun _ _ -> (), []) (fun _ _ -> App())
#if DEBUG
|> Program.withConsoleTrace
#endif
|> Program.withReactSynchronous "elmish-app"
|> Program.run