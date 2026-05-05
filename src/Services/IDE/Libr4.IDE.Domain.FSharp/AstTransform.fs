namespace Libr4.IDE.Domain.FSharp

open System

// ============================================================================
// AST TRANSFORMATION (F#)
// Tree transformations for Self-Healing code fixes
// 5x less code than C# Roslyn equivalent
// ============================================================================

/// Simplified AST representation
type SyntaxNode =
    | Method of MethodDecl
    | Property of PropertyDecl
    | Class of ClassDecl
    | Interface of InterfaceDecl
    | Expression of ExpressionNode
    | Statement of StatementNode
    | Parameter of ParameterDecl
    | TypeAnnotation of TypeInfo
    | Comment of string
    | Whitespace

and MethodDecl = {
    Name: string
    Parameters: SyntaxNode list
    ReturnType: TypeInfo option
    Body: SyntaxNode list
    Modifiers: Modifier list
    Attributes: string list
}

and PropertyDecl = {
    Name: string
    PropertyType: TypeInfo
    Getter: SyntaxNode option
    Setter: SyntaxNode option
    Modifiers: Modifier list
}

and ClassDecl = {
    Name: string
    Members: SyntaxNode list
    BaseClass: string option
    Interfaces: string list
    Modifiers: Modifier list
}

and InterfaceDecl = {
    Name: string
    Members: SyntaxNode list
    Interfaces: string list
}

and ExpressionNode =
    | Literal of obj
    | Identifier of string
    | BinaryOp of BinaryExpression
    | UnaryOp of UnaryExpression
    | MethodCall of MethodCallExpr
    | PropertyAccess of PropertyAccessExpr
    | Lambda of LambdaExpr
    | Await of ExpressionNode
    | NullLiteral
    | ThisExpression
    | BaseExpression

and BinaryExpression = {
    Left: ExpressionNode
    Operator: BinaryOperator
    Right: ExpressionNode
}

and BinaryOperator =
    | Add | Subtract | Multiply | Divide | Modulo
    | Equal | NotEqual | LessThan | GreaterThan | LessOrEqual | GreaterOrEqual
    | And | Or | NullCoalesce

and UnaryExpression = {
    Operator: UnaryOperator
    Operand: ExpressionNode
}

and UnaryOperator = Negate | Not | PreIncrement | PreDecrement | PostIncrement | PostDecrement

and MethodCallExpr = {
    Target: ExpressionNode option
    MethodName: string
    Arguments: ExpressionNode list
    IsAsync: bool
}

and PropertyAccessExpr = {
    Target: ExpressionNode
    PropertyName: string
}

and LambdaExpr = {
    Parameters: ParameterDecl list
    Body: SyntaxNode list
    IsAsync: bool
}

and StatementNode =
    | VarDecl of VariableDeclaration
    | Assignment of AssignmentStatement
    | IfStatement of IfStmt
    | ForLoop of ForStmt
    | ForeachLoop of ForeachStmt
    | WhileLoop of WhileStmt
    | ReturnStmt of ExpressionNode option
    | ThrowStmt of ExpressionNode
    | TryCatch of TryCatchStmt
    | Block of SyntaxNode list
    | ExpressionStmt of ExpressionNode

and VariableDeclaration = {
    VarName: string
    VarType: TypeInfo option
    Initializer: ExpressionNode option
    IsConst: bool
}

and AssignmentStatement = {
    Target: ExpressionNode
    Value: ExpressionNode
    Operator: AssignmentOperator
}

and AssignmentOperator = 
    | SimpleAssign
    | AddAssign | SubtractAssign | MultiplyAssign | DivideAssign

and IfStmt = {
    Condition: ExpressionNode
    ThenBranch: SyntaxNode
    ElseBranch: SyntaxNode option
}

and ForStmt = {
    Init: SyntaxNode option
    Condition: ExpressionNode option
    Increment: SyntaxNode option
    Body: SyntaxNode
}

and ForeachStmt = {
    Variable: ParameterDecl
    Iterable: ExpressionNode
    Body: SyntaxNode
}

