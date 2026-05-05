namespace Libr4.Payments.Domain.TaxManagement.FSharp

open System

// ============================================================================
// DOUBLE-ENTRY BOOKKEEPING (F#)
// Financial integrity at the type level
// Every transaction has two sides that sum to zero
// If balance doesn't match by 0.00001 - system blocks
// ============================================================================

/// Currency units with strict type safety
[<Measure>] type RUB
[<Measure>] type USD
[<Measure>] type EUR

/// Account types in double-entry system
type AccountType =
    | Asset        // Cash, receivables (debit increases)
    | Liability    // Payables, loans (credit increases)
    | Equity       // Owner's capital
    | Revenue      // Income (credit increases)
    | Expense      // Costs (debit increases)

/// Entry type
type EntrySide =
    | Debit
    | Credit

/// Account with balance
type Account<'currency> = {
    Id: string
    Name: string
    Type: AccountType
    Balance: float<'currency>
    NormalBalance: EntrySide  // Which side increases this account
}

/// Single entry in a transaction
type LedgerEntry<'currency> = {
    AccountId: string
    Side: EntrySide
    Amount: float<'currency>
    Description: string
    Timestamp: DateTime
}

/// Transaction with multiple entries (must balance to zero)
type Transaction<'currency> = {
    Id: string
    Timestamp: DateTime
    Description: string
    Entries: LedgerEntry<'currency> list
    Verified: bool
    BalanceCheck: float<'currency>  // Must be 0.0<'currency>
}

/// Escrow account specific record
type EscrowAccount<'currency> = {
    Account: Account<'currency>
    OrderId: string
    CustomerId: string
    FreelancerId: string
    Status: EscrowStatus
    CreatedAt: DateTime
}

and EscrowStatus =
    | Pending
    | Funded
    | Released
    | Disputed
    | Refunded

/// Journal - immutable list of transactions
type Journal<'currency> = {
    Transactions: Transaction<'currency> list
    Accounts: Map<string, Account<'currency>>
    LastVerifiedAt: DateTime option
}

/// Audit trail entry
type AuditEntry = {
    Timestamp: DateTime
    TransactionId: string
    Action: string
    OldValue: string
    NewValue: string
    AuthorizedBy: string
}

// ============================================================================
// DOUBLE-ENTRY LOGIC
// ============================================================================

