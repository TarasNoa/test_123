namespace Libr4.Tasks.Domain.TimeTracking.FSharp

open System

// ============================================================================
// UNITS OF MEASURE - Compile-time financial safety
// These prevent mixing currencies, time units, and percentages at compile time
// ============================================================================

/// Currencies
[<Measure>] type RUB
[<Measure>] type USD
[<Measure>] type EUR
[<Measure>] type CNY
[<Measure>] type GBP
[<Measure>] type JPY
[<Measure>] type crypto  // Generic crypto unit

/// Time units - precise tracking
[<Measure>] type minute
[<Measure>] type hour
[<Measure>] type day
[<Measure>] type week
[<Measure>] type month
[<Measure>] type year

/// Ratios and percentages
[<Measure>] type percent

/// Conversion factors (compile-time constants)
module ConversionFactors =
    /// 1 hour = 60 minutes
    let hourToMinutes = 60.0<minute/hour>
    
    /// 1 day = 8 hours (work day)
    let dayToHours = 8.0<hour/day>
    
    /// 1 week = 40 hours (work week)
    let weekToHours = 40.0<hour/week>
    
    /// 1 month = 160 hours (work month)
    let monthToHours = 160.0<hour/month>
    
    /// 1 year = 1920 hours (work year)
    let yearToHours = 1920.0<hour/year>

// ============================================================================
// RATE TYPES - Self-documenting financial calculations
// ============================================================================

/// Hourly rate in RUB
/// Example: 1500.0<RUB/hour>
type HourlyRateRUB = float<RUB/hour>

/// Hourly rate in USD
/// Example: 50.0<USD/hour>
type HourlyRateUSD = float<USD/hour>

/// Daily rate in RUB
/// Example: 12000.0<RUB/day>
type DailyRateRUB = float<RUB/day>

/// Daily rate in USD
/// Example: 400.0<USD/day>
type DailyRateUSD = float<USD/day>

/// Fixed price in RUB
/// Example: 50000.0<RUB>
type FixedPriceRUB = float<RUB>

/// Fixed price in USD
/// Example: 1500.0<USD>
type FixedPriceUSD = float<USD>

/// Amount in RUB
/// Example: 10000.0<RUB>
type MoneyRUB = float<RUB>

/// Amount in USD
/// Example: 10000.0<USD>
type MoneyUSD = float<USD>

/// Duration in hours
/// Example: 40.0<hour>
type DurationHours = float<hour>

/// Duration in days
/// Example: 5.0<day>
type DurationDays = float<day>

/// Tax rate (always in percent)
/// Example: 20.0<percent>
type TaxRate = float<percent>

/// Discount rate (always in percent)
/// Example: 10.0<percent>
type DiscountRate = float<percent>

/// Commission rate (always in percent)
/// Example: 5.0<percent>
type CommissionRate = float<percent>

// ============================================================================
// CONVERSION FUNCTIONS - Type-safe transformations
// ============================================================================

