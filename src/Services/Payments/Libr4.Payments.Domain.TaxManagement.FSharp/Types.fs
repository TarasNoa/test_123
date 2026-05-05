namespace Libr4.Payments.Domain.TaxManagement.FSharp

open System

type TaxType = VAT | GST | PST | Sales | Income | Custom of string
type TaxStatus = Pending | Calculated | Filed | Paid | Refunded
type TaxFormType = W9 | W8BEN | W8BEN_E | Invoice

type TaxCalculation = {
    id: Guid
    transactionId: Guid
    amount: decimal
    taxType: TaxType
    rate: decimal
    taxAmount: decimal
    status: TaxStatus
    calculatedAt: DateTimeOffset
}

type TaxForm = {
    id: Guid
    userId: Guid
    formType: TaxFormType
    data: Map<string, obj>
    status: TaxStatus
    filedAt: DateTimeOffset option
    expiresAt: DateTimeOffset option
    createdAt: DateTimeOffset
}

type TaxReport = {
    id: Guid
    userId: Guid
    period: string
    totalIncome: decimal
    totalTax: decimal
    taxesPaid: decimal
    refund: decimal option
    status: TaxStatus
    generatedAt: DateTimeOffset
}
