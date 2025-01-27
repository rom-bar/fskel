module UserTests

open Expecto
open Shared
open Server
open Users
open System
open System.Net.Http
open System.Text.Json
open System.Text
open Microsoft.AspNetCore
open Microsoft.AspNetCore.Hosting
open Microsoft.AspNetCore.TestHost
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.DependencyInjection
open Saturn
open Giraffe
open System.Text.Json.Serialization

// Custom JSON converter for F# Result type
type ResultConverter<'T, 'TError>() =
    inherit JsonConverter<Result<'T, 'TError>>()
    
    override _.Read(reader: byref<Utf8JsonReader>, _: Type, options: JsonSerializerOptions) =
        // Skip to start object
        if reader.TokenType <> JsonTokenType.StartObject then
            reader.Read() |> ignore
        
        // Read property name (should be "ok" or "error")
        reader.Read() |> ignore
        let propertyName = reader.GetString().ToLowerInvariant()
        
        // Read property value
        reader.Read() |> ignore
        
        match propertyName with
        | "ok" ->
            let value = JsonSerializer.Deserialize<'T>(&reader, options)
            reader.Read() |> ignore  // Read EndObject
            Ok value
        | "error" ->
            let error = JsonSerializer.Deserialize<'TError>(&reader, options)
            reader.Read() |> ignore  // Read EndObject
            Error error
        | _ -> failwith $"Invalid Result type JSON format. Property name was: {propertyName}"

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

