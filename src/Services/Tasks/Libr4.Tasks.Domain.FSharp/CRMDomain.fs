namespace Libr4.Tasks.Domain.FSharp

open System

/// CRM Domain Types
type ContactStatus =
    | Lead
    | Prospect
    | Customer
    | Churned
    | Inactive

type ContactType =
    | Individual
    | Company
    | Partner
    | Vendor

type Contact = {
    Id: Guid
    Name: string
    Email: string option
    Phone: string option
    Company: string option
    Type: ContactType
    Status: ContactStatus
    Tags: string list
    AssignedTo: Guid option
    LastContactDate: DateTime option
    CreatedAt: DateTime
    Notes: string
}

type DealStage =
    | Prospecting
    | Qualification
    | Proposal
    | Negotiation
    | ClosedWon
    | ClosedLost

type Deal = {
    Id: Guid
    Name: string
    Description: string
    Value: decimal
    Currency: string
    Stage: DealStage
    ContactId: Guid
    AssignedTo: Guid
    Probability: int // 0-100
    ExpectedCloseDate: DateTime option
    ActualCloseDate: DateTime option
    CreatedAt: DateTime
}

type ActivityType =
    | Email
    | Call
    | Meeting
    | Note
    | Task

type Activity = {
    Id: Guid
    Type: ActivityType
    Description: string
    ContactId: Guid option
    DealId: Guid option
    AssignedTo: Guid
    DueDate: DateTime option
    CompletedAt: DateTime option
    CreatedAt: DateTime
}

/// CRM Operations
module CRMOperations =
    let createContact name email phone company contactType =
        {
            Id = Guid.NewGuid()
            Name = name
            Email = email
            Phone = phone
            Company = company
            Type = contactType
            Status = Lead
            Tags = []
            AssignedTo = None
            LastContactDate = None
            CreatedAt = DateTime.UtcNow
            Notes = ""
        }
    
    let convertToCustomer contact =
        { contact with Status = Customer }
    
    let markAsChurned contact =
        { contact with Status = Churned }
    
    let assignContact userId contact =
        { contact with AssignedTo = Some userId }
    
    let createDeal name description value currency contactId assignedTo =
        {
            Id = Guid.NewGuid()
            Name = name
            Description = description
            Value = value
            Currency = currency
            Stage = Prospecting
            ContactId = contactId
            AssignedTo = assignedTo
            Probability = 10
            ExpectedCloseDate = None
            ActualCloseDate = None
            CreatedAt = DateTime.UtcNow
        }
    
    let advanceDealStage deal =
        let newStage = match deal.Stage with
            | Prospecting -> Qualification
            | Qualification -> Proposal
            | Proposal -> Negotiation
            | Negotiation -> ClosedWon
            | ClosedWon -> ClosedWon
            | ClosedLost -> ClosedLost
        
        let newProb = match newStage with
            | Prospecting -> 10
            | Qualification -> 25
            | Proposal -> 50
            | Negotiation -> 75
            | ClosedWon -> 100
            | ClosedLost -> 0
        
        { deal with Stage = newStage; Probability = newProb }
    
    let closeDealWon deal =
        { deal with 
            Stage = ClosedWon
            Probability = 100
            ActualCloseDate = Some DateTime.UtcNow }
    
    let closeDealLost deal =
        { deal with 
            Stage = ClosedLost
            Probability = 0
            ActualCloseDate = Some DateTime.UtcNow }
    
    let calculateDealValue (deals: Deal list) =
        deals |> List.sumBy (fun d -> d.Value)
    
    let calculateWeightedForecast (deals: Deal list) =
        deals 
        |> List.filter (fun d -> match d.Stage with | ClosedWon | ClosedLost -> false | _ -> true)
        |> List.sumBy (fun d -> d.Value * (decimal d.Probability / 100m))
    
    let getDealsByStage stage deals =
        deals |> List.filter (fun d -> d.Stage = stage)
    
    let createActivity activityType description contactId dealId assignedTo dueDate =
        {
            Id = Guid.NewGuid()
            Type = activityType
            Description = description
            ContactId = contactId
            DealId = dealId
            AssignedTo = assignedTo
            DueDate = dueDate
            CompletedAt = None
            CreatedAt = DateTime.UtcNow
        }
    
    let completeActivity activity =
        { activity with CompletedAt = Some DateTime.UtcNow }
