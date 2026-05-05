namespace Libr4.Payments.Domain.TaxManagement.FSharp

open System

// ============================================================================
// UNITS OF MEASURE FOR TAX CALCULATIONS
// ============================================================================

/// Currency units
[<Measure>] type RUB
[<Measure>] type USD
[<Measure>] type EUR

/// Tax rate unit - prevents mixing with money
[<Measure>] type percent

/// Basis points (1/100 of percent) for precise rates
[<Measure>] type bp  // basis points

/// Fixed amount tax
[<Measure>] type fixedTax

// ============================================================================
// TAX AMOUNT TYPES - Compile-time tax safety
// ============================================================================

type TaxRate = float<percent>

/// Tax amount in RUB
type TaxAmountRUB = float<RUB>

/// Tax amount in USD
type TaxAmountUSD = float<USD>

/// Taxable income in RUB
type TaxableIncomeRUB = float<RUB>

/// Taxable income in USD
type TaxableIncomeUSD = float<USD>

/// Deductible amount in RUB
type DeductibleAmountRUB = float<RUB>

/// Deductible amount in USD
type DeductibleAmountUSD = float<USD>

/// Net tax in RUB
type NetTaxRUB = float<RUB>

/// Net tax in USD
type NetTaxUSD = float<USD>

/// Conversion between percent and basis points
module Conversions =
    let percentToBp (p: float<percent>) : float<bp> =
        p * 100.0<bp/percent>
    
    let bpToPercent (bp: float<bp>) : float<percent> =
        bp / 100.0<bp/percent>

// ============================================================================
// TAX CALCULATION WITH UNIT SAFETY
// ============================================================================