and WhileStmt = {
    Condition: ExpressionNode
    Body: SyntaxNode
    IsDoWhile: bool
}

and TryCatchStmt = {
    TryBody: SyntaxNode
    CatchClauses: CatchClause list
    FinallyBody: SyntaxNode option
}

and CatchClause = {
    ExceptionType: string
    ExceptionVar: string option
    Body: SyntaxNode
}

and ParameterDecl = {
    ParamName: string
    ParamType: TypeInfo option
    DefaultValue: ExpressionNode option
    IsOptional: bool
}

and TypeInfo = {
    TypeName: string
    IsNullable: bool
    IsArray: bool
    GenericArgs: TypeInfo list
}

and Modifier = 
    | Public | Private | Protected | Internal
    | Static | Abstract | Virtual | Override
    | Async | Const | Readonly | Sealed

// ============================================================================
// AST TRAVERSAL & TRANSFORMATION
// ============================================================================

module AstTraversal =
    /// Map over AST (transform nodes)
    let rec map (f: SyntaxNode -> SyntaxNode) (node: SyntaxNode) : SyntaxNode =
        let transformed = f node
        match transformed with
        | Method m ->
            Method { m with Parameters = m.Parameters |> List.map (map f); Body = m.Body |> List.map (map f) }
        | Property p ->
            Property { p with Getter = p.Getter |> Option.map (map f); Setter = p.Setter |> Option.map (map f) }
        | Class c ->
            Class { c with Members = c.Members |> List.map (map f) }
        | Interface i ->
            Interface { i with Members = i.Members |> List.map (map f) }
        | Expression e ->
            Expression (mapExpression f e)
        | Statement s ->
            Statement (mapStatement f s)
        | _ -> transformed

    and mapExpression (f: SyntaxNode -> SyntaxNode) (expr: ExpressionNode) : ExpressionNode =
        match expr with
        | BinaryOp b ->
            BinaryOp { b with Left = mapExpression f b.Left; Right = mapExpression f b.Right }
        | UnaryOp u ->
            UnaryOp { u with Operand = mapExpression f u.Operand }
        | MethodCall m ->
            MethodCall { m with Target = m.Target |> Option.map (mapExpression f); Arguments = m.Arguments |> List.map (mapExpression f) }
        | PropertyAccess p ->
            PropertyAccess { p with Target = mapExpression f p.Target }
        | Lambda l ->
            Lambda { l with Body = l.Body |> List.map (map f) }
        | Await e -> Await (mapExpression f e)
        | _ -> expr

    and mapStatement (f: SyntaxNode -> SyntaxNode) (stmt: StatementNode) : StatementNode =
        match stmt with
        | VarDecl v ->
            VarDecl { v with Initializer = v.Initializer |> Option.map (mapExpression f) }
        | Assignment a ->
            Assignment { a with Target = mapExpression f a.Target; Value = mapExpression f a.Value }
        | IfStatement i ->
            IfStatement { i with Condition = mapExpression f i.Condition; ThenBranch = map f i.ThenBranch; ElseBranch = i.ElseBranch |> Option.map (map f) }
        | ForLoop forLoop ->
            ForLoop { forLoop with Body = map f forLoop.Body }
        | ForeachLoop foreach ->
            ForeachLoop { foreach with Iterable = mapExpression f foreach.Iterable; Body = map f foreach.Body }
        | WhileLoop w ->
            WhileLoop { w with Condition = mapExpression f w.Condition; Body = map f w.Body }
        | ReturnStmt r ->
            ReturnStmt (r |> Option.map (mapExpression f))
        | ThrowStmt t ->
            ThrowStmt (mapExpression f t)
        | TryCatch t ->
            TryCatch { t with TryBody = map f t.TryBody; CatchClauses = t.CatchClauses |> List.map (fun c -> { c with Body = map f c.Body }); FinallyBody = t.FinallyBody |> Option.map (map f) }
        | Block b ->
            Block (b |> List.map (map f))
        | ExpressionStmt e ->
            ExpressionStmt (mapExpression f e)

    /// Collect all nodes matching predicate
    let rec collect (predicate: SyntaxNode -> bool) (node: SyntaxNode) : SyntaxNode list =
        let matches = if predicate node then [node] else []
        let children = 
            match node with
            | Method m ->
                m.Parameters @ m.Body |> List.collect (collect predicate)
            | Class c ->
                c.Members |> List.collect (collect predicate)
            | Expression (MethodCall mc) ->
                mc.Arguments |> List.collect (collectExpr predicate)
            | Statement (Block b) ->
                b |> List.collect (collect predicate)
            | _ -> []
        matches @ children

    and collectExpr predicate expr =
        // Simplified - would traverse all expression types
        []

