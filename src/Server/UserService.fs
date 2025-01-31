module UserService

open Shared
open Users

let userApi = {
    login = fun (email, password) -> async {
        match Storage.loginUser email password with
        | Ok user ->
            return {
                success = true
                message = "Login successful"
                data = Some user
                error = None
            }
        | Error msg ->
            return {
                success = false
                message = msg
                data = None
                error = Some { code = 401; details = msg }
            }
    }
    register = fun user -> async {
        match Storage.registerUser user with
        | Ok() ->
            let registeredUser = {
                Email = user.Email
                Password = ""  // Clear password for security
                Token = None
            }
            return {
                success = true
                message = "Registration successful"
                data = Some registeredUser
                error = None
            }
        | Error msg ->
            return {
                success = false
                message = msg
                data = None
                error = Some { code = 400; details = msg }
            }
    }
}