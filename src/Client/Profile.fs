module Profile

open Feliz
open Feliz.Bulma
open Browser.Dom
open Shared
open Fable.Core
open Fable.Core.JsInterop
open Fable.Core.JS

type State = {
    User: User option
}

[<ReactComponent>]
let ProfileView() =
    printfn "ProfileView mounted"
    let state, setState = React.useState({ User = None })

    // Load user data from localStorage on component mount
    React.useEffectOnce(fun () ->
        printfn "useEffect running"
        match window.localStorage.getItem("user") with
        | null -> 
            printfn "No user data found"
        | userJson -> 
            printfn "User data found: %s" userJson
            let user = JSON.parse(userJson) |> unbox<User>
            setState { User = Some user }
    )

    printfn "Current state: %A" state

    match state.User with
    | None -> 
        Html.div [
            prop.className "container"
            prop.style [style.padding 20]
            prop.children [
                Bulma.notification [
                    color.isDanger
                    prop.text "Please login to view your profile"
                ]
            ]
        ]
    | Some user ->
        Html.div [
            prop.className "container"
            prop.style [style.padding 20]
            prop.children [
                Bulma.box [
                    Bulma.title.h1 [
                        prop.text "Profile"
                    ]
                    
                    Bulma.field.div [
                        Bulma.label "Email"
                        Html.p user.Email
                    ]

                    Bulma.field.div [
                        Bulma.button.button [
                            color.isDanger
                            prop.onClick (fun _ -> 
                                window.localStorage.removeItem("user")
                                window.location.href <- "#/"
                            )
                            prop.text "Logout"
                        ]
                    ]
                ]
            ]
        ] 