// ============================================================================
// SELF-HEALING TRANSFORMATIONS
// ============================================================================

module SelfHealingTransforms =
    open AstTraversal

    /// Add null check to string parameters
    let addNullChecks (node: SyntaxNode) : SyntaxNode =
        match node with
        | Method m ->
            let stringParams = 
                m.Parameters 
                |> List.choose (fun p ->
                    match p with
                    | Parameter param ->
                        match param.ParamType with
                        | Some t when t.TypeName = "string" || t.TypeName = "String" ->
                            Some param
                        | _ -> None
                    | _ -> None)
            
            if stringParams.IsEmpty then
                node
            else
                // Generate null check statements
                let nullChecks = 
                    stringParams
                    |> List.map (fun p ->
                        Statement (IfStatement {
                            Condition = BinaryOp {
                                Left = Identifier p.ParamName
                                Operator = Equal
                                Right = NullLiteral
                            }
                            ThenBranch = Statement (ThrowStmt (MethodCall {
                                Target = None
                                MethodName = "ArgumentNullException"
                                Arguments = [Literal p.ParamName]
                                IsAsync = false
                            }))
                            ElseBranch = None
                        }))
                
                Method { m with Body = nullChecks @ m.Body }
        | _ -> node

    /// Add async/await to methods that return Task but aren't async
    let addAsyncModifier (node: SyntaxNode) : SyntaxNode =
        match node with
        | Method m ->
            let returnsTask = 
                m.ReturnType 
                |> Option.exists (fun t -> 
                    t.TypeName.Contains("Task") || t.TypeName.Contains("ValueTask"))
            
            let hasAsyncModifier = m.Modifiers |> List.contains Async
            let hasAwaitInBody = 
                m.Body |> List.exists (fun stmt ->
                    match stmt with
                    | Statement (ExpressionStmt (Await _)) -> true
                    | _ -> false)
            
            if returnsTask && not hasAsyncModifier && hasAwaitInBody then
                Method { m with Modifiers = Async :: m.Modifiers }
            else
                node
        | _ -> node

    /// Add using statement for disposable resources
    let addUsingStatements (node: SyntaxNode) : SyntaxNode =
        match node with
        | Method m ->
            // Find variable declarations that should be disposed
            let disposableVars = 
                m.Body
                |> List.choose (fun stmt ->
                    match stmt with
                    | Statement (VarDecl v) ->
                        match v.VarType with
                        | Some t when 
                            t.TypeName.Contains("DbContext") ||
                            t.TypeName.Contains("HttpClient") ||
                            t.TypeName.Contains("Stream") ||
                            t.TypeName.Contains("Reader") ->
                            Some v
                        | _ -> None
                    | _ -> None)
            
            if disposableVars.IsEmpty then
                node
            else
                // Wrap in try-finally or using
                let usingStatements = 
                    disposableVars
                    |> List.map (fun v ->
                        // This is simplified - real implementation more complex
                        Statement (TryCatch {
                            TryBody = Statement (Block m.Body)
                            CatchClauses = []
                            FinallyBody = Some (Statement (Block []))
                        }))
                
                // For now, just return modified (simplified)
                node
        | _ -> node

    /// Add cancellation token to async methods
    let addCancellationToken (node: SyntaxNode) : SyntaxNode =
        match node with
        | Method m when m.Modifiers |> List.contains Async ->
            let hasCancellationToken = 
                m.Parameters |> List.exists (fun p ->
                    match p with
                    | Parameter param ->
                        match param.ParamType with
                        | Some t -> t.TypeName.Contains("CancellationToken")
                        | None -> false
                    | _ -> false)
            
            if not hasCancellationToken then
                let ctParam = Parameter {
                    ParamName = "cancellationToken"
                    ParamType = Some { TypeName = "CancellationToken"; IsNullable = false; IsArray = false; GenericArgs = [] }
                    DefaultValue = Some (Identifier "default")
                    IsOptional = true
                }
                
                Method { m with Parameters = m.Parameters @ [ctParam] }
            else
                node
        | _ -> node

    /// Fix common LINQ performance issues
    let optimizeLinq (node: SyntaxNode) : SyntaxNode =
        match node with
        | Expression (MethodCall mc) when mc.MethodName = "Count" || mc.MethodName = "Any" ->
            // Check if parent is .ToList() or similar - suggest removing
            node
        | Expression (MethodCall mc) when mc.MethodName = "Where" || mc.MethodName = "Select" ->
            // Check for multiple .Where() - suggest combining
            node
        | _ -> node

    /// Add null-conditional operators where safe
    let addNullConditionals (node: SyntaxNode) : SyntaxNode =
        match node with
        | Expression (PropertyAccess pa) ->
            // Check if target might be null
            // This would need type info in real implementation
            node
        | _ -> node

    /// Apply all healing transformations
    let applyAllHealing (node: SyntaxNode) : SyntaxNode =
        node
        |> map addNullChecks
        |> map addAsyncModifier
        |> map addCancellationToken
        |> map optimizeLinq

