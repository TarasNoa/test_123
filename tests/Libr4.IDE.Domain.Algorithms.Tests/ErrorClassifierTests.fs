module ErrorClassifierTests

open Xunit
open Libr4.IDE.Domain.Algorithms
open Libr4.IDE.Domain.Algorithms.ErrorClassifier

[<Fact>]
let ``classifyError detects type mismatch`` () =
    let result =
        classifyError "src/App.tsx" 12 4 "Type 'string' is not assignable to type 'number'"

    match result.Classification with
    | ErrorClassification.TypeMismatch(expected, actual) ->
        Assert.Equal("number", expected)
        Assert.Equal("string", actual)
        Assert.False(result.AutoFixable)
    | other -> failwithf "expected TypeMismatch, got %A" other

[<Fact>]
let ``classifyError detects undefined variable`` () =
    let result =
        classifyError "Program.cs" 8 9 "CS0103: The name 'fooBar' does not exist in the current context"

    match result.Classification with
    | ErrorClassification.UndefinedVariable name ->
        Assert.Equal("fooBar", name)
        Assert.False(result.AutoFixable)
    | other -> failwithf "expected UndefinedVariable, got %A" other

[<Fact>]
let ``classifyBuildOutput parses ts error lines`` () =
    let output =
        "src/components/Button.tsx(15,23): error TS2345: Type 'string' is not assignable to type 'number'."

    let errors = classifyBuildOutput output "typescript"
    Assert.Equal(1, errors.Length)

    match errors.[0].Classification with
    | ErrorClassification.TypeMismatch _ ->
        Assert.Equal("src/components/Button.tsx", errors.[0].Location.FilePath)
    | other -> failwithf "expected TypeMismatch, got %A" other

[<Fact>]
let ``classifyError detects missing import hint`` () =
    let result =
        classifyError "App.tsx" 3 1 "Cannot find name 'React'. Did you mean to import 'react'?"

    match result.Classification with
    | ErrorClassification.MissingImport moduleName ->
        Assert.Equal("react", moduleName)
        Assert.True(result.AutoFixable)
    | other -> failwithf "expected MissingImport, got %A" other

[<Fact>]
let ``getComplexErrors filters auto-fixable syntax issues`` () =
    let typeErr =
        classifyError "b.ts" 2 2 "Type 'string' is not assignable to type 'number'"

    let importErr =
        classifyError "a.ts" 1 1 "Cannot find name 'React'. Did you mean to import 'react'?"

    let complex = getComplexErrors [ typeErr; importErr ]
    Assert.Equal(1, complex.Length)
    Assert.Contains(complex, fun e ->
        match e.Classification with
        | ErrorClassification.TypeMismatch _ -> true
        | _ -> false)

[<Fact>]
let ``applyAutoFix adds semicolon when pattern matches trailing semicolon marker`` () =
    let err = classifyError "file.ts" 1 1 "missing semicolon;"

    match err.Classification with
    | ErrorClassification.MissingSemicolon ->
        let patched = applyAutoFix err "const x = 1\n"
        Assert.True(patched.IsSome)
        Assert.EndsWith(";", patched.Value.TrimEnd())
    | other -> failwithf "expected MissingSemicolon, got %A" other

[<Fact>]
let ``calculateTokenSavings counts auto-fixable errors`` () =
    let importErr =
        classifyError "a.ts" 1 1 "Cannot find name 'React'. Did you mean to import 'react'?"

    let typeErr =
        classifyError "b.ts" 2 2 "Type 'string' is not assignable to type 'number'."

    let stats = calculateTokenSavings [ importErr; typeErr ]
    Assert.Equal(2, stats.Total)
    Assert.Equal(1, stats.AutoFixed)
    Assert.True(stats.TokenSavings > 0)

[<Fact>]
let ``CSharpInterop classifyForCSharp returns boxed results`` () =
    let output = "src/x.ts(1,1): error TS2345: Type 'string' is not assignable to type 'number'."
    let items = CSharpInterop.classifyForCSharp output
    Assert.NotEmpty(items)
