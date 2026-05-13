namespace Libr4.Trading.Domain.RealCustody.FSharp

open System

module CustodyOps =
    let lockFunds (amount: decimal) (wallet: ColdWallet) =
        { wallet with balance = wallet.balance - amount }

    let unlockFunds (amount: decimal) (wallet: ColdWallet) =
        { wallet with balance = wallet.balance + amount }

    let canWithdraw (wallet: ColdWallet) (amount: decimal) : bool =
        wallet.balance >= amount && wallet.isSecure

module KYCOps =
    let verify (now: DateTimeOffset) (profile: KYCProfile) : KYCProfile =
        { profile with status = KYCStatus.Verified; verifiedAt = Some now; expiresAt = Some (now.AddYears(2)) }

    let isValid (profile: KYCProfile) (now: DateTimeOffset) : bool =
        match profile.status, profile.expiresAt with
        | KYCStatus.Verified, Some expiry -> now < expiry
        | _ -> false

    let canTransact (profile: KYCProfile) (amount: decimal) : bool =
        match profile.level with
        | KYCLevel.Basic -> amount <= 1000m
        | KYCLevel.Enhanced -> amount <= 100000m
        | KYCLevel.Institutional -> true

module WithdrawalOps =
    let approve (approverId: Guid) (request: WithdrawalRequest) : WithdrawalRequest =
        { request with approvedBy = approverId :: request.approvedBy }

    let canProcess (request: WithdrawalRequest) (requiredApprovals: int) : bool =
        not request.requiresApproval || List.length request.approvedBy >= requiredApprovals

    let complete (txHash: string) (now: DateTimeOffset) (request: WithdrawalRequest) : WithdrawalRequest =
        { request with status = WithdrawalStatus.Completed; txHash = Some txHash; processedAt = Some now }
