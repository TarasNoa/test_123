namespace Libr4.Tasks.Domain.Tasks;

public enum TaskStatus
{
    Draft = 0,
    Published = 1,
    InProgress = 2,
    Completed = 3,
    Cancelled = 4,
    Disputed = 5
}

public enum TaskCategory
{
    Development = 0,
    Design = 1,
    Marketing = 2,
    Writing = 3,
    DataEntry = 4,
    Translation = 5,
    Other = 6
}
