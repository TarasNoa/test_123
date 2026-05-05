using FluentValidation;
using Libr4.Auth.Application.Abstractions;
using Libr4.Auth.Domain.Skills;
using Libr4.Shared.Kernel.Application;
using Libr4.Shared.Kernel.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Libr4.Auth.Application.Skills;

public static class SkillCalibrationCommands
{
    public sealed record RecordSkillAttemptCommand(Guid SkillTestId, int Score, bool Passed) : IRequest<RecordSkillAttemptResponse>;

    public sealed record GetCalibrationMetricsQuery(Guid SkillTestId) : IRequest<CalibrationMetricsDto>;

    public sealed record RecordSkillAttemptResponse(Guid CalibrationId, CalibrationMetricsDto Metrics);

    public sealed class RecordSkillAttemptValidator : AbstractValidator<RecordSkillAttemptCommand>
    {
        public RecordSkillAttemptValidator()
        {
            RuleFor(x => x.SkillTestId).NotEmpty();
            RuleFor(x => x.Score).InclusiveBetween(0, 100);
        }
    }

    public sealed class RecordSkillAttemptHandler : IRequestHandler<RecordSkillAttemptCommand, RecordSkillAttemptResponse>
    {
        private readonly IAuthDbContext _db;

        public RecordSkillAttemptHandler(IAuthDbContext db) => _db = db;

        public async Task<RecordSkillAttemptResponse> Handle(RecordSkillAttemptCommand cmd, CancellationToken ct)
        {
            var calibration = await _db.SkillCalibrations
                .FirstOrDefaultAsync(x => x.SkillTestId == cmd.SkillTestId, ct)
                ?? throw new DomainException("Calibration not found");

            calibration.RecordAttempt(cmd.Score, cmd.Passed, DateTimeOffset.UtcNow);

            _db.SkillCalibrations.Update(calibration);
            await _db.SaveChangesAsync(ct);

            var metrics = calibration.GetMetrics();
            return new RecordSkillAttemptResponse(
                calibration.Id,
                new CalibrationMetricsDto(
                    metrics.Difficulty,
                    metrics.PassRate,
                    metrics.AverageScore,
                    metrics.TotalAttempts,
                    metrics.PassedAttempts,
                    metrics.Recommendation
                )
            );
        }
    }

    public sealed class GetCalibrationMetricsHandler : IRequestHandler<GetCalibrationMetricsQuery, CalibrationMetricsDto>
    {
        private readonly IAuthDbContext _db;

        public GetCalibrationMetricsHandler(IAuthDbContext db) => _db = db;

        public async Task<CalibrationMetricsDto> Handle(GetCalibrationMetricsQuery query, CancellationToken ct)
        {
            var calibration = await _db.SkillCalibrations
                .FirstOrDefaultAsync(x => x.SkillTestId == query.SkillTestId, ct)
                ?? throw new DomainException("Calibration not found");

            var metrics = calibration.GetMetrics();
            return new CalibrationMetricsDto(
                metrics.Difficulty,
                metrics.PassRate,
                metrics.AverageScore,
                metrics.TotalAttempts,
                metrics.PassedAttempts,
                metrics.Recommendation
            );
        }
    }
}

public sealed record CalibrationMetricsDto(
    double Difficulty,
    double PassRate,
    double AverageScore,
    int TotalAttempts,
    int PassedAttempts,
    string Recommendation
);
