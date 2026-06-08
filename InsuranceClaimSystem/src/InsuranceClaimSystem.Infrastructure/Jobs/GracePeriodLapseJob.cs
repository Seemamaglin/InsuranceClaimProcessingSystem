using Hangfire;
using InsuranceClaimSystem.Application.Interfaces.Repositories;
using InsuranceClaimSystem.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace InsuranceClaimSystem.Infrastructure.Jobs;

[AutomaticRetry(Attempts = 3)]
public class GracePeriodLapseJob
{
    private readonly IPolicyRepository _policyRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<GracePeriodLapseJob> _logger;

    public GracePeriodLapseJob(
        IPolicyRepository policyRepository,
        IUnitOfWork unitOfWork,
        ILogger<GracePeriodLapseJob> logger)
    {
        _policyRepository = policyRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    // Schedule: "0 7 * * *" (daily at 7 AM)
    // Register via RecurringJob.AddOrUpdate<GracePeriodLapseJob>("grace-period-lapse", x => x.ExecuteAsync(), "0 7 * * *");
    public async Task ExecuteAsync()
    {
        _logger.LogInformation("GracePeriodLapseJob started at {Time}", DateTime.UtcNow);

        try
        {
            var policiesInGracePeriod = await _policyRepository.FindAsync(p =>
                p.Status == PolicyStatus.GracePeriod &&
                p.NextPremiumDueDate.AddDays(p.GracePeriodDays) < DateTime.UtcNow);

            foreach (var policy in policiesInGracePeriod)
            {
                if (policy.Status == PolicyStatus.CoverageExhausted || policy.Status == PolicyStatus.Cancelled)
                {
                    continue;
                }

                policy.Status = PolicyStatus.Lapsed;
                policy.LapsedAt = DateTime.UtcNow;

                _logger.LogInformation("Policy {PolicyId} lapsed due to grace period expiry", policy.Id);
            }

            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("GracePeriodLapseJob completed. Processed {Count} policies", policiesInGracePeriod.Count());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing GracePeriodLapseJob");
            throw;
        }
    }
}