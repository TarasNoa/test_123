# F# Units of Measure - Financial System Safety

## Overview

F# Units of Measure provide compile-time type safety for numerical values with units. This prevents mixing incompatible units (e.g., adding rubles to dollars, or hours to days) at the compilation level, not runtime.

## Why Critical for Libr4

1. **Payment System** (Port 5003): Escrow, Stripe, AML - no room for currency errors
2. **Time Tracking** (F# module): Billable hours vs real hours vs overtime
3. **Tax Management** (F# module): Different tax rates, jurisdictions
4. **Trading** (Port 5005): Crypto/stock prices with different decimals

## Basic Pattern

```fsharp
// Define units
[<Measure>] type ruble
[<Measure>] type dollar
[<Measure>] type hour
[<Measure>] type day
[<Measure>] type percent

// Create values with units
let hourlyRate = 1500.0<ruble/hour>
let workHours = 8.0<hour>
let total = hourlyRate * workHours  // 12000.0<ruble>

// This WILL NOT COMPILE:
// let wrong = hourlyRate + workHours  // ERROR: Can't add ruble/hour to hour
// let mixed = 100.0<ruble> + 50.0<dollar>  // ERROR: Can't add rubles to dollars
```

## Advanced Pattern: Generic Units

```fsharp
// Generic function that works with any currency
let calculateCommission<'currency when 'currency :> System.IComparable>
    (amount: float<'currency>)
    (rate: float<percent>) 
    : float<'currency> =
    amount * (rate / 100.0<percent>)

// Usage
let rubleCommission = calculateCommission 10000.0<ruble> 2.5<percent>  // 250.0<ruble>
let dollarCommission = calculateCommission 500.0<dollar> 2.5<percent>  // 12.5<dollar>
```

## Libr4 Financial Types

```fsharp
namespace Libr4.Tasks.Domain.TimeTracking.FSharp

module FinancialTypes =
    // Currencies
    [<Measure>] type RUB
    [<Measure>] type USD
    [<Measure>] type EUR
    [<Measure>] type CNY
    
    // Time units
    [<Measure>] type hour
    [<Measure>] type day = 8.0<hour>  // 1 day = 8 hours
    [<Measure>] type week = 40.0<hour>  // 1 week = 40 hours
    [<Measure>] type month = 160.0<hour>  // 1 month = 160 hours
    
    // Rate types
    type HourlyRate<'currency> = float<'currency/hour>
    type DailyRate<'currency> = float<'currency/day>
    type FixedPrice<'currency> = float<'currency>
    
    // Tax and discounts
    [<Measure>] type percent
    type TaxRate = float<percent>
    type DiscountRate = float<percent>
    
    // Conversions
    let toDaily (hourly: HourlyRate<'currency>) : DailyRate<'currency> =
        hourly * 8.0<hour/day>
    
    let toWeekly (hourly: HourlyRate<'currency>) : float<'currency/week> =
        hourly * 40.0<hour/week>

module Billing =
    open FinancialTypes
    
    // Calculate invoice with compile-time unit safety
    let calculateInvoice
        (hoursWorked: float<hour>)
        (hourlyRate: HourlyRate<RUB>)
        (taxRate: TaxRate)
        (discountRate: DiscountRate)
        : {| Subtotal: float<RUB>; Tax: float<RUB>; Discount: float<RUB>; Total: float<RUB> |} =
        
        let subtotal = hoursWorked * hourlyRate
        let discount = subtotal * (discountRate / 100.0<percent>)
        let taxableAmount = subtotal - discount
        let tax = taxableAmount * (taxRate / 100.0<percent>)
        let total = taxableAmount + tax
        
        {| Subtotal = subtotal; Tax = tax; Discount = discount; Total = total |}
    
    // This is SAFE - units are tracked through all calculations
    // let invoice = calculateInvoice 40.0<hour> 2000.0<RUB/hour> 20.0<percent> 10.0<percent>
    // Result: { Subtotal = 80000.0<RUB>; Tax = 14400.0<RUB>; ... }

module Escrow =
    open FinancialTypes
    
    // Escrow amount with currency safety
    type EscrowAmount<'currency> = {
        Amount: float<'currency>
        HeldSince: System.DateTime
        ReleaseConditions: ReleaseCondition list
    }
    
    and ReleaseCondition =
        | MilestoneCompleted of string
        | TimeElapsed of System.TimeSpan
        | CustomerApproval
        | AutomatedTestPass
    
    // Can only compare same currencies
    let canRelease (amount: EscrowAmount<'currency>) (conditions: bool list) : bool =
        conditions |> List.forall id
    
    // Conversion rate (runtime validation required)
    type ExchangeRate<'fromCurrency, 'toCurrency> = {
        Rate: float  // unitless ratio
        Timestamp: System.DateTime
        Source: string  // e.g., "CBRF", "ECB", "BINANCE"
    }
    
    // Safe conversion with explicit rate
    let convertCurrency (amount: float<'fromCurrency>) 
                        (rate: ExchangeRate<'fromCurrency, 'toCurrency>) 
                        : float<'toCurrency> =
        amount * rate.Rate * 1.0<'toCurrency/'fromCurrency>
```

## Integration with C#

```csharp
// C# wrapper for F# financial functions
public static class FinancialCalculator
{
    private static readonly dynamic _billingModule = 
        Microsoft.FSharp.Core.CompilerServices.RuntimeHelpers
            .InitializeStaticData(
                typeof(Libr4.Tasks.Domain.TimeTracking.FSharp.Billing).TypeInitializer);
    
    public static InvoiceResult CalculateInvoice(
        double hoursWorked,
        double hourlyRateRUB,
        double taxPercent,
        double discountPercent)
    {
        // Call F# function with unit-safe wrapper
        var result = Billing.calculateInvoice(
            hoursWorked * 1.0<hour>,  // F# creates unit value
            hourlyRateRUB * 1.0<RUB/hour>,
            taxPercent * 1.0<percent>,
            discountPercent * 1.0<percent>
        );
        
        return new InvoiceResult
        {
            Subtotal = (double)result.Subtotal,
            Tax = (double)result.Tax,
            Discount = (double)result.Discount,
            Total = (double)result.Total
        };
    }
}

public class InvoiceResult
{
    public double Subtotal { get; set; }
    public double Tax { get; set; }
    public double Discount { get; set; }
    public double Total { get; set; }
}
```

## Production Rules

### 1. Always Use Units for Money
```fsharp
// ✅ GOOD
let price: float<USD> = 99.99<USD>

// ❌ BAD
let price: float = 99.99  // No unit - can be mixed up!
```

### 2. Explicit Conversions Only
```fsharp
// ✅ GOOD - explicit rate lookup
let convertUSDToRUB (usd: float<USD>) (rate: float) : float<RUB> =
    usd * rate * 1.0<RUB/USD>

// ❌ BAD - implicit magic number
let wrongConvert (usd: float<USD>) = usd * 90.0  // Where did 90 come from?
```

### 3. Track Time Precisely
```fsharp
// ✅ GOOD - precise time tracking
let billableHours = 40.0<hour>
let overtime = 5.0<hour>
let totalHours = billableHours + overtime  // 45.0<hour>

// ❌ BAD - mixing units
let workDays = 5.0<day>
// let total = billableHours + workDays  // COMPILER ERROR!
```

### 4. Validate at Boundaries
```fsharp
// When receiving external data
let parseMoney (value: decimal) (currency: string) : Result<float<'currency>, string> =
    if value >= 0.0M then
        Ok (float value * 1.0<'currency>)
    else
        Error "Amount cannot be negative"
```

## Testing

```fsharp
[<Fact>]
let ``Cannot mix currencies at compile time`` () =
    // This test exists just to verify compilation
    let rubles = 100.0<RUB>
    let dollars = 50.0<USD>
    
    // Uncomment to verify compile fails:
    // let mixed = rubles + dollars  // ERROR!
    
    Assert.True(true)  // If it compiles, test passes

[<Fact>]
let ``Commission calculation preserves currency`` () =
    let amount = 10000.0<RUB>
    let commission = calculateCommission amount 2.5<percent>
    
    Assert.Equal(250.0<RUB>, commission)
    Assert.IsType<float<RUB>>(commission)  // Still in rubles!
```

## Benefits for Libr4

1. **Escrow Safety**: Can't accidentally release wrong currency
2. **Billing Accuracy**: Time tracking always in correct units
3. **Tax Compliance**: Tax calculations type-safe by jurisdiction
4. **Audit Trail**: Units embedded in type signatures
5. **Developer Confidence**: Compiler prevents unit errors

## Migration Guide

1. **Identify** all financial/time calculations in F# modules
2. **Add** `[<Measure>]` types for currencies and time units
3. **Annotate** all function parameters with units
4. **Update** C# wrappers to pass unit-annotated values
5. **Test** that invalid unit mixing fails at compile time
6. **Deploy** with confidence - unit errors are now impossible!
