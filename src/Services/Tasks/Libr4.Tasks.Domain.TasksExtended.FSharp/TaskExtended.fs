namespace Libr4.Tasks.Domain.TasksExtended.FSharp

open System

/// Task extended operations module
module TaskExtendedOps =

    /// Create a new task from draft
    let createFromDraft (draft: TaskDraft) (creatorId: Guid) (now: DateTimeOffset) : TaskExtendedRecord =
        {
            id = Guid.NewGuid()
            title = draft.title |> Option.defaultValue ""
            description = draft.description |> Option.defaultValue ""
            category = draft.category |> Option.defaultValue (TaskCategory.Other "")
            subcategory = None
            budgetMin = draft.budget |> Option.map fst
            budgetMax = draft.budget |> Option.map snd
            priority = TaskPriority.Medium
            status = TaskStatus.Open
            moderationStatus = ModerationStatus.Pending
            creatorId = creatorId
            assignedToId = None
            projectManagerId = None
            isRemote = draft.isRemote
            location = draft.location
            skillsRequired = draft.skillsRequired
            tags = []
            deadline = None
            estimatedHours = None
            complexity = 5
            isMultiUser = false
            maxTeamSize = 1
            currentTeamSize = 0
            isDraft = false
            draftId = Some draft.id
            templateId = None
            isRecurring = false
            recurringConfig = None
            milestones = []
            aiComplexityScore = None
            aiSuggestedMinPrice = None
            aiSuggestedMaxPrice = None
            aiAnalysisData = Map.empty
            aiAnalyzedAt = None
            metadata = Map.empty
            createdAt = now
            updatedAt = now
        }

    /// Create a new task from template
    let createFromTemplate (template: TaskTemplate) (creatorId: Guid) (title: string) (description: string) (now: DateTimeOffset) : TaskExtendedRecord =
        {
            id = Guid.NewGuid()
            title = title
            description = description
            category = template.category
            subcategory = None
            budgetMin = template.defaultBudget |> Option.map fst
            budgetMax = template.defaultBudget |> Option.map snd
            priority = template.defaultPriority
            status = TaskStatus.Open
            moderationStatus = ModerationStatus.Pending
            creatorId = creatorId
            assignedToId = None
            projectManagerId = None
            isRemote = true
            location = None
            skillsRequired = template.skillsRequired
            tags = template.tags
            deadline = template.defaultDuration |> Option.map (fun days -> now.AddDays(float days))
            estimatedHours = None
            complexity = 5
            isMultiUser = false
            maxTeamSize = 1
            currentTeamSize = 0
            isDraft = false
            draftId = None
            templateId = Some template.id
            isRecurring = false
            recurringConfig = None
            milestones = []
            aiComplexityScore = None
            aiSuggestedMinPrice = None
            aiSuggestedMaxPrice = None
            aiAnalysisData = Map.empty
            aiAnalyzedAt = None
            metadata = Map.empty
            createdAt = now
            updatedAt = now
        }

    /// Create a recurring task
    let createRecurring (baseTask: TaskExtendedRecord) (frequency: RecurringFrequency) (startDate: DateTimeOffset) (endDate: DateTimeOffset option) (maxOccurrences: int option) (now: DateTimeOffset) : TaskExtendedRecord * RecurringTaskConfig =
        let config: RecurringTaskConfig = {
            id = Guid.NewGuid()
            taskId = baseTask.id
            frequency = frequency
            startDate = startDate
            endDate = endDate
            maxOccurrences = maxOccurrences
            dayOfWeek = None
            dayOfMonth = None
            nextOccurrenceDate = startDate
            occurrenceCount = 0
            isActive = true
            createdAt = now
            updatedAt = now
        }
        let updatedTask = { baseTask with isRecurring = true; recurringConfig = Some config; deadline = endDate }
        (updatedTask, config)

    /// Add a milestone to task
    let addMilestone (title: string) (description: string option) (dueDate: DateTimeOffset) (budget: decimal option) (deliverables: string list) (now: DateTimeOffset) (task: TaskExtendedRecord) : TaskExtendedRecord =
        let milestone: Milestone = {
            id = Guid.NewGuid()
            taskId = task.id
            title = title
            description = description
            dueDate = dueDate
            budget = budget
            status = TaskStatus.Open
            deliverables = deliverables
            order = task.milestones.Length + 1
            createdAt = now
            updatedAt = now
        }
        { task with milestones = task.milestones @ [milestone]; updatedAt = now }

    /// Complete a milestone
    let completeMilestone (milestoneId: Guid) (now: DateTimeOffset) (task: TaskExtendedRecord) : TaskExtendedRecord =
        let updatedMilestones =
            task.milestones
            |> List.map (fun m ->
                if m.id = milestoneId then { m with status = TaskStatus.Completed; updatedAt = now }
                else m)
        { task with milestones = updatedMilestones; updatedAt = now }

    /// Update task status
    let updateStatus (newStatus: TaskStatus) (now: DateTimeOffset) (task: TaskExtendedRecord) : TaskExtendedRecord =
        { task with status = newStatus; updatedAt = now }

    /// Update moderation status
    let updateModerationStatus (newStatus: ModerationStatus) (now: DateTimeOffset) (task: TaskExtendedRecord) : TaskExtendedRecord =
        { task with moderationStatus = newStatus; updatedAt = now }

    /// Set task as multi-user
    let setMultiUser (maxTeamSize: int) (now: DateTimeOffset) (task: TaskExtendedRecord) : TaskExtendedRecord =
        { task with isMultiUser = true; maxTeamSize = maxTeamSize; updatedAt = now }

    /// Get complexity level based on milestones and team size
    let calculateComplexity (task: TaskExtendedRecord) : int =
        let milestoneComplexity = task.milestones.Length * 2
        let teamComplexity = if task.isMultiUser then task.maxTeamSize * 3 else 0
        min 10 (milestoneComplexity + teamComplexity)

    /// Check if task is overdue
    let isOverdue (task: TaskExtendedRecord) : bool =
        match task.deadline with
        | Some deadline -> DateTimeOffset.UtcNow > deadline
        | None -> false

    /// Get remaining days until deadline
    let daysUntilDeadline (task: TaskExtendedRecord) : int option =
        task.deadline
        |> Option.map (fun deadline ->
            let remaining = (deadline - DateTimeOffset.UtcNow).Days in
            max 0 remaining)