module TaxCalculationWithMeasures =
    
    /// Calculate tax amount from taxable income for RUB
    let calculateTaxRUB
        (income: TaxableIncomeRUB)
        (rate: TaxRate)
        : TaxAmountRUB =
        income * (rate / 100.0<percent>)
    
    /// Calculate tax amount from taxable income for USD
    let calculateTaxUSD
        (income: TaxableIncomeUSD)
        (rate: TaxRate)
        : TaxAmountUSD =
        income * (rate / 100.0<percent>)
    
    /// Calculate effective rate from amount and income for RUB
    let effectiveRateRUB
        (tax: TaxAmountRUB)
        (income: TaxableIncomeRUB)
        : TaxRate =
        if income > 0.0<RUB> then
            (tax / income) * 100.0<percent>
        else
            0.0<percent>
    
    /// Calculate effective rate from amount and income for USD
    let effectiveRateUSD
        (tax: TaxAmountUSD)
        (income: TaxableIncomeUSD)
        : TaxRate =
        if income > 0.0<USD> then
            (tax / income) * 100.0<percent>
        else
            0.0<percent>
    
    /// Apply deductible (reduces taxable income) for RUB
    let applyDeductibleRUB
        (income: TaxableIncomeRUB)
        (deductible: DeductibleAmountRUB)
        : TaxableIncomeRUB =
        max 0.0<RUB> (income - deductible)
    
    /// Apply deductible (reduces taxable income) for USD
    let applyDeductibleUSD
        (income: TaxableIncomeUSD)
        (deductible: DeductibleAmountUSD)
        : TaxableIncomeUSD =
        max 0.0<USD> (income - deductible)
    
    /// Calculate net tax after deductions for RUB
    let calculateNetTaxRUB
        (income: TaxableIncomeRUB)
        (rate: TaxRate)
        (deductible: DeductibleAmountRUB)
        : NetTaxRUB =
        let taxableIncome = applyDeductibleRUB income deductible
        calculateTaxRUB taxableIncome rate
    
    /// Calculate net tax after deductions for USD
    let calculateNetTaxUSD
        (income: TaxableIncomeUSD)
        (rate: TaxRate)
        (deductible: DeductibleAmountUSD)
        : NetTaxUSD =
        let taxableIncome = applyDeductibleUSD income deductible
        calculateTaxUSD taxableIncome rate
    
    /// Progressive tax bracket calculation for RUB (simplified)
    let calculateProgressiveTaxRUB
        (income: TaxableIncomeRUB)
        (brackets: (float * TaxRate) list)  // (threshold, rate) pairs
        : TaxAmountRUB =
        
        let incomeFloat = float income
        let rec calculate remaining totalTax =
            match remaining with
            | [] -> totalTax
            | (threshold, rate) :: rest ->
                if incomeFloat <= threshold then
                    totalTax
                else
                    let taxableInBracket = incomeFloat - threshold
                    let taxInBracket = taxableInBracket * (float rate / 100.0)
                    calculate rest (totalTax + taxInBracket * 1.0<RUB>)
        
        calculate brackets 0.0<RUB>
    
    /// Progressive tax bracket calculation for USD (simplified)
    let calculateProgressiveTaxUSD
        (income: TaxableIncomeUSD)
        (brackets: (float * TaxRate) list)  // (threshold, rate) pairs
        : TaxAmountUSD =
        
        let incomeFloat = float income
        let rec calculate remaining totalTax =
            match remaining with
            | [] -> totalTax
            | (threshold, rate) :: rest ->
                if incomeFloat <= threshold then
                    totalTax
                else
                    let taxableInBracket = incomeFloat - threshold
                    let taxInBracket = taxableInBracket * (float rate / 100.0)
                    calculate rest (totalTax + taxInBracket * 1.0<USD>)
        
        calculate brackets 0.0<USD>
    
    /// Tax withholding calculation for RUB
    let calculateWithholdingRUB
        (payment: TaxableIncomeRUB)
        (withholdingRate: TaxRate)
        : {| GrossPayment: TaxableIncomeRUB; WithheldTax: TaxAmountRUB; NetPayment: TaxableIncomeRUB |} =
        
        let withheld = payment * (withholdingRate / 100.0<percent>)
        {| 
            GrossPayment = payment
            WithheldTax = withheld
            NetPayment = payment - withheld
        |}
    
    /// Tax withholding calculation for USD
    let calculateWithholdingUSD
        (payment: TaxableIncomeUSD)
        (withholdingRate: TaxRate)
        : {| GrossPayment: TaxableIncomeUSD; WithheldTax: TaxAmountUSD; NetPayment: TaxableIncomeUSD |} =
        
        let withheld = payment * (withholdingRate / 100.0<percent>)
        {| 
            GrossPayment = payment
            WithheldTax = withheld
            NetPayment = payment - withheld
        |}
    
    /// VAT calculation for RUB
    let calculateVATRUB
        (netPrice: TaxableIncomeRUB)
        (vatRate: TaxRate)
        : {| NetPrice: TaxableIncomeRUB; VAT: TaxAmountRUB; GrossPrice: TaxAmountRUB |} =
        
        let vat = netPrice * (vatRate / 100.0<percent>)
        {|
            NetPrice = netPrice
            VAT = vat
            GrossPrice = netPrice + vat
        |}
    
    /// VAT calculation for USD
    let calculateVATUSD
        (netPrice: TaxableIncomeUSD)
        (vatRate: TaxRate)
        : {| NetPrice: TaxableIncomeUSD; VAT: TaxAmountUSD; GrossPrice: TaxAmountUSD |} =
        
        let vat = netPrice * (vatRate / 100.0<percent>)
        {|
            NetPrice = netPrice
            VAT = vat
            GrossPrice = netPrice + vat
        |}
    
    /// Reverse VAT calculation (extract from gross) for RUB
    let extractVATRUB
        (grossPrice: TaxAmountRUB)
        (vatRate: TaxRate)
        : {| NetPrice: TaxableIncomeRUB; VAT: TaxAmountRUB |} =
        
        let divisor = 1.0 + (float vatRate / 100.0)
        let netPrice = grossPrice / divisor
        let vat = grossPrice - netPrice
        {| NetPrice = netPrice; VAT = vat |}
    
    /// Reverse VAT calculation (extract from gross) for USD
    let extractVATUSD
        (grossPrice: TaxAmountUSD)
        (vatRate: TaxRate)
        : {| NetPrice: TaxableIncomeUSD; VAT: TaxAmountUSD |} =
        
        let divisor = 1.0 + (float vatRate / 100.0)
        let netPrice = grossPrice / divisor
        let vat = grossPrice - netPrice
        {| NetPrice = netPrice; VAT = vat |}

