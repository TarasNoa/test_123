namespace Libr4.AI.Domain.OrderAssistant.Algorithms

open System

[<CLIMutable>]
type FreelancerProfile =
    { Id: Guid
      Name: string
      Skills: string[]
      Rating: float
      CompletedTasks: int }

[<CLIMutable>]
type OrderAssistantResult =
    { SuggestedBudget: int
      SuggestedDuration: int
      RecommendedFreelancers: string[]
      Confidence: float32
      Reason: string }

module OrderAssistantAlgorithms =

    let private normalizeSkills (skills: string[]) =
        if isNull skills then [||]
        else skills |> Array.filter (fun s -> not (String.IsNullOrWhiteSpace s))

    let private scoreCandidate (requiredSkills: string[]) (candidate: FreelancerProfile) =
        let candidateSkills = normalizeSkills candidate.Skills
        let matchedSkills =
            requiredSkills
            |> normalizeSkills
            |> Array.filter(fun required ->
                candidateSkills
                |> Array.exists (fun skill -> String.Equals(skill, required, StringComparison.OrdinalIgnoreCase)))

        let score =
            float matchedSkills.Length * 2.0
            + candidate.Rating * 1.2
            + float (min candidate.CompletedTasks 20) * 0.05

        matchedSkills, score

    let suggestOrder
        (taskTitle: string)
        (description: string)
        (requiredSkills: string[])
        (budgetMin: int)
        (budgetMax: int)
        (durationDays: int)
        (candidateFreelancers: FreelancerProfile[])
        : OrderAssistantResult =

        let requiredSkills = normalizeSkills requiredSkills
        let candidates = if isNull candidateFreelancers then [||] else candidateFreelancers

        let ranked =
            candidates
            |> Array.map (fun candidate ->
                let matchedSkills, score = scoreCandidate requiredSkills candidate
                candidate.Name, matchedSkills, score)
            |> Array.sortByDescending (fun (_, _, score) -> score)
            |> Array.truncate 3

        let recommendedFreelancers = ranked |> Array.map (fun (name, _, _) -> name)

        let totalMatches =
            ranked
            |> Array.sumBy (fun (_, matched, _) -> float matched.Length)

        let skillCoverage =
            if requiredSkills.Length = 0 || candidates.Length = 0 then 0.0
            else
                let denominator = float (requiredSkills.Length * max 1 candidates.Length)
                min 1.0 (totalMatches / denominator)

        let budget =
            budgetMin
            + int (Math.Round(float (max budgetMax budgetMin - budgetMin) * skillCoverage))
            |> max budgetMin
            |> min budgetMax

        let duration =
            max 1 (int (Math.Round(float durationDays * max 0.7 (1.0 - skillCoverage * 0.25))))

        let confidence =
            float32 (min 1.0 (0.35 + skillCoverage * 0.55 + (if recommendedFreelancers.Length > 0 then 0.1 else 0.0)))

        let reason =
            if recommendedFreelancers.Length > 0 then
                "Подбор заказа выполнен по навыкам исполнителей и рейтингу."
            else
                "Недостаточно совпадений, расчет выполнен с учетом доступных данных."

        { SuggestedBudget = budget
          SuggestedDuration = duration
          RecommendedFreelancers = recommendedFreelancers
          Confidence = confidence
          Reason = reason }
