namespace Libr4.Tasks.Domain.FSharp

open System

/// Task Management Domain Types
type TaskStatus =
    | Backlog
    | Todo
    | InProgress of startedAt: DateTime
    | InReview of reviewerId: string option
    | Done of completedAt: DateTime
    | Cancelled of reason: string

type TaskPriority =
    | Low
    | Medium
    | High
    | Critical

type TaskType =
    | Feature
    | Bug
    | TechnicalDebt
    | Documentation
    | Research
    | Custom of string

type Task = {
    Id: Guid
    Title: string
    Description: string
    Status: TaskStatus
    Priority: TaskPriority
    Type: TaskType
    AssigneeId: Guid option
    ReporterId: Guid
    ProjectId: Guid
    SprintId: Guid option
    ParentTaskId: Guid option
    SubtaskIds: Guid list
    EstimatedHours: decimal option
    ActualHours: decimal option
    DueDate: DateTime option
    Tags: string list
    CreatedAt: DateTime
    UpdatedAt: DateTime option
}

type Sprint = {
    Id: Guid
    Name: string
    ProjectId: Guid
    StartDate: DateTime
    EndDate: DateTime
    Goal: string option
    TaskIds: Guid list
    Status: SprintStatus
}

and SprintStatus =
    | Planning
    | Active
    | Completed
    | Cancelled

type Project = {
    Id: Guid
    Name: string
    Description: string
    OwnerId: Guid
    MemberIds: Guid list
    Status: ProjectStatus
    CreatedAt: DateTime
}

and ProjectStatus =
    | Active
    | OnHold
    | Archived
    | Deleted

/// Task Domain Operations
module TaskOperations =
    let createTask title description reporterId projectId =
        {
            Id = Guid.NewGuid()
            Title = title
            Description = description
            Status = Backlog
            Priority = Medium
            Type = Feature
            AssigneeId = None
            ReporterId = reporterId
            ProjectId = projectId
            SprintId = None
            ParentTaskId = None
            SubtaskIds = []
            EstimatedHours = None
            ActualHours = None
            DueDate = None
            Tags = []
            CreatedAt = DateTime.UtcNow
            UpdatedAt = None
        }
    
    let startTask task =
        match task.Status with
        | Todo -> { task with Status = InProgress(DateTime.UtcNow); UpdatedAt = Some DateTime.UtcNow }
        | _ -> task
    
    let completeTask task =
        match task.Status with
        | InProgress _ -> { task with Status = Done(DateTime.UtcNow); UpdatedAt = Some DateTime.UtcNow }
        | _ -> task
    
    let assignTask assigneeId task =
        { task with AssigneeId = Some assigneeId; UpdatedAt = Some DateTime.UtcNow }
    
    let unassignTask task =
        { task with AssigneeId = None; UpdatedAt = Some DateTime.UtcNow }
    
    let setPriority priority task =
        { task with Priority = priority; UpdatedAt = Some DateTime.UtcNow }
    
    let addSubtask subtaskId task =
        { task with SubtaskIds = subtaskId :: task.SubtaskIds; UpdatedAt = Some DateTime.UtcNow }
    
    let addToSprint sprintId task =
        { task with SprintId = Some sprintId; UpdatedAt = Some DateTime.UtcNow }
    
    let calculateProgress (tasks: Task list) =
        let total = tasks.Length
        if total = 0 then 0.0
        else
            let completed = tasks |> List.filter (fun t -> match t.Status with | Done _ -> true | _ -> false) |> List.length
            (float completed / float total) * 100.0
    
    let getTasksByStatus status tasks =
        tasks |> List.filter (fun t -> t.Status = status)
    
    let getOverdueTasks currentDate tasks =
        tasks |> List.filter (fun t ->
            match t.DueDate with
            | Some dueDate -> dueDate < currentDate && not (match t.Status with | Done _ -> true | _ -> false)
            | None -> false)
    
    let estimateSprintCapacity teamSize workingHoursPerDay sprintDays =
        decimal teamSize * decimal workingHoursPerDay * decimal sprintDays
    
    let validateTask task =
        if String.IsNullOrWhiteSpace(task.Title) then
            Error "Task title is required"
        elif task.Title.Length > 200 then
            Error "Task title too long (max 200)"
        else
            Ok task