/// Task draft operations module
module TaskDraftOps =

    /// Create a new draft
    let create (userId: Guid) (now: DateTimeOffset) : TaskDraft =
        {
            id = Guid.NewGuid()
            userId = userId
            title = None
            description = None
            category = None
            budget = None
            priority = None
            isRemote = true
            location = None
            skillsRequired = []
            createdAt = now
            updatedAt = now
            expiresAt = Some (now.AddDays(30.0))
        }

    /// Update draft title
    let updateTitle (title: string) (now: DateTimeOffset) (draft: TaskDraft) : TaskDraft =
        { draft with title = Some (title.Trim()); updatedAt = now }

    /// Update draft description
    let updateDescription (description: string) (now: DateTimeOffset) (draft: TaskDraft) : TaskDraft =
        { draft with description = Some (description.Trim()); updatedAt = now }

    /// Update draft category
    let updateCategory (category: TaskCategory) (now: DateTimeOffset) (draft: TaskDraft) : TaskDraft =
        { draft with category = Some category; updatedAt = now }

    /// Update draft budget
    let updateBudget (minBudget: decimal) (maxBudget: decimal) (now: DateTimeOffset) (draft: TaskDraft) : TaskDraft =
        { draft with budget = Some (minBudget, maxBudget); updatedAt = now }

    /// Add skill to draft
    let addSkill (skill: string) (now: DateTimeOffset) (draft: TaskDraft) : TaskDraft =
        let skills = if List.contains skill draft.skillsRequired then draft.skillsRequired else draft.skillsRequired @ [skill]
        { draft with skillsRequired = skills; updatedAt = now }

    /// Check if draft is complete enough to publish
    let isPublishable (draft: TaskDraft) : bool =
        draft.title.IsSome &&
        draft.description.IsSome &&
        draft.category.IsSome &&
        draft.skillsRequired.Length > 0

    /// Check if draft has expired
    let isExpired (draft: TaskDraft) : bool =
        match draft.expiresAt with
        | Some expiry -> DateTimeOffset.UtcNow > expiry
        | None -> false

/// Task template operations module
module TaskTemplateOps =

    /// Create a new template
    let create (userId: Guid) (name: string) (description: string) (category: TaskCategory) (skillsRequired: string list) (now: DateTimeOffset) : TaskTemplate =
        {
            id = Guid.NewGuid()
            userId = userId
            name = name
            description = description
            category = category
            defaultBudget = None
            defaultPriority = TaskPriority.Medium
            skillsRequired = skillsRequired
            defaultDuration = None
            tags = []
            isPublic = false
            usageCount = 0
            createdAt = now
            updatedAt = now
        }

    /// Increment usage count
    let incrementUsage (now: DateTimeOffset) (template: TaskTemplate) : TaskTemplate =
        { template with usageCount = template.usageCount + 1; updatedAt = now }

    /// Publish template
    let publish (now: DateTimeOffset) (template: TaskTemplate) : TaskTemplate =
        { template with isPublic = true; updatedAt = now }

    /// Unpublish template
    let unpublish (now: DateTimeOffset) (template: TaskTemplate) : TaskTemplate =
        { template with isPublic = false; updatedAt = now }

/// Recurring task operations module
module RecurringTaskOps =

    /// Calculate next occurrence date
    let calculateNextOccurrence (config: RecurringTaskConfig) : DateTimeOffset =
        match config.frequency with
        | RecurringFrequency.Daily -> config.nextOccurrenceDate.AddDays(1.0)
        | RecurringFrequency.Weekly -> config.nextOccurrenceDate.AddDays(7.0)
        | RecurringFrequency.BiWeekly -> config.nextOccurrenceDate.AddDays(14.0)
        | RecurringFrequency.Monthly -> config.nextOccurrenceDate.AddMonths(1)
        | RecurringFrequency.Quarterly -> config.nextOccurrenceDate.AddMonths(3)
        | RecurringFrequency.Yearly -> config.nextOccurrenceDate.AddYears(1)
        | RecurringFrequency.Custom days -> config.nextOccurrenceDate.AddDays(float days)

    /// Check if should create next occurrence
    let shouldCreateNext (config: RecurringTaskConfig) : bool =
        let now = DateTimeOffset.UtcNow
        let withinEndDate = match config.endDate with Some ed -> now <= ed | None -> true
        let withinMaxOccurrences = match config.maxOccurrences with Some m -> config.occurrenceCount < m | None -> true
        config.isActive && withinEndDate && withinMaxOccurrences && now >= config.nextOccurrenceDate

    /// Advance to next occurrence
    let advanceToNext (now: DateTimeOffset) (config: RecurringTaskConfig) : RecurringTaskConfig =
        { config with nextOccurrenceDate = calculateNextOccurrence config; occurrenceCount = config.occurrenceCount + 1; updatedAt = now }

    /// Deactivate recurring config
    let deactivate (now: DateTimeOffset) (config: RecurringTaskConfig) : RecurringTaskConfig =
        { config with isActive = false; updatedAt = now }
