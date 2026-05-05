using Libr4.Shared.Kernel.Domain;

namespace Libr4.Auth.Domain.Skills;

public sealed class SkillCalibration : AggregateRoot<Guid>
{
    public Guid SkillTestId { get; private set; }
    public string SkillName { get; private set; } = "";
    public double CurrentDifficulty { get; private set; } = 0.5;
    public int TotalAttempts { get; private set; }
    public int PassedAttempts { get; private set; }
    public double PassRate { get; private set; }
    public double AverageScore { get; private set; }
    public DateTimeOffset LastCalibrationAt { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private SkillCalibration() { }

    public static SkillCalibration Create(Guid skillTestId, string skillName, DateTimeOffset now)
    {
        return new SkillCalibration
        {
            Id = Guid.NewGuid(),
            SkillTestId = skillTestId,
            SkillName = skillName,
            CurrentDifficulty = 0.5,
            TotalAttempts = 0,
            PassedAttempts = 0,
            PassRate = 0,
            AverageScore = 0,
            CreatedAt = now,
            LastCalibrationAt = now
        };
    }

    public void RecordAttempt(int score, bool passed, DateTimeOffset now)
    {
        TotalAttempts++;
        if (passed) PassedAttempts++;

        AverageScore = (AverageScore * (TotalAttempts - 1) + score) / TotalAttempts;
        PassRate = TotalAttempts > 0 ? (double)PassedAttempts / TotalAttempts : 0;

        CalibrateIfNeeded(now);
    }

    private void CalibrateIfNeeded(DateTimeOffset now)
    {
        if (TotalAttempts < 10) return;

        var targetPassRate = 0.65;
        var passRateDiff = PassRate - targetPassRate;

        if (Math.Abs(passRateDiff) > 0.15)
        {
            if (passRateDiff > 0)
                CurrentDifficulty = Math.Min(1.0, CurrentDifficulty + 0.05);
            else
                CurrentDifficulty = Math.Max(0.0, CurrentDifficulty - 0.05);

            LastCalibrationAt = now;
        }
    }

    public CalibrationMetrics GetMetrics()
    {
        return new CalibrationMetrics(
            Difficulty: CurrentDifficulty,
            PassRate: PassRate,
            AverageScore: AverageScore,
            TotalAttempts: TotalAttempts,
            PassedAttempts: PassedAttempts,
            Recommendation: GetRecommendation()
        );
    }

    private string GetRecommendation()
    {
        if (PassRate > 0.8) return "Increase difficulty";
        if (PassRate < 0.5) return "Decrease difficulty";
        return "Optimal difficulty";
    }
}

public record CalibrationMetrics(
    double Difficulty,
    double PassRate,
    double AverageScore,
    int TotalAttempts,
    int PassedAttempts,
    string Recommendation
);
