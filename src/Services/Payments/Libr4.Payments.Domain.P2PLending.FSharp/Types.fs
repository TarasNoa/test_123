namespace Libr4.Payments.Domain.P2PLending.FSharp

open System

type LoanRequestStatus = Open | Funded | Active | Repaid | Defaulted | Cancelled
type CreditScore = Excellent | Good | Fair | Poor

type LoanRequest = {
    id: Guid
    borrowerId: Guid
    amount: decimal
    interestRate: decimal
    term: int
    purpose: string
    status: LoanRequestStatus
    fundedAmount: decimal
    createdAt: DateTimeOffset
}

type CreditProfile = {
    userId: Guid
    score: CreditScore
    totalBorrowed: decimal
    totalRepaid: decimal
    defaultCount: int
    lastUpdated: DateTimeOffset
}

type P2PLoan = {
    id: Guid
    requestId: Guid
    lenderId: Guid
    borrowerId: Guid
    principal: decimal
    interestRate: decimal
    monthlyPayment: decimal
    term: int
    status: LoanRequestStatus
    repaidAmount: decimal
    createdAt: DateTimeOffset
}
