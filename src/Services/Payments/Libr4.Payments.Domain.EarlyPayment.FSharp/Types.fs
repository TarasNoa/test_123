namespace Libr4.Payments.Domain.EarlyPayment.FSharp

open System

type FactorStatus = Pending | Approved | Funded | Completed | Rejected
type LoanStatus = Active | Repaid | Defaulted | Cancelled

type FactorRequest = {
    id: Guid
    invoiceId: Guid
    userId: Guid
    invoiceAmount: decimal
    requestedAmount: decimal
    discountRate: decimal
    status: FactorStatus
    createdAt: DateTimeOffset
}

type EarlyPaymentLoan = {
    id: Guid
    factorId: Guid
    principalAmount: decimal
    discountAmount: decimal
    netAmount: decimal
    dueDate: DateTimeOffset
    status: LoanStatus
    repaidAmount: decimal
    createdAt: DateTimeOffset
}
