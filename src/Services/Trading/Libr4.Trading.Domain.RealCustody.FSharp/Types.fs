namespace Libr4.Trading.Domain.RealCustody.FSharp

open System

type KYCStatus = NotSubmitted | Pending | Verified | Rejected | Expired
type KYCLevel = Basic | Enhanced | Institutional
type WithdrawalStatus = Pending | Approved | Rejected | Processing | Completed | Cancelled

type KYCProfile = {
    id: Guid
    userId: Guid
    status: KYCStatus
    level: KYCLevel
    firstName: string
    lastName: string
    dateOfBirth: DateTime
    country: string
    documentType: string
    documentNumber: string
    verifiedAt: DateTimeOffset option
    expiresAt: DateTimeOffset option
    createdAt: DateTimeOffset
}

type ColdWallet = {
    id: Guid
    userId: Guid
    address: string
    currency: string
    balance: decimal
    isSecure: bool
    requiresMultiSig: bool
    signerIds: Guid list
    lastAccessedAt: DateTimeOffset option
    createdAt: DateTimeOffset
}

type WithdrawalRequest = {
    id: Guid
    userId: Guid
    walletId: Guid
    toAddress: string
    amount: decimal
    currency: string
    status: WithdrawalStatus
    requiresApproval: bool
    approvedBy: Guid list
    fee: decimal
    txHash: string option
    requestedAt: DateTimeOffset
    processedAt: DateTimeOffset option
}

type AuditLog = {
    id: Guid
    userId: Guid
    action: string
    resource: string
    resourceId: Guid
    details: string
    ipAddress: string
    timestamp: DateTimeOffset
}
