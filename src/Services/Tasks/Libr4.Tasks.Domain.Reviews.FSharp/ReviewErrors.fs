namespace Libr4.Tasks.Domain.Reviews.FSharp

/// Domain errors for reviews
module ReviewErrors =

    /// Error type for reviews domain
    type ReviewError =
        | ReviewNotFound
        | ResponseNotFound
        | DisputeNotFound
        | NotReviewOwner
        | NotReviewee
        | InvalidRating
        | ReviewAlreadyPublished
        | CannotModifyPublishedReview
        | InvalidDisputeStatus
        | DisputeAlreadyResolved
        | InvalidBadgeType
        | UserDoesNotQualifyForBadge

    /// Convert error to message
    let errorMessage = function
        | ReviewNotFound -> "Review not found"
        | ResponseNotFound -> "Response not found"
        | DisputeNotFound -> "Dispute not found"
        | NotReviewOwner -> "You are not the owner of this review"
        | NotReviewee -> "You are not the reviewee"
        | InvalidRating -> "Rating must be between 1 and 5"
        | ReviewAlreadyPublished -> "Review is already published"
        | CannotModifyPublishedReview -> "Cannot modify a published review"
        | InvalidDisputeStatus -> "Invalid dispute status transition"
        | DisputeAlreadyResolved -> "Dispute is already resolved"
        | InvalidBadgeType -> "Invalid badge type"
        | UserDoesNotQualifyForBadge -> "User does not qualify for this badge"

    /// Validation result type
    type ValidationResult<'T> = Result<'T, ReviewError>

    /// Validate rating
    let validateRating (rating: int) : ValidationResult<int> =
        if rating >= 1 && rating <= 5 then Ok rating
        else Error InvalidRating

    /// Validate review ownership
    let validateReviewOwner (userId: System.Guid) (review: ReviewRecord) : ValidationResult<unit> =
        if review.reviewerId = userId then Ok ()
        else Error NotReviewOwner

    /// Validate review not published
    let validateReviewNotPublished (review: ReviewRecord) : ValidationResult<unit> =
        if review.status <> ReviewStatus.Published then Ok ()
        else Error ReviewAlreadyPublished

    /// Validate dispute status transition
    let validateDisputeStatusTransition (currentStatus: DisputeStatus) (newStatus: DisputeStatus) : ValidationResult<unit> =
        match currentStatus, newStatus with
        | DisputeStatus.Closed, _ -> Error InvalidDisputeStatus
        | DisputeStatus.Resolved, DisputeStatus.Open -> Error InvalidDisputeStatus
        | _, _ -> Ok ()

    /// Validate badge qualification
    let validateBadgeQualification (requirement: BadgeRequirementRecord) (rateHistory: RateHistoryRecord) : ValidationResult<unit> =
        if BadgeOps.qualifiesForBadge requirement rateHistory then Ok ()
        else Error UserDoesNotQualifyForBadge
