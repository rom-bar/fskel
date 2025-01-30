module UserTests

open Expecto
open Shared
open Users
open System.Text.Json
open System.Text.Json.Serialization

// Custom converter for Result<User, string>
type ResultConverter<'T, 'TError>() =
    inherit JsonConverter<Result<'T, 'TError>>()

    override _.Read(reader: byref<Utf8JsonReader>, _: System.Type, options: JsonSerializerOptions) =
        let mutable obj = JsonDocument.Parse(reader.GetString()).RootElement
        if obj.TryGetProperty("Ok", &obj) then
            Ok(JsonSerializer.Deserialize<'T>(obj.GetRawText(), options))
        else
            let errorObj = obj.GetProperty("Error")
            Error(JsonSerializer.Deserialize<'TError>(errorObj.GetRawText(), options))

    override _.Write(writer: Utf8JsonWriter, value: Result<'T, 'TError>, options: JsonSerializerOptions) =
        writer.WriteStartObject()
        match value with
        | Ok v ->
            writer.WritePropertyName("Ok")
            JsonSerializer.Serialize(writer, v, options)
        | Error e ->
            writer.WritePropertyName("Error")
            JsonSerializer.Serialize(writer, e, options)
        writer.WriteEndObject()

let testConfig =
    let options = JsonSerializerOptions()
    options.Converters.Add(ResultConverter<User, string>())
    options

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