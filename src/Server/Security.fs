module Security

open System
open System.Text
open System.Security.Claims
open System.Security.Cryptography
open Microsoft.AspNetCore.Cryptography.KeyDerivation
open System.IdentityModel.Tokens.Jwt
open Microsoft.IdentityModel.Tokens

// Password hashing functions
let private generateSalt() = 
    let salt = Array.zeroCreate<byte> 16
    use rng = RandomNumberGenerator.Create()
    rng.GetBytes(salt)
    Convert.ToBase64String(salt)

let private hashPassword (password: string) (salt: string) =
    let saltBytes = Convert.FromBase64String(salt)
    let hash = KeyDerivation.Pbkdf2(
        password = password,
        salt = saltBytes,
        prf = KeyDerivationPrf.HMACSHA256,
        iterationCount = 10000,
        numBytesRequested = 32)
    Convert.ToBase64String(hash)

let createPasswordHash (password: string) =
    let salt = generateSalt()
    let hash = hashPassword password salt
    (hash, salt)

let verifyPassword (password: string) (hash: string) (salt: string) =
    let computedHash = hashPassword password salt
    hash = computedHash

// JWT functions
let jwtSecret = "your-256-bit-secret-key-that-is-very-long-and-secure-32"  // 32 characters = 256 bits
let private key = SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret))
let private credentials = SigningCredentials(key, SecurityAlgorithms.HmacSha256)

// let secret =
//     let fi = FileInfo("./temp/token.txt")

//     if not fi.Exists then
//         let passPhrase = createPassPhrase ()

//         if not fi.Directory.Exists then
//             fi.Directory.Create()

//         File.WriteAllBytes(fi.FullName, passPhrase)

//     File.ReadAllBytes(fi.FullName) |> System.Text.Encoding.UTF8.GetString

let issuer = "rombar.io"

let generateToken (email: string) =
    let claims = [|
        Claim(JwtRegisteredClaimNames.Sub, email)
        Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
    |]
    
    let token = JwtSecurityToken(
        issuer = "safe-stack-app",
        audience = "safe-stack-app",
        claims = claims,
        expires = DateTime.UtcNow.AddHours(1.0),
        signingCredentials = credentials
    )
    
    JwtSecurityTokenHandler().WriteToken(token) 