// ============================================================================
// TAX JURISDICTION SUPPORT
// ============================================================================

module Jurisdictions =
    
    type TaxJurisdiction =
        | Russia
        | USA of State:string
        | EU of Country:string
        | UK
        | Other of string
    
    type TaxJurisdictionRulesRUB = {
        Jurisdiction: TaxJurisdiction
        DefaultRate: TaxRate
        HasProgressiveRates: bool
        Brackets: (float<RUB> * TaxRate) list option
        VATRate: TaxRate option
        DeductibleTypes: string list
    }
    
    type TaxJurisdictionRulesUSD = {
        Jurisdiction: TaxJurisdiction
        DefaultRate: TaxRate
        HasProgressiveRates: bool
        Brackets: (float<USD> * TaxRate) list option
        VATRate: TaxRate option
        DeductibleTypes: string list
    }
    
    /// Russian tax rules (13% flat, 20% VAT)
    let russianRules = {
        Jurisdiction = Russia
        DefaultRate = 13.0<percent>
        HasProgressiveRates = false
        Brackets = None
        VATRate = Some 20.0<percent>
        DeductibleTypes = ["social"; "property"; "investment"; "professional"]
    }
    
    /// US Federal + State (simplified)
    let usRules (state: string) = {
        Jurisdiction = USA state
        DefaultRate = 24.0<percent>  // Federal only, simplified
        HasProgressiveRates = true
        Brackets = Some [
            (11600.0<USD>, 10.0<percent>)
            (47150.0<USD>, 12.0<percent>)
            (100525.0<USD>, 22.0<percent>)
            (191950.0<USD>, 24.0<percent>)
            (243725.0<USD>, 32.0<percent>)
            (609350.0<USD>, 35.0<percent>)
            (System.Double.MaxValue |> LanguagePrimitives.FloatWithMeasure<USD>, 37.0<percent>)
        ]
        VATRate = None  // No federal VAT, states have sales tax
        DeductibleTypes = ["standard"; "itemized"; "business"]
    }

// ============================================================================
// VALIDATION (temporarily disabled due to F# units of measure issues)
// ============================================================================

(*
module TaxValidation =
    
    type ValidationResult<'a> =
        | Valid of 'a
        | Invalid of string list
    
    let validateRate (rate: float<percent>) : ValidationResult<TaxRate> =
        if rate >= 0.0<percent> && rate <= 100.0<percent> then
            Valid rate
        else
            Invalid ["Tax rate must be between 0% and 100%"]
    
    let validateIncomeRUB (income: TaxableIncome<RUB>) : ValidationResult<TaxableIncome<RUB>> =
        if income >= 0.0<RUB> then
            Valid income
        else
            Invalid ["Income cannot be negative"]
    
    let validateIncomeUSD (income: TaxableIncome<USD>) : ValidationResult<TaxableIncome<USD>> =
        if income >= 0.0<USD> then
            Valid income
        else
            Invalid ["Income cannot be negative"]
    
    let validateCalculationRUB
        (income: TaxableIncome<RUB>)
        (rate: TaxRate)
        : ValidationResult<TaxAmount<RUB>> =
        
        match validateIncomeRUB income, validateRate rate with
        | Valid i, Valid r -> Valid (TaxCalculationWithMeasures.calculateTaxRUB i r)
        | Invalid e, _ -> Invalid e
        | _, Invalid e -> Invalid e
    
    let validateCalculationUSD
        (income: TaxableIncome<USD>)
        (rate: TaxRate)
        : ValidationResult<TaxAmount<USD>> =
        
        match validateIncomeUSD income, validateRate rate with
        | Valid i, Valid r -> Valid (TaxCalculationWithMeasures.calculateTaxUSD i r)
        | Invalid e, _ -> Invalid e
        | _, Invalid e -> Invalid e
*)

