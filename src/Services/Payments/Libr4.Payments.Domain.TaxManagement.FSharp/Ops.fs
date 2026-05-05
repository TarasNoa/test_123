namespace Libr4.Payments.Domain.TaxManagement.FSharp

open System

module TaxOps =
    let calculateTax (amount: decimal) (rate: decimal) : decimal = amount * rate / 100m
    
    let createCalculation (txId: Guid) (amount: decimal) (taxType: TaxType) (rate: decimal) (now: DateTimeOffset) : TaxCalculation =
        {
            id = Guid.NewGuid()
            transactionId = txId
            amount = amount
            taxType = taxType
            rate = rate
            taxAmount = calculateTax amount rate
            status = TaxStatus.Calculated
            calculatedAt = now
        }

    let getTaxRate (taxType: TaxType) : decimal =
        match taxType with
        | TaxType.VAT -> 20m
        | TaxType.GST -> 5m
        | TaxType.PST -> 7m
        | TaxType.Sales -> 8.5m
        | TaxType.Income -> 25m
        | TaxType.Custom _ -> 0m

module TaxFormOps =
    let createForm (userId: Guid) (formType: TaxFormType) (now: DateTimeOffset) : TaxForm =
        {
            id = Guid.NewGuid()
            userId = userId
            formType = formType
            data = Map.empty
            status = TaxStatus.Pending
            filedAt = None
            expiresAt = Some (now.AddYears(1))
            createdAt = now
        }

    let fileForm (now: DateTimeOffset) (form: TaxForm) : TaxForm =
        { form with status = TaxStatus.Filed; filedAt = Some now }

module TaxReportOps =
    let createReport (userId: Guid) (period: string) (totalIncome: decimal) (now: DateTimeOffset) : TaxReport =
        let totalTax = totalIncome * 0.25m
        {
            id = Guid.NewGuid()
            userId = userId
            period = period
            totalIncome = totalIncome
            totalTax = totalTax
            taxesPaid = 0m
            refund = None
            status = TaxStatus.Pending
            generatedAt = now
        }

    let recordPayment (amount: decimal) (now: DateTimeOffset) (report: TaxReport) : TaxReport =
        { report with taxesPaid = report.taxesPaid + amount; status = TaxStatus.Paid }