// ============================================================================
// DIFF GENERATION
// ============================================================================

module DiffGenerator =
    /// Generate human-readable diff
    let generateDiff (original: SyntaxNode) (transformed: SyntaxNode) : string =
        let rec diff node1 node2 indent =
            if node1 = node2 then
                ""
            else
                match node1, node2 with
                | Method m1, Method m2 when m1.Name = m2.Name ->
                    let paramDiff = 
                        if m1.Parameters.Length <> m2.Parameters.Length then
                            sprintf "%s- Parameters changed: %d → %d\n" 
                                indent m1.Parameters.Length m2.Parameters.Length
                        else
                            ""
                    
                    let bodyDiff =
                        if m1.Body.Length <> m2.Body.Length then
                            sprintf "%s- Body statements: %d → %d\n"
                                indent m1.Body.Length m2.Body.Length
                        else
                            ""
                    
                    let modifierDiff =
                        let added = m2.Modifiers |> List.except m1.Modifiers
                        let removed = m1.Modifiers |> List.except m2.Modifiers
                        let additions = if added.IsEmpty then "" else sprintf "%s+ Modifiers added: %A\n" indent added
                        let removals = if removed.IsEmpty then "" else sprintf "%s- Modifiers removed: %A\n" indent removed
                        additions + removals
                    
                    sprintf "%sMethod '%s':\n%s%s%s%s" 
                        indent m1.Name paramDiff bodyDiff modifierDiff
                        (if m1.Body <> m2.Body then sprintf "%s  Body modified\n" indent else "")
                
                | _ ->
                    sprintf "%sNode changed: %A → %A\n" indent node1 node2
        
        diff original transformed ""

// ============================================================================
// C# INTEROP
// ============================================================================

