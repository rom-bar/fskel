module UserTests

open Expecto
open Shared
open Users

let tests = testList "User Authentication Integration Tests" [
    test "Register with Valid Email" {
        let user = {
            Email = "test@example.com"
            Password = "password123"
            Token = None
        }

        let result = Storage.registerUser user
        Expect.isOk result "Registration should succeed"
    }

    test "Register with Invalid Email Format" {
        let user = {
            Email = "invalid-email"
            Password = "password123"
            Token = None
        }

        let result = Storage.registerUser user
        Expect.isError result "Registration should fail"
        match result with
        | Error msg -> Expect.equal msg "Invalid email format" "Should return invalid email message"
        | Ok _ -> failtest "Should not succeed"
    }

    test "Login with Valid Credentials" {
        // First register a user
        let email = "test2@example.com"
        let password = "password123"
        let user = {
            Email = email
            Password = password
            Token = None
        }

        let registerResult = Storage.registerUser user
        Expect.isOk registerResult "Registration should succeed"

        // Then try to login
        let loginResult = Storage.loginUser email password
        Expect.isOk loginResult "Login should succeed"

        match loginResult with
        | Ok loggedInUser ->
            Expect.equal loggedInUser.Email email "Email should match"
            Expect.isSome loggedInUser.Token "Token should be present"
        | Error _ -> failtest "Login should not fail"
    }

    test "Login with Invalid Password" {
        let email = "test3@example.com"
        let password = "password123"
        let user = {
            Email = email
            Password = password
            Token = None
        }

        let registerResult = Storage.registerUser user
        Expect.isOk registerResult "Registration should succeed"

        let loginResult = Storage.loginUser email "wrongpassword"
        Expect.isError loginResult "Login should fail"

        match loginResult with
        | Error msg -> Expect.equal msg "Invalid email or password" "Should return invalid credentials message"
        | Ok _ -> failtest "Should not succeed"
    }
]