namespace Libr4.Payments.Domain.EarlyPayment.FSharp

open System

module FactorOps =
    let createRequest (invoiceId: Guid) (userId: Guid) (invoiceAmount: decimal) (discountRate: decimal) (now: DateTimeOffset) : FactorRequest =
        {
            id = Guid.NewGuid()
            invoiceId = invoiceId
            userId = userId
            invoiceAmount = invoiceAmount
            requestedAmount = invoiceAmount
            discountRate = discountRate
            status = FactorStatus.Pending
            createdAt = now
        }

    let approve (now: DateTimeOffset) (request: FactorRequest) : FactorRequest =
        { request with status = FactorStatus.Approved }

    let fund (now: DateTimeOffset) (request: FactorRequest) : FactorRequest =
        { request with status = FactorStatus.Funded }

module LoanOps =
    let createLoan (factorId: Guid) (principal: decimal) (discountRate: decimal) (daysToMaturity: int) (now: DateTimeOffset) : EarlyPaymentLoan =
        let discountAmount = principal * discountRate / 100m
        {
            id = Guid.NewGuid()
            factorId = factorId
            principalAmount = principal
            discountAmount = discountAmount
            netAmount = principal - discountAmount
            dueDate = now.AddDays(float daysToMaturity)
            status = LoanStatus.Active
            repaidAmount = 0m
            createdAt = now
        }

    let recordRepayment (amount: decimal) (now: DateTimeOffset) (loan: EarlyPaymentLoan) : EarlyPaymentLoan =
        let newRepaid = loan.repaidAmount + amount
        let newStatus = if newRepaid >= loan.principalAmount then LoanStatus.Repaid else LoanStatus.Active
        { loan with repaidAmount = newRepaid; status = newStatus }
