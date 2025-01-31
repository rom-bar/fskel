namespace Shared

type User = {
    Email: string
    Password: string
    Token: string option
}

type ApiError = {
    code: int
    details: string
}

type ApiResponse<'T> = {
    success: bool
    message: string
    data: 'T option
    error: ApiError option
}

type IUserApi = {
    login: string * string -> Async<ApiResponse<User>>
    register: User -> Async<ApiResponse<User>>
}

module Route =
    let builder typeName methodName =
        sprintf "/api/%s/%s" typeName methodName