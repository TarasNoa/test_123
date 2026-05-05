namespace Libr4.Tasks.Domain.Reviews.FSharp

open System

/// Review status discriminated union
type ReviewStatus =
    | Pending
    | Published
    | Disputed
    | Resolved
    | Archived

/// Review target type discriminated union
type ReviewTargetType =
    | Freelancer
    | Client
    | Task

/// Badge type discriminated union
type BadgeType =
    | TopRated
    | Verified
    | Responsive
    | Professional
    | Expert
    | Trusted

/// Dispute status discriminated union
type DisputeStatus =
    | Open
    | UnderReview
    | Resolved
    | Closed

/// Review record
type ReviewRecord = {
    id: Guid
    taskId: Guid
    reviewerId: Guid
    revieweeId: Guid
    targetType: ReviewTargetType
    rating: int // 1-5
    title: string
    comment: string
    status: ReviewStatus
    isAnonymous: bool
    
    // Детали оценки
    communicationRating: int option
    qualityRating: int option
    deliveryRating: int option
    valueRating: int option
    
    // Медиа
    attachments: string list
    
    // Метаданные
    metadata: Map<string, obj>
    createdAt: DateTimeOffset
    updatedAt: DateTimeOffset
}

/// Review response record
type ReviewResponseRecord = {
    id: Guid
    reviewId: Guid
    responderId: Guid
    message: string
    status: ReviewStatus
    createdAt: DateTimeOffset
    updatedAt: DateTimeOffset
}

/// Review dispute record
type ReviewDisputeRecord = {
    id: Guid
    reviewId: Guid
    initiatorId: Guid
    reason: string
    description: string option
    status: DisputeStatus
    evidence: string list
    resolution: string option
    resolvedBy: Guid option
    resolvedAt: DateTimeOffset option
    createdAt: DateTimeOffset
    updatedAt: DateTimeOffset
}

/// Rate history record
type RateHistoryRecord = {
    id: Guid
    userId: Guid
    period: string // "week", "month", "all_time"
    averageRating: float
    totalReviews: int
    ratingDistribution: Map<int, int> // rating -> count
    createdAt: DateTimeOffset
}

/// Badge record
type BadgeRecord = {
    id: Guid
    userId: Guid
    badgeType: BadgeType
    earnedAt: DateTimeOffset
    expiresAt: DateTimeOffset option
    metadata: Map<string, obj>
}

/// Badge requirement record
type BadgeRequirementRecord = {
    badgeType: BadgeType
    minRating: float
    minReviews: int
    minResponseRate: float
    minDeliveryRate: float
    validityMonths: int option
}