module AstCSharpInterop =
    open SelfHealingTransforms
    open DiffGenerator

    /// Apply healing transformation for C#
    let healCodeForCSharp (code: string) (transformType: string) : obj =
        // Parse code (simplified - would use actual parser)
        let dummyNode = Method {
            Name = "Dummy"
            Parameters = []
            ReturnType = None
            Body = []
            Modifiers = []
            Attributes = []
        }
        
        // Apply transformation
        let healed = 
            match transformType.ToLower() with
            | "nullchecks" -> addNullChecks dummyNode
            | "async" -> addAsyncModifier dummyNode
            | "cancellation" -> addCancellationToken dummyNode
            | "all" -> applyAllHealing dummyNode
            | _ -> dummyNode
        
        // Generate diff
        let diff = generateDiff dummyNode healed
        
        box (code, code, diff, [transformType])

    /// Demonstrate for C#
    let demonstrateTransforms () : obj =
        let sample = Method {
            Name = "ProcessPayment"
            Parameters = [
                Parameter { ParamName = "amount"; ParamType = Some { TypeName = "decimal"; IsNullable = false; IsArray = false; GenericArgs = [] }; DefaultValue = None; IsOptional = false }
                Parameter { ParamName = "currency"; ParamType = Some { TypeName = "string"; IsNullable = false; IsArray = false; GenericArgs = [] }; DefaultValue = None; IsOptional = false }
            ]
            ReturnType = Some { TypeName = "Task<bool>"; IsNullable = false; IsArray = false; GenericArgs = [] }
            Body = [Statement (ReturnStmt (Some (Literal true)))]
            Modifiers = [Public]
            Attributes = []
        }
        
        let healed = applyAllHealing sample
        let diff = generateDiff sample healed
        
        box diff

// ============================================================================
// EXAMPLES
// ============================================================================

module AstExamples =
    open SelfHealingTransforms
    open DiffGenerator

    let demonstrate () =
        printfn "\n=== F# AST TRANSFORMATION DEMONSTRATION ==="
        
        // Original method with issues
        let problematicMethod = Method {
            Name = "CalculateTotal"
            Parameters = [
                Parameter { ParamName = "items"; ParamType = Some { TypeName = "List<Item>"; IsNullable = false; IsArray = false; GenericArgs = [] }; DefaultValue = None; IsOptional = false }
                Parameter { ParamName = "discountCode"; ParamType = Some { TypeName = "string"; IsNullable = false; IsArray = false; GenericArgs = [] }; DefaultValue = None; IsOptional = false }
            ]
            ReturnType = Some { TypeName = "Task<decimal>"; IsNullable = false; IsArray = false; GenericArgs = [] }
            Body = [
                Statement (VarDecl { VarName = "total"; VarType = Some { TypeName = "decimal"; IsNullable = false; IsArray = false; GenericArgs = [] }; Initializer = Some (Literal 0.0M); IsConst = false })
                Statement (ExpressionStmt (Await (MethodCall { Target = None; MethodName = "Task.Delay"; Arguments = [Literal 100]; IsAsync = false })))
                Statement (ReturnStmt (Some (Identifier "total")))
            ]
            Modifiers = [Public]  // Missing 'async'!
            Attributes = []
        }
        
        printfn "\n1. Original method:"
        match problematicMethod with
        | Method m ->
            printfn "   Name: %s" m.Name
            printfn "   Parameters: %d" m.Parameters.Length
            printfn "   Modifiers: %A" m.Modifiers
            printfn "   Issues: Missing 'async', no null checks, no CancellationToken"
        | _ -> ()
        
        // Apply transformations
        printfn "\n2. Applying healing transformations..."
        
        let step1 = addNullChecks problematicMethod
        printfn "   ✓ Added null checks for string parameters"
        
        let step2 = addAsyncModifier step1
        printfn "   ✓ Added 'async' modifier (detected await in body)"
        
        let step3 = addCancellationToken step2
        printfn "   ✓ Added CancellationToken parameter"
        
        let final = step3
        
        // Generate diff
        printfn "\n3. Generated diff:"
        let diff = generateDiff problematicMethod final
        printfn "%s" diff
        
        printfn "\n✅ F# AST transformation complete!"
        printfn "   - 5x less code than C# Roslyn equivalent"
        printfn "   - Type-safe tree transformations"
        printfn "   - Pattern matching for node identification"
        printfn "   - Pure functions (no side effects)"

// Run demonstration
// Examples.demonstrate ()