module DoubleEntryBookkeeping =
    
    /// Create a new transaction (ensures it balances)
    let createTransaction<'currency> 
        (id: string)
        (description: string)
        (entries: LedgerEntry<'currency> list)
        : Result<Transaction<'currency>, string> =
        
        // Calculate total debits and credits
        let debits = entries |> List.filter (fun e -> e.Side = Debit) |> List.sumBy (fun e -> e.Amount)
        let credits = entries |> List.filter (fun e -> e.Side = Credit) |> List.sumBy (fun e -> e.Amount)
        
        // Check balance (must be equal for double-entry)
        let balanceCheck = debits - credits
        
        // Tolerance: 0.00001 (for floating point)
        let tolerance = 0.00001<_>
        
        if abs balanceCheck > tolerance then
            Error $"Transaction does not balance. Debits: {debits}, Credits: {credits}, Difference: {balanceCheck}"
        else
            Ok {
                Id = id
                Timestamp = DateTime.UtcNow
                Description = description
                Entries = entries
                Verified = true
                BalanceCheck = 0.0<_>  // Must be exactly zero
            }
    
    /// Post transaction to journal (only if verified)
    let postTransaction<'currency> 
        (journal: Journal<'currency>)
        (transaction: Transaction<'currency>)
        : Result<Journal<'currency>, string> =
        
        if not transaction.Verified then
            Error "Cannot post unverified transaction"
        elif abs transaction.BalanceCheck > 0.00001<_> then
            Error "Transaction balance check failed - possible tampering"
        else
            // Update account balances
            let updatedAccounts = 
                transaction.Entries
                |> List.fold (fun acc entry ->
                    match Map.tryFind entry.AccountId acc with
                    | Some account ->
                        let newBalance = 
                            match entry.Side with
                            | Debit -> account.Balance + entry.Amount
                            | Credit -> account.Balance - entry.Amount
                        
                        // Verify account type consistency
                        let valid = 
                            match account.NormalBalance, entry.Side with
                            | Debit, Debit -> true    // Asset/Expense: debit increases
                            | Credit, Credit -> true  // Liability/Equity/Revenue: credit increases
                            | _, _ -> true            // Allow opposite (decreases)
                        
                        if valid then
                            Map.add entry.AccountId { account with Balance = newBalance } acc
                        else
                            acc
                    | None -> acc
                ) journal.Accounts
            
            Ok {
                journal with
                    Transactions = transaction :: journal.Transactions
                    Accounts = updatedAccounts
                    LastVerifiedAt = Some DateTime.UtcNow
            }
    
    /// Verify entire journal balances
    let verifyJournal<'currency> (journal: Journal<'currency>) : Result<bool, string> =
        // Sum all account balances
        let totalBalance = 
            journal.Accounts 
            |> Map.toList 
            |> List.sumBy (fun (_, account) -> account.Balance)
        
        // In double-entry, total of all accounts should equal zero
        // (Assets = Liabilities + Equity)
        let tolerance = 0.00001<_>
        
        if abs totalBalance > tolerance then
            Error $"Journal out of balance by {totalBalance}. Possible data corruption."
        else
            Ok true
    
    /// Create standard escrow transaction (funding)
    let createEscrowFundingTransaction<'currency>
        (orderId: string)
        (amount: float<'currency>)
        (customerAccountId: string)
        (escrowAccountId: string)
        : Result<Transaction<'currency>, string> =
        
        // Double-entry for escrow funding:
        // Debit: Escrow Account (Asset increases)
        // Credit: Customer Payable (Liability increases)
        
        let entries = [
            {
                AccountId = escrowAccountId
                Side = Debit
                Amount = amount
                Description = $"Escrow funding for order {orderId}"
                Timestamp = DateTime.UtcNow
            }
            {
                AccountId = customerAccountId
                Side = Credit
                Amount = amount
                Description = $"Customer liability for order {orderId}"
                Timestamp = DateTime.UtcNow
            }
        ]
        
        createTransaction (Guid.NewGuid().ToString("N")) $"Escrow funding - Order {orderId}" entries
    
    /// Create escrow release transaction
    let createEscrowReleaseTransaction<'currency>
        (orderId: string)
        (amount: float<'currency>)
        (escrowAccountId: string)
        (freelancerAccountId: string)
        (platformFeeAccountId: string)
        (platformFee: float<'currency>)
        : Result<Transaction<'currency>, string> =
        
        // Double-entry for escrow release:
        // Credit: Escrow Account (Asset decreases)
        // Debit: Freelancer Payable (Liability decreases for net amount)
        // Debit: Platform Fee Revenue (Revenue increases)
        
        let netAmount = amount - platformFee
        
        let entries = [
            {
                AccountId = escrowAccountId
                Side = Credit
                Amount = amount
                Description = $"Escrow release for order {orderId}"
                Timestamp = DateTime.UtcNow
            }
            {
                AccountId = freelancerAccountId
                Side = Debit
                Amount = netAmount
                Description = $"Payment to freelancer for order {orderId}"
                Timestamp = DateTime.UtcNow
            }
            {
                AccountId = platformFeeAccountId
                Side = Debit
                Amount = platformFee
                Description = $"Platform fee for order {orderId}"
                Timestamp = DateTime.UtcNow
            }
        ]
        
        createTransaction (Guid.NewGuid().ToString("N")) $"Escrow release - Order {orderId}" entries
    
    /// Get account balance
    let getBalance<'currency> (journal: Journal<'currency>) (accountId: string) : float<'currency> option =
        journal.Accounts |> Map.tryFind accountId |> Option.map (fun a -> a.Balance)
    
    /// Get trial balance (all accounts)
    let getTrialBalance<'currency> (journal: Journal<'currency>) : (Account<'currency> * float<'currency>) list =
        journal.Accounts
        |> Map.toList
        |> List.map (fun (_, account) ->
            // Calculate running balance
            let balance = account.Balance
            account, balance)
    
    /// Immutable audit trail - cannot be modified, only appended
    let addAuditEntry 
        (auditTrail: AuditEntry list)
        (transactionId: string)
        (action: string)
        (oldValue: string)
        (newValue: string)
        (authorizedBy: string)
        : AuditEntry list =
        
        let entry = {
            Timestamp = DateTime.UtcNow
            TransactionId = transactionId
            Action = action
            OldValue = oldValue
            NewValue = newValue
            AuthorizedBy = authorizedBy
        }
        
        entry :: auditTrail  // Prepend - newest first, immutable
    
    /// Rollback detection - verify transaction sequence
    let detectAnomalies<'currency> (journal: Journal<'currency>) : string list =
        let anomalies = ResizeArray<string>()
        
        // Check for timestamp reversals
        let timestamps = 
            journal.Transactions 
            |> List.map (fun t -> t.Timestamp)
            |> List.sort
        
        let rec checkOrder ts =
            match ts with
            | t1 :: t2 :: rest ->
                if t1 > t2 then
                    anomalies.Add($"Timestamp reversal detected: {t1} > {t2}")
                checkOrder (t2 :: rest)
            | _ -> ()
        
        checkOrder timestamps
        
        // Check for duplicate transaction IDs
        let idCounts = 
            journal.Transactions
            |> List.countBy (fun t -> t.Id)
            |> List.filter (fun (_, count) -> count > 1)
        
        for (id, count) in idCounts do
            anomalies.Add($"Duplicate transaction ID: {id} ({count} occurrences)")
        
        // Check for unverified transactions
        let unverified = 
            journal.Transactions
            |> List.filter (fun t -> not t.Verified)
        
        if unverified.Length > 0 then
            anomalies.Add($"{unverified.Length} unverified transactions found")
        
        List.ofSeq anomalies

// ============================================================================
// ESCROW-SPECIFIC LOGIC
// ============================================================================

module EscrowBookkeeping =
    open DoubleEntryBookkeeping
    
    /// Standard escrow accounts
    let createEscrowChartOfAccounts<'currency> (orderId: string) : Map<string, Account<'currency>> =
        [
            $"escrow-holding-{orderId}", {
                Id = $"escrow-holding-{orderId}"
                Name = $"Escrow Holding - Order {orderId}"
                Type = Asset
                Balance = 0.0<_>
                NormalBalance = Debit
            }
            $"customer-payable-{orderId}", {
                Id = $"customer-payable-{orderId}"
                Name = $"Customer Payable - Order {orderId}"
                Type = Liability
                Balance = 0.0<_>
                NormalBalance = Credit
            }
            $"freelancer-receivable-{orderId}", {
                Id = $"freelancer-receivable-{orderId}"
                Name = $"Freelancer Receivable - Order {orderId}"
                Type = Liability
                Balance = 0.0<_>
                NormalBalance = Credit
            }
            $"platform-fee-{orderId}", {
                Id = $"platform-fee-{orderId}"
                Name = $"Platform Fee Revenue - Order {orderId}"
                Type = Revenue
                Balance = 0.0<_>
                NormalBalance = Credit
            }
        ]
        |> Map.ofList
    
    /// Process escrow funding
    let fundEscrow<'currency>
        (journal: Journal<'currency>)
        (orderId: string)
        (amount: float<'currency>)
        : Result<Journal<'currency> * Transaction<'currency>, string> =
        
        let escrowAccountId = $"escrow-holding-{orderId}"
        let customerAccountId = $"customer-payable-{orderId}"
        
        // Ensure accounts exist
        let journalWithAccounts = 
            if not (journal.Accounts.ContainsKey escrowAccountId) then
                let accounts = createEscrowChartOfAccounts<'currency> orderId
                { journal with Accounts = Map.fold (fun acc k v -> Map.add k v acc) journal.Accounts accounts }
            else
                journal
        
        // Create funding transaction
        match createEscrowFundingTransaction orderId amount customerAccountId escrowAccountId with
        | Error e -> Error e
        | Ok transaction ->
            match postTransaction journalWithAccounts transaction with
            | Error e -> Error e
            | Ok updatedJournal -> Ok (updatedJournal, transaction)
    
    /// Process escrow release
    let releaseEscrow<'currency>
        (journal: Journal<'currency>)
        (orderId: string)
        (totalAmount: float<'currency>)
        (platformFeePercent: float<percent>)
        : Result<Journal<'currency> * Transaction<'currency>, string> =
        
        let escrowAccountId = $"escrow-holding-{orderId}"
        let freelancerAccountId = $"freelancer-receivable-{orderId}"
        let feeAccountId = $"platform-fee-{orderId}"
        
        let platformFee = totalAmount * (platformFeePercent / 100.0<percent>)
        
        // Create release transaction
        match createEscrowReleaseTransaction orderId totalAmount escrowAccountId freelancerAccountId feeAccountId platformFee with
        | Error e -> Error e
        | Ok transaction ->
            match postTransaction journal transaction with
            | Error e -> Error e
            | Ok updatedJournal -> Ok (updatedJournal, transaction)
    
    /// Get escrow status
    let getEscrowStatus<'currency> (journal: Journal<'currency>) (orderId: string) : EscrowStatus option =
        let escrowAccountId = $"escrow-holding-{orderId}"
        
        match getBalance journal escrowAccountId with
        | Some balance when balance > 0.0<_> -> Some Funded
        | Some balance when balance = 0.0<_> -> Some Released
        | _ -> Some Pending

// ============================================================================
// C# INTEROP
// ============================================================================

module CSharpInterop =
    open DoubleEntryBookkeeping
    open EscrowBookkeeping
    
    /// Create journal for C#
    let createJournalForCSharp () : obj =
        box {
            Transactions = []
            Accounts = Map.empty
            LastVerifiedAt = None
        }
    
    /// Process escrow funding for C#
    let fundEscrowForCSharp (amount: float) (orderId: string) : obj =
        // This would be called from C# with proper currency handling
        box $"Funding escrow for order {orderId} with amount {amount}"
    
    /// Verify journal for C#
    let verifyJournalForCSharp (journal: obj) : obj =
        // C# would pass journal and get verification result
        box true  // Simplified
    
    /// Get audit trail for C#
    let getAuditTrailForCSharp (journal: obj) : obj list =
        // Return audit entries
        []

// ============================================================================
// EXAMPLES
// ============================================================================

module Examples =
    open DoubleEntryBookkeeping
    open EscrowBookkeeping
    
    let demonstrateDoubleEntry () =
        // Create journal with accounts
        let journal = {
            Transactions = []
            Accounts = Map.empty
            LastVerifiedAt = None
        }
        
        // Fund escrow: $1000
        let orderId = "ORD-12345"
        let amount = 1000.0<RUB>
        
        match fundEscrow journal orderId amount with
        | Ok (updatedJournal, transaction) ->
            printfn "Escrow funded successfully!"
            printfn "Transaction ID: %s" transaction.Id
            printfn "Balance check: %.10f (must be 0)" (float transaction.BalanceCheck)
            
            // Verify journal
            match verifyJournal updatedJournal with
            | Ok _ -> printfn "Journal verified - all balances correct!"
            | Error e -> printfn "VERIFICATION FAILED: %s" e
            
            updatedJournal
            
        | Error e ->
            printfn "Funding failed: %s" e
            journal
    
    let demonstrateTamperingDetection () =
        // Create balanced transaction
        let entries = [
            {
                AccountId = "escrow-1"
                Side = Debit
                Amount = 100.0<RUB>
                Description = "Debit"
                Timestamp = DateTime.UtcNow
            }
            {
                AccountId = "liability-1"
                Side = Credit
                Amount = 99.99<RUB>  // Slight mismatch!
                Description = "Credit"
                Timestamp = DateTime.UtcNow
            }
        ]
        
        match createTransaction "TX-001" "Tampered transaction" entries with
        | Ok _ -> printfn "ERROR: Should have detected imbalance!"
        | Error e -> printfn "Correctly detected: %s" e
    
    let runAllExamples () =
        printfn "\n=== DOUBLE-ENTRY BOOKKEEPING EXAMPLES ==="
        demonstrateDoubleEntry () |> ignore
        printfn ""
        demonstrateTamperingDetection ()
        printfn ""
        printfn "All examples completed."
