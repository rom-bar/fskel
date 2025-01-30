namespace Shared

type User = {
    Email: string
    Password: string
    Token: string option
}

type IUserApi = {
    login: string * string -> Async<Result<User, string>>
    register: User -> Async<Result<User, string>>
}

module Route =
    let builder typeName methodName =
        sprintf "/api/%s/%s" typeName methodName