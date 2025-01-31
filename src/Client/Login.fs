module Login

open Feliz
open Feliz.Bulma
open Auth
open Browser.Types
open Shared
open Browser.Dom
open Fable.Core
open Fable.Core.JsInterop
open Fable.Core.JS

type State = {
    Form: LoginForm
    IsLoading: bool
    Error: string option
}

type Msg =
    | SetEmail of string
    | SetPassword of string
    | SubmitForm
    | LoginSuccess of User
    | LoginError of string

let init() = {
    Form = emptyLoginForm
    IsLoading = false
    Error = None
}

let update (msg: Msg) (state: State) =
    match msg with
    | SetEmail email ->
        { state with Form = { state.Form with Email = email } }
    | SetPassword password ->
        { state with Form = { state.Form with Password = password } }
    | SubmitForm ->
        { state with IsLoading = true; Error = None }
    | LoginSuccess user ->
        { state with IsLoading = false }
    | LoginError error ->
        { state with IsLoading = false; Error = Some error }

[<ReactComponent>]
let LoginView() =
    let state, setState = React.useState(init)

    let handleSubmit (e: Event) =
        e.preventDefault()
        async {
            setState { state with IsLoading = true; Error = None }
            try
                let! response = userApi.login (state.Form.Email, state.Form.Password)
                match response with
                | { success = true; data = Some user } ->
                    setState { state with IsLoading = false }
                    window.localStorage.setItem("user", JSON.stringify(user))
                    window.location.href <- "#/profile"
                | { success = false; message = msg } ->
                    setState { state with IsLoading = false; Error = Some msg }
                | { success = _; message = msg } ->
                    setState { state with IsLoading = false; Error = Some msg }
            with e ->
                setState { state with IsLoading = false; Error = Some e.Message }
        } |> Async.StartImmediate

    Html.form [
        prop.onSubmit handleSubmit
        prop.children [
            Bulma.box [
                // Title
                Bulma.title.h1 [
                    prop.text "Login"
                ]

                // Email field
                Bulma.field.div [
                    Bulma.label "Email"
                    Bulma.control.div [
                        Bulma.input.email [
                            prop.value state.Form.Email
                            prop.onChange (fun (v: string) ->
                                setState { state with Form = { state.Form with Email = v } }
                            )
                            prop.placeholder "Enter your email"
                            prop.required true
                        ]
                    ]
                ]

                // Password field
                Bulma.field.div [
                    Bulma.label "Password"
                    Bulma.control.div [
                        Bulma.input.password [
                            prop.value state.Form.Password
                            prop.onChange (fun (v: string) ->
                                setState { state with Form = { state.Form with Password = v } }
                            )
                            prop.placeholder "Enter your password"
                            prop.required true
                        ]
                    ]
                ]

                // Error message
                if state.Error.IsSome then
                    Bulma.notification [
                        color.isDanger
                        prop.text state.Error.Value
                    ]

                // Submit button
                Bulma.field.div [
                    Bulma.control.div [
                        Bulma.button.button [
                            color.isPrimary
                            prop.type' "submit"
                            prop.disabled state.IsLoading
                            prop.text (if state.IsLoading then "Loading..." else "Login")
                        ]
                    ]
                ]

                // Registration link
                Bulma.field.div [
                    prop.style [
                        style.marginTop 20
                        style.textAlign.center
                    ]
                    prop.children [
                        Bulma.text.p [
                            prop.children [
                                Html.text "Don't have an account yet? "
                                Html.a [
                                    prop.href "#/register"
                                    prop.text "Register here"
                                    prop.style [
                                        style.color "#485fc7"  // Bulma's default link color
                                        style.textDecoration.underline
                                    ]
                                ]
                            ]
                        ]
                    ]
                ]
            ]
        ]
    ]