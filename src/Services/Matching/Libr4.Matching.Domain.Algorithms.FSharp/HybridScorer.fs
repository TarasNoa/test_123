module HybridScorer

open System

[<CLIMutable>]
type ScoringWeights =
    { KeywordSkillWeight : float
      SemanticWeight     : float
      ExperienceWeight   : float
      ReputationWeight   : float
      RecencyWeight      : float
      BudgetFitWeight    : float }

let defaultWeights =
    { KeywordSkillWeight = 0.25
      SemanticWeight     = 0.40
      ExperienceWeight   = 0.10
      ReputationWeight   = 0.15
      RecencyWeight      = 0.05
      BudgetFitWeight    = 0.05 }

[<CLIMutable>]
type TaskProfile =
    { TaskId          : Guid
      Title           : string
      Description     : string
      RequiredSkills  : string[]
      BudgetMin       : int
      BudgetMax       : int
      DurationDays    : int
      PostedAt        : DateTimeOffset
      Embedding       : float32[] }

[<CLIMutable>]
type FreelancerProfile =
    { FreelancerId    : Guid
      Skills          : string[]
      Interests       : string[]
      AverageRating   : float
      CompletedTasks  : int
      HourlyRateMin   : int
      HourlyRateMax   : int
      Embedding       : float32[] }

[<CLIMutable>]
type MatchScore =
    { FreelancerId    : Guid
      TaskId          : Guid
      TotalScore      : float
      KeywordScore    : float
      SemanticScore   : float
      ExperienceScore : float
      ReputationScore : float
      RecencyScore    : float
      BudgetFitScore  : float
      MatchingSkills  : string[]
      Explanation     : string }

let private keywordScore (task: TaskProfile) (freelancer: FreelancerProfile) =
    let matched = SkillNormalizer.intersection task.RequiredSkills freelancer.Skills
    let total = SkillNormalizer.normalizeAll task.RequiredSkills |> Array.length
    if total = 0 then 0.5
    else float matched.Length / float total

let private semanticScore (task: TaskProfile) (freelancer: FreelancerProfile) =
    if task.Embedding = null || task.Embedding.Length = 0
       || freelancer.Embedding = null || freelancer.Embedding.Length = 0
    then 0.0
    else
        let dot =
            Array.map2 (fun a b -> float a * float b) task.Embedding freelancer.Embedding
            |> Array.sum
        let normT = task.Embedding |> Array.sumBy (fun x -> float x * float x) |> sqrt
        let normF = freelancer.Embedding |> Array.sumBy (fun x -> float x * float x) |> sqrt
        if normT < 1e-8 || normF < 1e-8 then 0.0
        else min 1.0 (max 0.0 (dot / (normT * normF)))

let private experienceScore (freelancer: FreelancerProfile) =
    min 1.0 (log (float freelancer.CompletedTasks + 1.0) / log 51.0)

let private reputationScore (freelancer: FreelancerProfile) =
    min 1.0 (max 0.0 (freelancer.AverageRating / 5.0))

let private recencyScore (task: TaskProfile) =
    let age = (DateTimeOffset.UtcNow - task.PostedAt).TotalDays
    max 0.0 (1.0 - age / 30.0)

let private budgetFitScore (task: TaskProfile) (freelancer: FreelancerProfile) =
    let taskMax = float task.BudgetMax
    let taskMin = float task.BudgetMin
    let fMin    = float freelancer.HourlyRateMin
    let fMax    = float freelancer.HourlyRateMax
    let overlapMin = max taskMin fMin
    let overlapMax = min taskMax fMax
    if overlapMin <= overlapMax then
        let overlap   = overlapMax - overlapMin
        let taskRange = max 1.0 (taskMax - taskMin)
        min 1.0 (overlap / taskRange)
    else 0.0

let private buildExplanation
    (task: TaskProfile)
    (freelancer: FreelancerProfile)
    (score: float)
    (matchingSkills: string[]) =
    let skillPart =
        if matchingSkills.Length > 0 then
            sprintf "Совпадают навыки: %s." (String.concat ", " matchingSkills)
        else
            "Совпадение по навыкам слабое, но профиль семантически близок задаче."
    let scorePart =
        if score > 0.8 then "Отличное совпадение."
        elif score > 0.6 then "Хорошее совпадение."
        elif score > 0.4 then "Умеренное совпадение."
        else "Слабое совпадение, рассмотрите других кандидатов."
    sprintf "%s %s" skillPart scorePart

let scoreMatch
    (weights: ScoringWeights)
    (task: TaskProfile)
    (freelancer: FreelancerProfile)
    : MatchScore =
    let kw   = keywordScore task freelancer
    let sem  = semanticScore task freelancer
    let exp  = experienceScore freelancer
    let rep  = reputationScore freelancer
    let rec_ = recencyScore task
    let bud  = budgetFitScore task freelancer
    let total =
        kw  * weights.KeywordSkillWeight
        + sem * weights.SemanticWeight
        + exp * weights.ExperienceWeight
        + rep * weights.ReputationWeight
        + rec_ * weights.RecencyWeight
        + bud * weights.BudgetFitWeight
    let matched = SkillNormalizer.intersection task.RequiredSkills freelancer.Skills
    { FreelancerId    = freelancer.FreelancerId
      TaskId          = task.TaskId
      TotalScore      = min 1.0 (max 0.0 total)
      KeywordScore    = kw
      SemanticScore   = sem
      ExperienceScore = exp
      ReputationScore = rep
      RecencyScore    = rec_
      BudgetFitScore  = bud
      MatchingSkills  = matched
      Explanation     = buildExplanation task freelancer total matched }

let rankFreelancers
    (weights: ScoringWeights)
    (task: TaskProfile)
    (freelancers: FreelancerProfile[])
    : MatchScore[] =
    if isNull freelancers || freelancers.Length = 0 then [||]
    else
        freelancers
        |> Array.map (scoreMatch weights task)
        |> Array.sortByDescending (fun s -> s.TotalScore)
