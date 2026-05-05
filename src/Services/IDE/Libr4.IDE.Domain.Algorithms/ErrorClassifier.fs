namespace Libr4.IDE.Domain.Algorithms

open System
open System.Text.RegularExpressions

// ============================================================================
// ERROR CLASSIFICATION SYSTEM (F#)
// Classifies build errors to reduce AI token usage
// Simple errors fixed locally, only complex errors sent to LLM
// ============================================================================

/// Classification of build errors
type ErrorClassification =
    | SyntaxError of severity: string
    | MissingSemicolon
    | MissingImport of moduleName: string
    | TypeMismatch of expected: string * actual: string
    | UndefinedVariable of variableName: string
    | MissingBrace of braceType: string
    | ImportCycle of modules: string list
    | ComplexError of description: string  // Requires AI intervention

/// Error location information
type ErrorLocation = {
    FilePath: string
    Line: int
    Column: int
}

/// Classified error with fix suggestion
type ClassifiedError = {
    Classification: ErrorClassification
    Location: ErrorLocation
    OriginalMessage: string
    SuggestedFix: string option
    AutoFixable: bool
    ConfidenceScore: float  // 0.0 to 1.0
}

/// Module for error classification logic
module ErrorClassifier =
    
    /// Regex patterns for common errors
    let private patterns = [
        // Missing semicolon (TypeScript/JavaScript/C#)
        (Regex(@";\s*$", RegexOptions.Multiline), 
         fun m loc msg -> Some {
             Classification = MissingSemicolon
             Location = loc
             OriginalMessage = msg
             SuggestedFix = Some "Add semicolon at end of line"
             AutoFixable = true
             ConfidenceScore = 0.95
         })
        
        // Missing import (TypeScript)
        (Regex(@"Cannot find name '([^']+)'.*Did you mean to import '([^']+)'\?"),
         fun m loc msg -> 
             let varName = m.Groups.[1].Value
             let moduleName = m.Groups.[2].Value
             Some {
                 Classification = MissingImport moduleName
                 Location = loc
                 OriginalMessage = msg
                 SuggestedFix = Some $"import {{ {varName} }} from '{moduleName}';"
                 AutoFixable = true
                 ConfidenceScore = 0.90
             })
        
        // Undefined variable
        (Regex(@"(ReferenceError|CS0103):?\s*The name '([^']+)' does not exist"),
         fun m loc msg ->
             let varName = m.Groups.[2].Value
             Some {
                 Classification = UndefinedVariable varName
                 Location = loc
                 OriginalMessage = msg
                 SuggestedFix = Some $"Define variable '{varName}' or check for typos"
                 AutoFixable = false  // Need context to fix
                 ConfidenceScore = 0.85
             })
        
        // Missing brace
        (Regex(@"(Unexpected end of input|CS1513):?\s*[^}]*\{|expected '}'"),
         fun m loc msg ->
             Some {
                 Classification = MissingBrace "curly"
                 Location = loc
                 OriginalMessage = msg
                 SuggestedFix = Some "Check for missing closing brace }"
                 AutoFixable = true
                 ConfidenceScore = 0.80
             })
        
        // Type mismatch (TypeScript)
        (Regex(@"Type '([^']+)' is not assignable to type '([^']+)'"),
         fun m loc msg ->
             let actual = m.Groups.[1].Value
             let expected = m.Groups.[2].Value
             Some {
                 Classification = TypeMismatch (expected, actual)
                 Location = loc
                 OriginalMessage = msg
                 SuggestedFix = Some $"Convert {actual} to {expected} or adjust type annotation"
                 AutoFixable = false  // Complex fix
                 ConfidenceScore = 0.70
             })
        
        // Import cycle
        (Regex(@"(Circular dependency|import cycle)"),
         fun m loc msg ->
             Some {
                 Classification = ImportCycle []  // Would need to extract module names
                 Location = loc
                 OriginalMessage = msg
                 SuggestedFix = Some "Restructure imports to avoid circular dependency"
                 AutoFixable = false
                 ConfidenceScore = 0.75
             })
    ]
    
    /// Classify a single error message
    let classifyError (filePath: string) (line: int) (column: int) (errorMessage: string) : ClassifiedError =
        let location = { FilePath = filePath; Line = line; Column = column }
        
        // Try to match against known patterns
        let matched = 
            patterns
            |> List.tryPick (fun (regex, classifier) ->
                let m = regex.Match(errorMessage)
                if m.Success then classifier m location errorMessage
                else None)
        
        match matched with
        | Some classified -> classified
        | None ->
            // Unknown error - requires AI
            {
                Classification = ComplexError errorMessage
                Location = location
                OriginalMessage = errorMessage
                SuggestedFix = None
                AutoFixable = false
                ConfidenceScore = 0.0
            }
    
    /// Classify multiple errors from build output
    let classifyBuildOutput (buildOutput: string) (language: string) : ClassifiedError list =
        let lines = buildOutput.Split('\n')
        
        lines
        |> Array.mapi (fun i line -> (i + 1, line))
        |> Array.choose (fun (lineNum, line) ->
            // Parse error format: "file.ts(10,5): error TS2345: message"
            let errorRegex = Regex(@"([^\(]+)\((\d+),(\d+)\):\s*(error|warning)\s*(\w+):?\s*(.*)")
            let m = errorRegex.Match(line)
            
            if m.Success then
                let filePath = m.Groups.[1].Value.Trim()
                let line = int m.Groups.[2].Value
                let col = int m.Groups.[3].Value
                let errorCode = m.Groups.[5].Value
                let message = m.Groups.[6].Value
                
                Some (classifyError filePath line col message)
            else
                None)
        |> Array.toList
    
    /// Apply auto-fix for simple errors
    let applyAutoFix (error: ClassifiedError) (sourceCode: string) : string option =
        if not error.AutoFixable then None
        else
            let lines = sourceCode.Split('\n')
            let targetLine = error.Location.Line - 1
            
            if targetLine < 0 || targetLine >= lines.Length then None
            else
                match error.Classification with
                | MissingSemicolon ->
                    // Add semicolon at end of line
                    let currentLine = lines.[targetLine]
                    if not (currentLine.TrimEnd().EndsWith(";")) then
                        lines.[targetLine] <- currentLine.TrimEnd() + ";"
                        Some (String.Join("\n", lines))
                    else None
                    
                | MissingImport moduleName ->
                    // Add import at top of file
                    let importStatement = $"import {{ ??? }} from '{moduleName}';  // TODO: Specify import"
                    let newLines = Array.append [| importStatement |] lines
                    Some (String.Join("\n", newLines))
                    
                | MissingBrace _ ->
                    // Add closing brace (naive approach)
                    lines.[targetLine] <- lines.[targetLine] + "\n}"
                    Some (String.Join("\n", lines))
                    
                | _ -> None  // Other errors not auto-fixable
    
    /// Filter errors requiring AI intervention
    let getComplexErrors (errors: ClassifiedError list) : ClassifiedError list =
        errors
        |> List.filter (fun e -> 
            match e.Classification with
            | ComplexError _ -> true
            | TypeMismatch _ -> true  // Often requires AI
            | UndefinedVariable _ -> true  // Need context
            | _ -> false)
    
    /// Calculate token savings from auto-fixes
    let calculateTokenSavings (allErrors: ClassifiedError list) : {| Total: int; AutoFixed: int; TokenSavings: int |} =
        let total = allErrors.Length
        let autoFixed = allErrors |> List.filter (fun e -> e.AutoFixable) |> List.length
        
        // Estimate: simple error = 100 tokens, complex error = 1000 tokens
        let tokenSavings = autoFixed * 900  // Saved by not sending to LLM
        
        {| Total = total; AutoFixed = autoFixed; TokenSavings = tokenSavings |}

