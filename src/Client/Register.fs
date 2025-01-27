module Register

open Feliz
open Feliz.Bulma
open Auth
open Browser.Types
open Shared
open Fable.Core
open Fable.Core.JsInterop
open Browser.Dom

type State = {
    Form: RegisterForm
    IsLoading: bool
    Error: string option
}

type Msg =
    | SetEmail of string
    | SetPassword of string
    | SetConfirmPassword of string
    | SubmitForm
    | RegisterSuccess of User
    | RegisterError of string

let init() = {
    Form = emptyRegisterForm
    IsLoading = false
    Error = None
}

let update (msg: Msg) (state: State) =
    match msg with
    | SetEmail email ->
        { state with Form = { state.Form with Email = email } }
    | SetPassword password ->
        { state with Form = { state.Form with Password = password } }
    | SetConfirmPassword password ->
        { state with Form = { state.Form with ConfirmPassword = password } }
    | SubmitForm ->
        { state with IsLoading = true; Error = None }
    | RegisterSuccess user ->
        { state with IsLoading = false }
    | RegisterError error ->
        { state with IsLoading = false; Error = Some error }

[<ReactComponent>]
let RegisterView() =
    let state, setState = React.useState(init)

    let handleSubmit (e: Event) =
        e.preventDefault()
        if state.Form.Password <> state.Form.ConfirmPassword then
            setState { state with Error = Some "Passwords do not match" }
        else
            async {
                setState { state with IsLoading = true; Error = None }
                try
                    let user = {
                        Email = state.Form.Email
                        Password = state.Form.Password
                        Token = None
                    }
                    let! result = userApi.register user
                    printfn "Registration result: %A" result
                    match result with
                    | Ok registeredUser ->
                        printfn "Registration successful: %A" registeredUser
                        setState { state with IsLoading = false; Error = None }
                        match registeredUser.Token with
                        | Some token -> 
                            ()
                        | None -> ()
                        window.location.href <- "#/login"
                    | Error msg ->
                        printfn "Registration error: %s" msg
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
                    prop.text "Register"
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

                // Confirm Password field
                Bulma.field.div [
                    Bulma.label "Confirm Password"
                    Bulma.control.div [
                        Bulma.input.password [
                            prop.value state.Form.ConfirmPassword
                            prop.onChange (fun (v: string) -> 
                                setState { state with Form = { state.Form with ConfirmPassword = v } }
                            )
                            prop.placeholder "Confirm your password"
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
                            prop.text (if state.IsLoading then "Loading..." else "Register")
                        ]
                    ]
                ]
            ]
        ]
    ]