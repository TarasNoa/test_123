using System;
using Libr4.Shared.Kernel.Domain;
using Libr4.AI.Domain.InterviewQuestions.Events;

namespace Libr4.AI.Domain.InterviewQuestions;

public class InterviewQuestionSet : AggregateRoot<Guid>
{
    public Guid JobId { get; private set; }
    public string JobTitle { get; private set; } = string.Empty;
    public string JobDescription { get; private set; } = string.Empty;
    public List<InterviewQuestion> Questions { get; private set; } = new();
    public DateTimeOffset GeneratedAt { get; private set; }

    private InterviewQuestionSet() { }

    public void AddQuestion(string question, string category, string difficulty, DateTimeOffset now)
    {
        var interviewQuestion = new InterviewQuestion
        {
            Id = Guid.NewGuid(),
            Question = question,
            Category = category,
            Difficulty = difficulty
        };
        Questions.Add(interviewQuestion);
        GeneratedAt = now;
        RaiseDomainEvent(new QuestionAddedEvent(Id, JobId, question, category, now));
    }
}

public class InterviewQuestion
{
    public Guid Id { get; set; }
    public string Question { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty; // Technical, Behavioral, Situational
    public string Difficulty { get; set; } = string.Empty; // Easy, Medium, Hard
    public string? ExpectedAnswer { get; set; }
}
