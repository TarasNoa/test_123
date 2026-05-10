namespace Libr4.AI.Domain.TaskRecommendations.Algorithms

open System

[<CLIMutable>]
type TaskBrief =
    { TaskId: Guid
      Title: string
      Category: string
      RequiredSkills: string[]
      EstimatedHours: int
      Description: string }

[<CLIMutable>]
type UserProfileSummary =
    { UserId: Guid
      Skills: string[]
      Interests: string[]
      AverageRating: float
      CompletedTasks: int }

[<CLIMutable>]
type TaskRecommendationResult =
    { TaskId: Guid
      Title: string
      MatchScore: float32
      MatchingSkills: string[]
      Reason: string }

module TaskRecommendationAlgorithms =

    let private normalize (values: string[]) =
        if isNull values then [||]
        else values |> Array.filter (fun value -> not (String.IsNullOrWhiteSpace value))

    let private containsIgnoreCase (text: string) (pattern: string) =
        not (String.IsNullOrWhiteSpace pattern)
        && text.IndexOf(pattern, StringComparison.OrdinalIgnoreCase) >= 0

    let recommendTasks
        (userProfile: UserProfileSummary)
        (availableTasks: TaskBrief[])
        : TaskRecommendationResult[] =

        let userSkills = normalize userProfile.Skills
        let userInterests = normalize userProfile.Interests

        availableTasks
        |> Array.map (fun task ->
            let requiredSkills = normalize task.RequiredSkills

            let matchingSkills =
                userSkills
                |> Array.filter (fun skill ->
                    requiredSkills
                    |> Array.exists (fun required -> String.Equals(skill, required, StringComparison.OrdinalIgnoreCase)))

            let interestMatchCount =
                userInterests
                |> Array.filter (containsIgnoreCase task.Category)
                |> Array.length

            let score =
                float matchingSkills.Length * 2.0
                + float interestMatchCount * 1.5
                + min (float userSkills.Length) 20.0 * 0.05
                + min (float userInterests.Length) 10.0 * 0.05

            let normalizedScore = float32 (min 1.0 (score / 10.0))

            { TaskId = task.TaskId
              Title = task.Title
              MatchScore = normalizedScore
              MatchingSkills = matchingSkills
              Reason =
                if matchingSkills.Length > 0 then
                    "Задача хорошо подходит по навыкам."
                else
                    "Задача рекомендуется на основе интересов и категории." })
        |> Array.sortByDescending (fun r -> r.MatchScore)
