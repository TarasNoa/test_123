namespace Libr4.Payments.Domain.P2PLending.FSharp

open System

module CreditOps =
    let calculateScore (totalRepaid: decimal) (totalBorrowed: decimal) (defaultCount: int) : CreditScore =
        let repaymentRate = if totalBorrowed > 0m then totalRepaid / totalBorrowed else 0m
        match defaultCount, repaymentRate with
        | 0, rate when rate >= 0.95m -> CreditScore.Excellent
        | 0, rate when rate >= 0.85m -> CreditScore.Good
        | 0, rate when rate >= 0.70m -> CreditScore.Fair
        | _ -> CreditScore.Poor

    let createProfile (userId: Guid) (now: DateTimeOffset) : CreditProfile =
        {
            userId = userId
            score = CreditScore.Fair
            totalBorrowed = 0m
            totalRepaid = 0m
            defaultCount = 0
            lastUpdated = now
        }

module LoanRequestOps =
    let create (borrowerId: Guid) (amount: decimal) (interestRate: decimal) (term: int) (purpose: string) (now: DateTimeOffset) : LoanRequest =
        {
            id = Guid.NewGuid()
            borrowerId = borrowerId
            amount = amount
            interestRate = interestRate
            term = term
            purpose = purpose
            status = LoanRequestStatus.Open
            fundedAmount = 0m
            createdAt = now
        }

    let fund (amount: decimal) (request: LoanRequest) : LoanRequest =
        let newFunded = request.fundedAmount + amount
        let newStatus = if newFunded >= request.amount then LoanRequestStatus.Funded else LoanRequestStatus.Open
        { request with fundedAmount = newFunded; status = newStatus }

module P2PLoanOps =
    let create (requestId: Guid) (lenderId: Guid) (borrowerId: Guid) (principal: decimal) (interestRate: decimal) (term: int) (now: DateTimeOffset) : P2PLoan =
        let monthlyRate = float (interestRate / 100m / 12m)
        let monthlyPayment = decimal (float principal * monthlyRate / (1.0 - (1.0 + monthlyRate) ** float (-term)))
        {
            id = Guid.NewGuid()
            requestId = requestId
            lenderId = lenderId
            borrowerId = borrowerId
            principal = principal
            interestRate = interestRate
            monthlyPayment = monthlyPayment
            term = term
            status = LoanRequestStatus.Active
            repaidAmount = 0m
            createdAt = now
        }

    let recordPayment (amount: decimal) (loan: P2PLoan) : P2PLoan =
        let newRepaid = loan.repaidAmount + amount
        let newStatus = if newRepaid >= loan.principal then LoanRequestStatus.Repaid else LoanRequestStatus.Active
        { loan with repaidAmount = newRepaid; status = newStatus }
