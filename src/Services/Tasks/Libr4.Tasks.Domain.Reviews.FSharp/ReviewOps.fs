namespace Libr4.Tasks.Domain.Reviews.FSharp

open System

/// Review operations module
module ReviewOps =

    /// Create a new review
    let create (taskId: Guid) (reviewerId: Guid) (revieweeId: Guid) (targetType: ReviewTargetType) (rating: int) (title: string) (comment: string) (now: DateTimeOffset) : ReviewRecord =
        {
            id = Guid.NewGuid()
            taskId = taskId
            reviewerId = reviewerId
            revieweeId = revieweeId
            targetType = targetType
            rating = rating
            title = title
            comment = comment
            status = ReviewStatus.Pending
            isAnonymous = false
            communicationRating = None
            qualityRating = None
            deliveryRating = None
            valueRating = None
            attachments = []
            metadata = Map.empty
            createdAt = now
            updatedAt = now
        }

    /// Publish review
    let publish (now: DateTimeOffset) (review: ReviewRecord) : ReviewRecord =
        { review with status = ReviewStatus.Published; updatedAt = now }

    /// Archive review
    let archive (now: DateTimeOffset) (review: ReviewRecord) : ReviewRecord =
        { review with status = ReviewStatus.Archived; updatedAt = now }

    /// Set detailed ratings
    let setDetailedRatings (commRating: int option) (qualRating: int option) (delivRating: int option) (valRating: int option) (now: DateTimeOffset) (review: ReviewRecord) : ReviewRecord =
        { review with
            communicationRating = commRating
            qualityRating = qualRating
            deliveryRating = delivRating
            valueRating = valRating
            updatedAt = now
        }

    /// Add attachment
    let addAttachment (attachmentPath: string) (now: DateTimeOffset) (review: ReviewRecord) : ReviewRecord =
        { review with attachments = review.attachments @ [attachmentPath]; updatedAt = now }

    /// Get average detailed rating
    let getAverageDetailedRating (review: ReviewRecord) : float option =
        let ratings = [review.communicationRating; review.qualityRating; review.deliveryRating; review.valueRating]
        let values = ratings |> List.choose id
        if values.Length > 0 then Some (float (List.sum values) / float values.Length)
        else None

/// Review response operations module
module ReviewResponseOps =

    /// Create a response to review
    let create (reviewId: Guid) (responderId: Guid) (message: string) (now: DateTimeOffset) : ReviewResponseRecord =
        {
            id = Guid.NewGuid()
            reviewId = reviewId
            responderId = responderId
            message = message
            status = ReviewStatus.Published
            createdAt = now
            updatedAt = now
        }

    /// Update response
    let update (message: string) (now: DateTimeOffset) (response: ReviewResponseRecord) : ReviewResponseRecord =
        { response with message = message; updatedAt = now }

/// Review dispute operations module
module ReviewDisputeOps =

    /// Create a dispute
    let create (reviewId: Guid) (initiatorId: Guid) (reason: string) (description: string option) (now: DateTimeOffset) : ReviewDisputeRecord =
        {
            id = Guid.NewGuid()
            reviewId = reviewId
            initiatorId = initiatorId
            reason = reason
            description = description
            status = DisputeStatus.Open
            evidence = []
            resolution = None
            resolvedBy = None
            resolvedAt = None
            createdAt = now
            updatedAt = now
        }

    /// Add evidence
    let addEvidence (evidencePath: string) (now: DateTimeOffset) (dispute: ReviewDisputeRecord) : ReviewDisputeRecord =
        { dispute with evidence = dispute.evidence @ [evidencePath]; updatedAt = now }

    /// Resolve dispute
    let resolve (resolution: string) (resolvedBy: Guid) (now: DateTimeOffset) (dispute: ReviewDisputeRecord) : ReviewDisputeRecord =
        { dispute with
            status = DisputeStatus.Resolved
            resolution = Some resolution
            resolvedBy = Some resolvedBy
            resolvedAt = Some now
            updatedAt = now
        }

    /// Close dispute
    let close (now: DateTimeOffset) (dispute: ReviewDisputeRecord) : ReviewDisputeRecord =
        { dispute with status = DisputeStatus.Closed; updatedAt = now }

/// Rate history operations module
module RateHistoryOps =

    /// Calculate rate history
    let calculate (userId: Guid) (period: string) (reviews: ReviewRecord list) (now: DateTimeOffset) : RateHistoryRecord =
        let publishedReviews = reviews |> List.filter (fun r -> r.status = ReviewStatus.Published)
        let totalReviews = publishedReviews.Length
        let averageRating = 
            if totalReviews > 0 then
                float (List.sumBy (fun r -> r.rating) publishedReviews) / float totalReviews
            else 0.0
        
        let ratingDistribution =
            publishedReviews
            |> List.groupBy (fun r -> r.rating)
            |> List.map (fun (rating, group) -> (rating, group.Length))
            |> Map.ofList
        
        {
            id = Guid.NewGuid()
            userId = userId
            period = period
            averageRating = averageRating
            totalReviews = totalReviews
            ratingDistribution = ratingDistribution
            createdAt = now
        }

/// Badge operations module
module BadgeOps =

    /// Check if user qualifies for badge
    let qualifiesForBadge (requirement: BadgeRequirementRecord) (rateHistory: RateHistoryRecord) : bool =
        rateHistory.averageRating >= requirement.minRating &&
        rateHistory.totalReviews >= requirement.minReviews

    /// Award badge
    let awardBadge (userId: Guid) (badgeType: BadgeType) (now: DateTimeOffset) : BadgeRecord =
        {
            id = Guid.NewGuid()
            userId = userId
            badgeType = badgeType
            earnedAt = now
            expiresAt = None
            metadata = Map.empty
        }

    /// Check if badge is expired
    let isExpired (badge: BadgeRecord) : bool =
        match badge.expiresAt with
        | Some expiry -> DateTimeOffset.UtcNow > expiry
        | None -> false

    /// Get badge requirements
    let getBadgeRequirements (badgeType: BadgeType) : BadgeRequirementRecord =
        match badgeType with
        | BadgeType.TopRated -> { badgeType = badgeType; minRating = 4.8; minReviews = 10; minResponseRate = 0.95; minDeliveryRate = 0.95; validityMonths = Some 12 }
        | BadgeType.Verified -> { badgeType = badgeType; minRating = 4.0; minReviews = 5; minResponseRate = 0.90; minDeliveryRate = 0.90; validityMonths = Some 24 }
        | BadgeType.Responsive -> { badgeType = badgeType; minRating = 3.5; minReviews = 3; minResponseRate = 0.98; minDeliveryRate = 0.80; validityMonths = Some 12 }
        | BadgeType.Professional -> { badgeType = badgeType; minRating = 4.5; minReviews = 15; minResponseRate = 0.95; minDeliveryRate = 0.95; validityMonths = Some 12 }
        | BadgeType.Expert -> { badgeType = badgeType; minRating = 4.7; minReviews = 30; minResponseRate = 0.98; minDeliveryRate = 0.98; validityMonths = Some 6 }
        | BadgeType.Trusted -> { badgeType = badgeType; minRating = 4.6; minReviews = 20; minResponseRate = 0.96; minDeliveryRate = 0.96; validityMonths = Some 12 }
