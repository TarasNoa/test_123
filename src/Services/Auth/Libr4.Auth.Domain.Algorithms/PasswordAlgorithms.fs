namespace Libr4.Auth.Domain.Algorithms

open System
open System.Security.Cryptography

[<CLIMutable>]
type PasswordPolicy =
    { MinLength: int
      RequireUppercase: bool
      RequireLowercase: bool
      RequireDigits: bool
      RequireSpecialChars: bool }

module PasswordAlgorithms =

    let defaultPolicy =
        { MinLength = 8
          RequireUppercase = true
          RequireLowercase = true
          RequireDigits = true
          RequireSpecialChars = true }

    let validatePassword (policy: PasswordPolicy) (password: string) =
        password.Length >= policy.MinLength &&
        (not policy.RequireUppercase || password |> Seq.exists Char.IsUpper) &&
        (not policy.RequireLowercase || password |> Seq.exists Char.IsLower) &&
        (not policy.RequireDigits || password |> Seq.exists Char.IsDigit) &&
        (not policy.RequireSpecialChars || password |> Seq.exists (fun c -> not (Char.IsLetterOrDigit c)))

    let generateSalt () =
        let bytes = Array.zeroCreate<byte> 16
        RandomNumberGenerator.Fill(bytes)
        Convert.ToBase64String(bytes)

    let hashPassword (password: string) (salt: string) =
        use pbkdf2 = new Rfc2898DeriveBytes(password, Convert.FromBase64String(salt), 10000, HashAlgorithmName.SHA256)
        let hash = pbkdf2.GetBytes(32)
        Convert.ToBase64String(hash)

    let verifyPassword (password: string) (salt: string) (hash: string) =
        hashPassword password salt = hash

    // Strength scoring algorithm
    let calculatePasswordStrength (password: string) =
        let lengthScore = min (password.Length * 2) 40
        let varietyScore =
            [ Char.IsUpper, Char.IsLower, Char.IsDigit, (fun c -> not (Char.IsLetterOrDigit c)) ]
            |> List.sumBy (fun pred -> if password |> Seq.exists pred then 15 else 0)
        lengthScore + varietyScore