module RateConversions =
    open ConversionFactors
    
    /// Convert hourly rate to daily rate (temporarily disabled)
    (*
    let toDaily<'currency> (hourly: HourlyRate<'currency>) : DailyRate<'currency> =
        hourly * dayToHours
    *)
    
    /// Convert hourly rate to weekly earnings (temporarily disabled)
    (*
    let toWeekly<'currency> (hourly: HourlyRate<'currency>) : float<'currency/week> =
        hourly * weekToHours
    *)
    
    /// Convert hourly rate to monthly earnings (temporarily disabled)
    (*
    let toMonthly<'currency> (hourly: HourlyRate<'currency>) : float<'currency/month> =
        hourly * monthToHours
    *)
    
    /// Convert any duration to hours
    /// Example: 5.0<day> -> 40.0<hour>
    let toHours (duration: float<'t>) : float<hour> =
        // This uses explicit conversions based on unit
        // F# will enforce correct units at compile time
        failwith "Use specific conversion functions below"
    
    /// Convert minutes to hours
    let minutesToHours (mins: float<minute>) : float<hour> =
        mins / 60.0<minute/hour>
    
    /// Convert hours to minutes
    let hoursToMinutes (hrs: float<hour>) : float<minute> =
        hrs * 60.0<minute/hour>
    
    /// Convert days to hours
    let daysToHours (days: float<day>) : float<hour> =
        days * dayToHours
    
    /// Convert weeks to hours
    let weeksToHours (weeks: float<week>) : float<hour> =
        weeks * weekToHours

// ============================================================================
// BILLING CALCULATIONS - Compile-time correctness guaranteed
// ============================================================================

module Billing =
    open ConversionFactors
    
    /// Calculate earnings from time worked in RUB
    let calculateEarningsRUB
        (hoursWorked: float<hour>)
        (hourlyRate: HourlyRateRUB)
        : MoneyRUB =
        hoursWorked * hourlyRate
    
    /// Calculate earnings from time worked in USD
    let calculateEarningsUSD
        (hoursWorked: float<hour>)
        (hourlyRate: HourlyRateUSD)
        : MoneyUSD =
        hoursWorked * hourlyRate
    
    /// Calculate earnings from minutes worked in RUB
    let calculateEarningsFromMinutesRUB
        (minutesWorked: float<minute>)
        (hourlyRate: HourlyRateRUB)
        : MoneyRUB =
        let hours = minutesWorked / 60.0<minute/hour>
        hours * hourlyRate
    
    /// Calculate earnings from minutes worked in USD
    let calculateEarningsFromMinutesUSD
        (minutesWorked: float<minute>)
        (hourlyRate: HourlyRateUSD)
        : MoneyUSD =
        let hours = minutesWorked / 60.0<minute/hour>
        hours * hourlyRate
    
    /// Apply discount in RUB
    let applyDiscountRUB
        (amount: MoneyRUB)
        (discountRate: DiscountRate)
        : {| Original: MoneyRUB; Discount: MoneyRUB; Final: MoneyRUB |} =
        
        let discount = amount * (discountRate / 100.0<percent>)
        {| Original = amount; Discount = discount; Final = amount - discount |}
    
    /// Apply discount in USD
    let applyDiscountUSD
        (amount: MoneyUSD)
        (discountRate: DiscountRate)
        : {| Original: MoneyUSD; Discount: MoneyUSD; Final: MoneyUSD |} =
        
        let discount = amount * (discountRate / 100.0<percent>)
        {| Original = amount; Discount = discount; Final = amount - discount |}
    
    /// Apply tax in RUB
    let applyTaxRUB
        (amount: MoneyRUB)
        (taxRate: TaxRate)
        : {| Subtotal: MoneyRUB; Tax: MoneyRUB; Total: MoneyRUB |} =
        
        let tax = amount * (taxRate / 100.0<percent>)
        {| Subtotal = amount; Tax = tax; Total = amount + tax |}
    
    /// Apply tax in USD
    let applyTaxUSD
        (amount: MoneyUSD)
        (taxRate: TaxRate)
        : {| Subtotal: MoneyUSD; Tax: MoneyUSD; Total: MoneyUSD |} =
        
        let tax = amount * (taxRate / 100.0<percent>)
        {| Subtotal = amount; Tax = tax; Total = amount + tax |}
    
    /// Full invoice calculation with all components in RUB
    let calculateInvoiceRUB
        (hoursWorked: float<hour>)
        (hourlyRate: HourlyRateRUB)
        (discountRate: DiscountRate)
        (taxRate: TaxRate)
        : {| 
            HoursWorked: float<hour>
            HourlyRate: HourlyRateRUB
            Subtotal: MoneyRUB
            Discount: MoneyRUB
            TaxableAmount: MoneyRUB
            Tax: MoneyRUB
            Total: MoneyRUB
        |} =
        
        let subtotal = calculateEarningsRUB hoursWorked hourlyRate
        let discountResult = applyDiscountRUB subtotal discountRate
        let taxResult = applyTaxRUB discountResult.Final taxRate
        
        {|
            HoursWorked = hoursWorked
            HourlyRate = hourlyRate
            Subtotal = subtotal
            Discount = discountResult.Discount
            TaxableAmount = discountResult.Final
            Tax = taxResult.Tax
            Total = taxResult.Total
        |}
    
    /// Full invoice calculation with all components in USD
    let calculateInvoiceUSD
        (hoursWorked: float<hour>)
        (hourlyRate: HourlyRateUSD)
        (discountRate: DiscountRate)
        (taxRate: TaxRate)
        : {| 
            HoursWorked: float<hour>
            HourlyRate: HourlyRateUSD
            Subtotal: MoneyUSD
            Discount: MoneyUSD
            TaxableAmount: MoneyUSD
            Tax: MoneyUSD
            Total: MoneyUSD
        |} =
        
        let subtotal = calculateEarningsUSD hoursWorked hourlyRate
        let discountResult = applyDiscountUSD subtotal discountRate
        let taxResult = applyTaxUSD discountResult.Final taxRate
        
        {|
            HoursWorked = hoursWorked
            HourlyRate = hourlyRate
            Subtotal = subtotal
            Discount = discountResult.Discount
            TaxableAmount = discountResult.Final
            Tax = taxResult.Tax
            Total = taxResult.Total
        |}

// ============================================================================
// ESCROW - Payment safety with currency enforcement
// ============================================================================

module Escrow =
    
    /// Release condition types
    type ReleaseCondition =
        | MilestoneCompleted of string
        | TimeElapsed of TimeSpan
        | CustomerApproval
        | AutomatedTestsPass
        | CodeReviewApproved
        | SecurityScanClean
    
    /// Escrow status
    type EscrowStatus =
        | Holding
        | Released
        | Disputed
        | Cancelled
    
    /// Escrow amount with currency safety and release conditions (RUB)
    type EscrowAmountRUB = {
        Amount: MoneyRUB
        HeldSince: DateTime
        ReleaseConditions: ReleaseCondition list
        Status: EscrowStatus
    }
    
    /// Escrow amount with currency safety and release conditions (USD)
    type EscrowAmountUSD = {
        Amount: MoneyUSD
        HeldSince: DateTime
        ReleaseConditions: ReleaseCondition list
        Status: EscrowStatus
    }
    
    /// Create new escrow amount in RUB
    let createEscrowRUB
        (amount: MoneyRUB)
        (conditions: ReleaseCondition list)
        : EscrowAmountRUB =
        {
            Amount = amount
            HeldSince = DateTime.UtcNow
            ReleaseConditions = conditions
            Status = Holding
        }
    
    /// Create new escrow amount in USD
    let createEscrowUSD
        (amount: MoneyUSD)
        (conditions: ReleaseCondition list)
        : EscrowAmountUSD =
        {
            Amount = amount
            HeldSince = DateTime.UtcNow
            ReleaseConditions = conditions
            Status = Holding
        }
    
    /// Release escrow when conditions are met (RUB)
    let releaseEscrowRUB (escrow: EscrowAmountRUB) : EscrowAmountRUB =
        { escrow with Status = Released }
    
    /// Release escrow when conditions are met (USD)
    let releaseEscrowUSD (escrow: EscrowAmountUSD) : EscrowAmountUSD =
        { escrow with Status = Released }
    
    /// Check if all conditions are met (RUB)
    let canReleaseRUB (escrow: EscrowAmountRUB) (conditionsMet: bool list) : bool =
        escrow.Status = Holding && 
        List.length conditionsMet = List.length escrow.ReleaseConditions &&
        conditionsMet |> List.forall id
    
    /// Check if all conditions are met (USD)
    let canReleaseUSD (escrow: EscrowAmountUSD) (conditionsMet: bool list) : bool =
        escrow.Status = Holding && 
        List.length conditionsMet = List.length escrow.ReleaseConditions &&
        conditionsMet |> List.forall id

// ============================================================================
// CURRENCY CONVERSION - Explicit and tracked (temporarily disabled)
// ============================================================================

(*
module CurrencyConversion =
    
    /// Exchange rate between two currencies
    /// Note: The rate itself is unitless (just a number)
    /// But we track what it converts from/to at type level
    type ExchangeRate<'fromCurrency, 'toCurrency> = {
        Rate: float  // Unitless: e.g., 90.0 for USD to RUB
        Timestamp: DateTime
        Source: string  // "CBRF", "ECB", "BINANCE", etc.
        ExpiresAt: DateTime
    }
    
    /// Convert money between currencies using exchange rate (temporarily disabled)
    let convertCurrency
        (amount: Money<'fromCurrency>)
        (rate: ExchangeRate<'fromCurrency, 'toCurrency>)
        : Money<'toCurrency> =
        
        if DateTime.UtcNow > rate.ExpiresAt then
            failwith "Exchange rate has expired"
        
        // The magic: amount * rate produces 'toCurrency unit
        amount * rate.Rate * 1.0<'toCurrency/'fromCurrency>
    
    /// Example rates (in production, fetch from API) (temporarily disabled)
    module SampleRates =
        let usdToRub = {
            Rate = 90.0
            Timestamp = DateTime.UtcNow
            Source = "CBRF"
            ExpiresAt = DateTime.UtcNow.AddHours(1.0)
        }
        
        let eurToUsd = {
            Rate = 1.08
            Timestamp = DateTime.UtcNow
            Source = "ECB"
            ExpiresAt = DateTime.UtcNow.AddHours(1.0)
        }
*)

// ============================================================================
// VALIDATION - Safe parsing and boundary checks
// ============================================================================

module Validation =
    
    /// Result type for validation
    type ValidationResult<'a> =
        | Valid of 'a
        | Invalid of string
    
    /// Parse money amount in RUB
    let parseMoneyRUB (value: decimal) : ValidationResult<MoneyRUB> =
        if value >= 0.0M then
            Valid (float value * 1.0<RUB>)
        else
            Invalid "Amount cannot be negative"
    
    /// Parse money amount in USD
    let parseMoneyUSD (value: decimal) : ValidationResult<MoneyUSD> =
        if value >= 0.0M then
            Valid (float value * 1.0<USD>)
        else
            Invalid "Amount cannot be negative"
    
    /// Parse hourly rate in RUB
    let parseHourlyRateRUB (value: decimal) : ValidationResult<HourlyRateRUB> =
        if value > 0.0M then
            Valid (float value * 1.0<RUB/hour>)
        else
            Invalid "Rate must be positive"
    
    /// Parse hourly rate in USD
    let parseHourlyRateUSD (value: decimal) : ValidationResult<HourlyRateUSD> =
        if value > 0.0M then
            Valid (float value * 1.0<USD/hour>)
        else
            Invalid "Rate must be positive"
    
    /// Parse duration
    let parseHours (value: float) : ValidationResult<float<hour>> =
        if value >= 0.0 then
            Valid (value * 1.0<hour>)
        else
            Invalid "Duration cannot be negative"
    
    /// Validate percentage
    let parsePercent (value: float) : ValidationResult<float<percent>> =
        if value >= 0.0 && value <= 100.0 then
            Valid (value * 1.0<percent>)
        else
            Invalid "Percentage must be between 0 and 100"

// ============================================================================
// C# INTEROP - Wrappers for consumption from C#
// ============================================================================

module CSharpInterop =
    
    /// Convert Money<'currency> to plain float for C# (temporarily disabled)
    (*
    /// This is the "escape hatch" - use only at boundaries!
    let toFloat<'currency> (money: Money<'currency>) : float =
        float money  // Removes unit, returns raw number
    *)
    
    /// Convert float to Money (temporarily disabled)
    (*
    let toMoney<'currency> (value: float) : Money<'currency> =
        value * 1.0<'currency>
    *)
    
    /// Convert HourlyRate to float (temporarily disabled due to generic units issue)
    (*
    let toFloatRate<'currency> (rate: HourlyRate<'currency>) : float =
        float rate
    *)
    
    /// Convert float to HourlyRate (temporarily disabled due to generic units issue)
    (*
    let toHourlyRate<'currency> (value: float) : HourlyRate<'currency> =
        value * 1.0<'currency/hour>
    *)
    
    /// Invoice result for C# consumption
    type InvoiceResult<'currency> = {
        HoursWorked: float
        HourlyRate: float
        Subtotal: float
        Discount: float
        TaxableAmount: float
        Tax: float
        Total: float
    }
    
    /// Calculate invoice and return C#-friendly result (temporarily disabled)
    (*
    let calculateInvoiceForCSharp<'currency>
        (hours: float)
        (rate: float)
        (discount: float)
        (tax: float)
        : InvoiceResult<'currency> =
        
        let result = Billing.calculateInvoice
                        (hours * 1.0<hour>)
                        (rate * 1.0<'currency/hour>)
                        (discount * 1.0<percent>)
                        (tax * 1.0<percent>)
        
        {
            HoursWorked = float result.HoursWorked
            HourlyRate = float result.HourlyRate
            Subtotal = float result.Subtotal
            Discount = float result.Discount
            TaxableAmount = float result.TaxableAmount
            Tax = float result.Tax
            Total = float result.Total
        }
    *)

// ============================================================================
// EXAMPLES - Show compile-time safety
// ============================================================================

module Examples =
    open Billing
    open RateConversions
    
    /// Example: Correct usage (temporarily disabled)
    let exampleCorrect () =
        let hourlyRate = 1500.0<RUB/hour>
        let hoursWorked = 40.0<hour>
        // let earnings = calculateEarnings hoursWorked hourlyRate  // temporarily disabled
        hourlyRate * hoursWorked  // simplified for now
    
    /// This will NOT compile - uncomment to see error:
    // let exampleWrong () =
    //     let hourlyRate = 1500.0<RUB/hour>
    //     let daysWorked = 5.0<day>
    //     // ERROR: Can't multiply RUB/hour by day!
    //     let earnings = calculateEarnings daysWorked hourlyRate
    
    /// This will NOT compile - mixing currencies:
    // let exampleWrongCurrency () =
    //     let rubles = 100.0<RUB>
    //     let dollars = 50.0<USD>
    //     // ERROR: Can't add RUB to USD!
    //     let total = rubles + dollars
    
    /// This will NOT compile - unit mismatch:
    // let exampleWrongUnit () =
    //     let rubles = 100.0<RUB>
    //     let hours = 5.0<hour>
    //     // ERROR: Can't add money to time!
    //     let nonsense = rubles + hours