// ============================================================================
// C# INTEROP - For consumption from SelfHealingBuildPipeline
// ============================================================================

module CSharpInterop =
    open ErrorClassifier
    
    /// Classify errors from build output (for C#)
    let classifyForCSharp (buildOutput: string) : obj list =
        let errors = classifyBuildOutput buildOutput "csharp"
        
        errors
        |> List.map (fun e ->
            box {|
                Classification = e.Classification.ToString()
                FilePath = e.Location.FilePath
                Line = e.Location.Line
                Column = e.Location.Column
                Message = e.OriginalMessage
                AutoFixable = e.AutoFixable
                SuggestedFix = e.SuggestedFix |> Option.defaultValue ""
                Confidence = e.ConfidenceScore
            |})
    
    /// Get only errors requiring AI (for C#)
    let getErrorsForAI (buildOutput: string) : obj list =
        let allErrors = classifyBuildOutput buildOutput "typescript"
        let complexErrors = getComplexErrors allErrors
        
        complexErrors
        |> List.map (fun e ->
            box {|
                FilePath = e.Location.FilePath
                Line = e.Location.Line
                Message = e.OriginalMessage
            |})
    
    /// Calculate statistics (for C#)
    let getStatistics (buildOutput: string) : obj =
        let errors = classifyBuildOutput buildOutput "typescript"
        let stats = calculateTokenSavings errors
        
        box {|
            TotalErrors = stats.Total
            AutoFixableErrors = stats.AutoFixed
            ComplexErrors = stats.Total - stats.AutoFixed
            EstimatedTokenSavings = stats.TokenSavings
        |}

// ============================================================================
// EXAMPLES
// ============================================================================

module Examples =
    open ErrorClassifier
    
    let exampleBuildOutput = """
src/components/Button.tsx(15,23): error TS1005: ';' expected.
src/utils/api.ts(10,5): error TS2345: Type 'string' is not assignable to type 'number'.
src/app/page.tsx(5,1): error TS2304: Cannot find name 'React'.
"""
    
    let demonstrate () =
        let errors = classifyBuildOutput exampleBuildOutput "typescript"
        
        printfn "Classified %d errors:" errors.Length
        
        errors |> List.iter (fun e ->
            printfn "  - %s at %s:%d" 
                (e.Classification.ToString()) 
                e.Location.FilePath 
                e.Location.Line
            
            if e.AutoFixable then
                printfn "    [AUTO-FIXABLE] %s" (e.SuggestedFix |> Option.defaultValue "")
            else
                printfn "    [NEEDS AI] %s" e.OriginalMessage)
        
        let stats = calculateTokenSavings errors
        printfn "\nStatistics:"
        printfn "  Total: %d, Auto-fixable: %d, Token savings: ~%d" 
            stats.Total stats.AutoFixed stats.TokenSavings
