module FeedbackModel

open HybridScorer

type FeedbackSignal =
    | Hired
    | Rejected
    | Applied
    | Viewed

let updateWeights
    (currentWeights: ScoringWeights)
    (matchScore: MatchScore)
    (feedback: FeedbackSignal)
    (learningRate: float) : ScoringWeights =

    let reward =
        match feedback with
        | Hired    ->  1.0
        | Applied  ->  0.3
        | Viewed   ->  0.1
        | Rejected -> -0.5

    let alpha = learningRate * reward

    let adjust w comp =
        let updated = w + alpha * comp * (1.0 - abs w)
        max 0.01 (min 0.99 updated)

    let raw =
        { KeywordSkillWeight = adjust currentWeights.KeywordSkillWeight matchScore.KeywordScore
          SemanticWeight     = adjust currentWeights.SemanticWeight matchScore.SemanticScore
          ExperienceWeight   = adjust currentWeights.ExperienceWeight matchScore.ExperienceScore
          ReputationWeight   = adjust currentWeights.ReputationWeight matchScore.ReputationScore
          RecencyWeight      = adjust currentWeights.RecencyWeight matchScore.RecencyScore
          BudgetFitWeight    = adjust currentWeights.BudgetFitWeight matchScore.BudgetFitScore }

    let total =
        raw.KeywordSkillWeight + raw.SemanticWeight + raw.ExperienceWeight
        + raw.ReputationWeight + raw.RecencyWeight + raw.BudgetFitWeight

    { KeywordSkillWeight = raw.KeywordSkillWeight / total
      SemanticWeight     = raw.SemanticWeight / total
      ExperienceWeight   = raw.ExperienceWeight / total
      ReputationWeight   = raw.ReputationWeight / total
      RecencyWeight      = raw.RecencyWeight / total
      BudgetFitWeight    = raw.BudgetFitWeight / total }