// Custom JSON converter for F# option type
type OptionConverter<'T>() =
    inherit JsonConverter<'T option>()
    
    override _.Read(reader: byref<Utf8JsonReader>, _: Type, options: JsonSerializerOptions) =
        if reader.TokenType = JsonTokenType.Null then
            None
        else
            match reader.TokenType with
            | JsonTokenType.StartObject ->
                // Skip the "case" property
                reader.Read() |> ignore  // Move to "case" property name
                reader.Read() |> ignore  // Move to "case" value ("Some")
                reader.Read() |> ignore  // Move to "fields" property name
                reader.Read() |> ignore  // Move to start of fields array
                reader.Read() |> ignore  // Move to the actual value
                let value = JsonSerializer.Deserialize<'T>(&reader, options)
                reader.Read() |> ignore  // Move past the array end
                reader.Read() |> ignore  // Move past the object end
                Some value
            | _ -> failwith "Invalid option type JSON format"

    override _.Write(writer: Utf8JsonWriter, value: 'T option, options: JsonSerializerOptions) =
        match value with
        | None -> writer.WriteNullValue()
        | Some v -> JsonSerializer.Serialize(writer, v, options)

let jsonSerializerOptions = 
    let options = JsonSerializerOptions(PropertyNameCaseInsensitive = true)
    options.Converters.Add(ResultConverter<User, string>())
    options.Converters.Add(OptionConverter<string>())  // Add converter for string option
    options

let createTestServer() =
    let app = Server.webApp
    let host = 
        WebHostBuilder()
            .UseTestServer()
            .ConfigureServices(fun services ->
                services.AddGiraffe() |> ignore  // Add Giraffe services
            )
            .Configure(fun builder -> 
                builder.UseGiraffe(app)
            )
            .Build()
    host.Start()
    host.GetTestClient()

let createHttpClient() =
    createTestServer()

let postJson<'T> (client: HttpClient) (url: string) (data: obj) =
    let content = JsonSerializer.Serialize(data, jsonSerializerOptions)
    printfn "Request Content: %s" content  // Debug: print request content
    let stringContent = new StringContent(content, Encoding.UTF8, "application/json")
    let response = client.PostAsync(url, stringContent).Result
    let responseContent = response.Content.ReadAsStringAsync().Result
    printfn "Response Status: %A" response.StatusCode
    printfn "Response Content: %s" responseContent
    response.StatusCode, 
    if System.String.IsNullOrEmpty(responseContent) then 
        None 
    else 
        Some(JsonSerializer.Deserialize<'T>(responseContent, jsonSerializerOptions))

[<Tests>]
let tests =
    testList "User Authentication Integration Tests" [
        testCase "Register and Login - Success Flow" <| fun _ ->
            // Arrange
            use client = createHttpClient()
            let testUser = {
                Email = "test22@example.com"
                Password = "password123"
                Token = None
            }

            // Act - Register
            let registerStatus, registerResult = 
                postJson<Result<User, string>> client "/api/users/register" testUser

            // Assert - Register
            Expect.equal registerStatus System.Net.HttpStatusCode.OK "Should return OK status"
            match registerResult with
            | Some (Ok user) ->
                Expect.equal user.Email testUser.Email "Should return correct email"
                Expect.equal user.Password "" "Should not return password"
                Expect.equal user.Token None "Should not have token yet"
            | _ -> failtest "Registration should succeed"

            // Act - Login
            let loginStatus, loginResult = 
                postJson<Result<User, string>> client "/api/users/login" (testUser.Email, testUser.Password)

            // Assert - Login
            Expect.equal loginStatus System.Net.HttpStatusCode.OK "Should return OK status"
            match loginResult with
            | Some (Ok user) ->
                Expect.equal user.Email testUser.Email "Should return correct email"
                Expect.equal user.Password "" "Should not return password"
                Expect.isSome user.Token "Should have token"
            | _ -> failtest "Login should succeed"

        testCase "Register Duplicate Email - Should Fail" <| fun _ ->
            // Arrange
            use client = createHttpClient()
            let testUser = {
                Email = "duplicate@example.com"
                Password = "password123"
                Token = None
            }

            // Act - First Registration
            let _, firstRegisterResult = 
                postJson<Result<User, string>> client "/api/users/register" testUser

            // Assert - First Registration
            match firstRegisterResult with
            | Some (Ok _) -> ()
            | _ -> failtest "First registration should succeed"

            // Act - Second Registration
            let secondRegisterStatus, secondRegisterResult = 
                postJson<Result<User, string>> client "/api/users/register" testUser

            // Assert - Second Registration
            Expect.equal secondRegisterStatus System.Net.HttpStatusCode.OK "Should return OK status"
            match secondRegisterResult with
            | Some (Error msg) ->
                Expect.stringContains msg "already registered" "Should return already registered error"
            | _ -> failtest "Second registration should fail"

        testCase "Login Without Registration - Should Fail" <| fun _ ->
            // Arrange
            use client = createHttpClient()
            let nonExistentUser = ("nonexistent@example.com", "password123")

            // Act
            let loginStatus, loginResult = 
                postJson<Result<User, string>> client "/api/users/login" nonExistentUser

            // Assert
            Expect.equal loginStatus System.Net.HttpStatusCode.OK "Should return OK status"
            match loginResult with
            | Some (Error msg) ->
                Expect.stringContains msg "Invalid email or password" "Should return invalid credentials error"
            | _ -> failtest "Login should fail for non-existent user"

        testCase "Register with Invalid Email Format" <| fun _ ->
            // Arrange
            use client = createHttpClient()
            let invalidEmailUser = {
                Email = "invalid.email"  // Missing @ and domain
                Password = "password123"
                Token = None
            }

            // Act
            let registerStatus, registerResult = 
                postJson<Result<User, string>> client "/api/users/register" invalidEmailUser

            // Assert
            Expect.equal registerStatus System.Net.HttpStatusCode.OK "Should return OK status"
            match registerResult with
            | Some (Error msg) ->
                Expect.stringContains msg "Invalid email format" "Should return invalid email error"
            | _ -> failtest "Registration should fail with invalid email"

        testCase "Login with Invalid Email Format" <| fun _ ->
            // Arrange
            use client = createHttpClient()
            let invalidEmail = "not.an.email"
            let password = "password123"

            // Act
            let loginStatus, loginResult = 
                postJson<Result<User, string>> client "/api/users/login" (invalidEmail, password)

            // Assert
            Expect.equal loginStatus System.Net.HttpStatusCode.OK "Should return OK status"
            match loginResult with
            | Some (Error msg) ->
                Expect.stringContains msg "Invalid email format" "Should return invalid email error"
            | _ -> failtest "Login should fail with invalid email"

        testCase "Register with Various Invalid Email Formats" <| fun _ ->
            // Arrange
            use client = createHttpClient()
            let invalidEmails = [
                ""                      // Empty
                "noatsign.com"         // Missing @
                "@nodomain"            // Missing domain
                "spaces in@email.com"   // Contains spaces
                ".starts@with.dot"     // Starts with dot
                "multiple@@ats.com"    // Multiple @ symbols
            ]

            // Act & Assert
            for invalidEmail in invalidEmails do
                let user = {
                    Email = invalidEmail
                    Password = "password123"
                    Token = None
                }
                let registerStatus, registerResult = 
                    postJson<Result<User, string>> client "/api/users/register" user

                Expect.equal registerStatus System.Net.HttpStatusCode.OK "Should return OK status"
                match registerResult with
                | Some (Error msg) ->
                    Expect.stringContains msg "Invalid email format" 
                        $"Should reject invalid email format: {invalidEmail}"
                | _ -> failtest $"Registration should fail for invalid email: {invalidEmail}"
    ] 