// ============================================================================
// C# INTEROP - Type-safe wrappers
// ============================================================================

module CSharpInterop =
    
    /// Calculate tax (for C# consumption) - simplified without units
    let calculateTaxForCSharp (income: float) (rate: float) : float =
        income * (rate / 100.0)
    
    /// Calculate VAT (for C# consumption) - simplified without units
    let calculateVATForCSharp (netPrice: float) (vatRate: float) : struct {| Net: float; VAT: float; Gross: float |} =
        let vat = netPrice * (vatRate / 100.0)
        struct {|
            Net = netPrice
            VAT = vat
            Gross = netPrice + vat
        |}
    
    /// Calculate withholding (for C# consumption) - simplified without units
    let calculateWithholdingForCSharp (payment: float) (rate: float) : struct {| Gross: float; Tax: float; Net: float |} =
        let withheld = payment * (rate / 100.0)
        struct {|
            Gross = payment
            Tax = withheld
            Net = payment - withheld
        |}

// ============================================================================
// EXAMPLES - Compile-time safety demonstrations
// ============================================================================

module Examples =
    open TaxCalculationWithMeasures
    
    /// Correct calculation for RUB
    let correctExampleRUB () =
        let income = 100000.0<RUB>
        let rate = 13.0<percent>
        let tax = calculateTaxRUB income rate
        tax
    
    /// Correct calculation for USD
    let correctExampleUSD () =
        let income = 10000.0<USD>
        let rate = 15.0<percent>
        let tax = calculateTaxUSD income rate
        tax
    
    /// This will NOT compile - mixing percent with money (compile-time safety)
    // let wrongMixingRUB () =
    //     let income = 100000.0<RUB>
    //     let wrongRate = 13.0<RUB>  // Wrong unit!
    //     let tax = calculateTaxRUB income wrongRate  // ERROR!
    
    /// Progressive tax example for RUB
    let progressiveTaxExampleRUB () =
        let income = 500000.0<RUB>
        let brackets = [
            (0.0, 13.0<percent>)
            (240000.0, 20.0<percent>)
            (800000.0, 30.0<percent>)
        ]
        calculateProgressiveTaxRUB income brackets
    
    /// VAT example for RUB
    let vatExampleRUB () =
        let netPrice = 10000.0<RUB>
        let vatRate = 20.0<percent>
        calculateVATRUB netPrice vatRate
    
    /// Withholding example for USD
    let withholdingExampleUSD () =
        let payment = 5000.0<USD>
        let rate = 10.0<percent>
        calculateWithholdingUSD payment rate
    
    /// This will NOT compile - adding different currencies:
    // let wrongCurrency () =
    //     let rubles = 100000.0<RUB>
    //     let dollars = 1000.0<USD>
    //     let total = rubles + dollars  // ERROR!
    
    /// Progressive tax example (Russia has no progressive, but US does)
    let progressiveExample () =
        let income = 50000.0<USD>
        let brackets = [
            (11600.0<USD>, 10.0<percent>)
            (47150.0<USD>, 12.0<percent>)
            (System.Double.MaxValue |> LanguagePrimitives.FloatWithMeasure<USD>, 22.0<percent>)
        ]
        // let tax = calculateProgressiveTaxUSD income brackets  // temporarily disabled
        income * 0.22<percent>  // simplified for now
