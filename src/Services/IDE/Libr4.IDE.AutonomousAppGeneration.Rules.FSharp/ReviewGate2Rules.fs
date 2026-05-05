module Libr4.IDE.AutonomousAppGeneration.Rules.ReviewGate2

open Domain

/// Built-in rules that complement ReviewGate2 substring checks.
/// These are the F# implementations of the checklist items; each one can
/// be consulted by the C# ReviewGate2Service as a typed alternative to
/// raw substring scanning.

// ── Cross-stack rules ────────────────────────────────────────────────────

/// error_handling: at least one file must reference a structured error pattern.
let errorHandlingRule : Rule =
    Cross(
        checkId = "error_handling",
        evaluate = fun files ->
            let tokens = [ "error.code"; "error_code"; "ErrorCode"; "\"code\"" ]
            let found = tokens |> List.exists (fun t -> Rule.anyFileContains t files)
            if found then Pass("error_handling", Some "error code pattern found")
            else Fail("error_handling", "no_error_code_pattern", Some "Add error.code field to error responses")
    )

/// observability_baseline: structured JSON logging and correlation IDs required.
let observabilityRule : Rule =
    Cross(
        checkId = "observability_baseline",
        evaluate = fun files ->
            let hasLogger = Rule.anyFileContains "logger" files || Rule.anyFileContains "ILogger" files
            let hasCorrelation =
                Rule.anyFileContains "x-request-id" files ||
                Rule.anyFileContains "correlation" files ||
                Rule.anyFileContains "CorrelationId" files
            if hasLogger && hasCorrelation then
                Pass("observability_baseline", Some "logger + correlation found")
            else
                let reason =
                    match hasLogger, hasCorrelation with
                    | false, _ -> "no_logger"
                    | _, false -> "no_correlation_id"
                    | _ -> "incomplete_observability"
                Fail("observability_baseline", reason, Some "Add structured logging + x-request-id propagation")
    )

/// semantic_security: JWT or encryption must appear in non-trivial locations.
let securityRule : Rule =
    Cross(
        checkId = "semantic_security",
        evaluate = fun files ->
            let codeFiles =
                files |> List.filter (fun f ->
                    not (f.RelativePath.EndsWith(".md", System.StringComparison.OrdinalIgnoreCase) &&
                         f.RelativePath.Contains("README", System.StringComparison.OrdinalIgnoreCase)))
            let hasJwt = Rule.anyFileContains "jwt" codeFiles || Rule.anyFileContains "Bearer" codeFiles
            let hasEncryption =
                Rule.anyFileContains "encrypt" codeFiles ||
                Rule.anyFileContains "AES" codeFiles ||
                Rule.anyFileContains "RSA" codeFiles
            if hasJwt || hasEncryption then
                Pass("semantic_security", Some "security token/encryption found in code")
            else
                Fail("semantic_security", "no_security_primitives_in_code",
                     Some "Implement JWT auth or encryption in application code (not just README)")
    )

// ── Stack-specific rules ─────────────────────────────────────────────────

/// .NET: AddAuthentication + AddJwtBearer wiring check.
let dotNetAuthRule : Rule =
    StackSpecific(
        stack = DotNet,
        checkId = "auth_implementation",
        evaluate = fun files ->
            let csFiles = files |> List.filter (fun f ->
                f.RelativePath.EndsWith(".cs", System.StringComparison.OrdinalIgnoreCase))
            let hasAddAuth = Rule.anyFileContains "AddAuthentication" csFiles
            let hasAddJwt  = Rule.anyFileContains "AddJwtBearer" csFiles
            if hasAddAuth && hasAddJwt then
                Pass("auth_implementation", Some "AddAuthentication + AddJwtBearer found")
            else
                let missing =
                    [ if not hasAddAuth then "AddAuthentication"
                      if not hasAddJwt  then "AddJwtBearer" ]
                    |> String.concat ","
                Fail("auth_implementation", $"missing:{missing}",
                     Some "Wire services.AddAuthentication().AddJwtBearer() in Program.cs")
    )

/// Python: FastAPI security dependency check.
let pythonAuthRule : Rule =
    StackSpecific(
        stack = Python,
        checkId = "auth_implementation",
        evaluate = fun files ->
            let pyFiles = files |> List.filter (fun f ->
                f.RelativePath.EndsWith(".py", System.StringComparison.OrdinalIgnoreCase))
            let hasOAuth = Rule.anyFileContains "OAuth2PasswordBearer" pyFiles
            let hasDepends = Rule.anyFileContains "Depends" pyFiles
            if hasOAuth && hasDepends then
                Pass("auth_implementation", Some "OAuth2PasswordBearer + Depends found")
            else
                Fail("auth_implementation", "missing_fastapi_security_dependency",
                     Some "Use OAuth2PasswordBearer + Depends(...) in FastAPI route definitions")
    )

/// Node.js: passport or jsonwebtoken check.
let nodeAuthRule : Rule =
    StackSpecific(
        stack = Node,
        checkId = "auth_implementation",
        evaluate = fun files ->
            let jsFiles = files |> List.filter (fun f ->
                f.RelativePath.EndsWith(".js", System.StringComparison.OrdinalIgnoreCase) ||
                f.RelativePath.EndsWith(".ts", System.StringComparison.OrdinalIgnoreCase))
            let hasJwt = Rule.anyFileContains "jsonwebtoken" jsFiles || Rule.anyFileContains "passport" jsFiles
            if hasJwt then Pass("auth_implementation", Some "JWT/passport found")
            else Fail("auth_implementation", "missing_jwt_library",
                      Some "Add jsonwebtoken or passport-jwt to authentication middleware")
    )

// ── Registry ─────────────────────────────────────────────────────────────

/// All built-in F# rules. C# consumers iterate this list and filter by AppliesTo(stack).
let allRules : Rule list =
    [ errorHandlingRule
      observabilityRule
      securityRule
      dotNetAuthRule
      pythonAuthRule
      nodeAuthRule ]

/// Public API for C# interop: evaluate all applicable rules and return results.
/// Returns a list of (checkId, satisfied, reasonOrEmpty) tuples where reasonOrEmpty is "" when absent.
/// Using plain string (not option) for easy C# consumption without FSharpOption unwrapping.
let evaluateAll
    (stackTag: string)
    (files: FileInput list)
    : (string * bool * string) list =

    let stack =
        match stackTag.ToLowerInvariant() with
        | "dotnet" | "csharp" | "aspnet" -> DotNet
        | "python" | "py" -> Python
        | "node" | "nodejs" | "javascript" | "typescript" -> Node
        | _ -> Any

    allRules
    |> List.filter (Rule.appliesTo stack)
    |> List.map (fun rule ->
        match Rule.evaluate files rule with
        | Pass (id, detail) -> (id, true, Option.defaultValue "" detail)
        | Fail (id, reason, _) -> (id, false, reason))
