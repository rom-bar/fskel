module UserService

open Shared
open Users

let userApi = {
    login = fun (email, password) -> async {
        match Storage.loginUser email password with
        | Ok user -> return Ok user
        | Error msg -> return Error msg
    }
    register = fun user -> async {
        match Storage.registerUser user with
        | Ok() -> 
            let registeredUser = {
                Email = user.Email
                Password = ""  // Clear password for security
                Token = None
            }
            return Ok registeredUser
        | Error msg -> return Error msg
    }
} 