using Hangfire;
using InsuranceClaimSystem.Application.Interfaces.Repositories;
using InsuranceClaimSystem.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace InsuranceClaimSystem.Infrastructure.Jobs;

public class PolicyExpiryJob
{
    private readonly IPolicyRepository _policyRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<PolicyExpiryJob> _logger;

    public PolicyExpiryJob(
        IPolicyRepository policyRepository,
        IUnitOfWork unitOfWork,
        ILogger<PolicyExpiryJob> logger)
    {
        _policyRepository = policyRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    // Schedule: "0 0 * * *" (daily at midnight)
    // Register via RecurringJob.AddOrUpdate<PolicyExpiryJob>("policy-expiry", x => x.ExecuteAsync(), "0 0 * * *");
    public async Task ExecuteAsync()
    {
        _logger.LogInformation("PolicyExpiryJob started at {Time}", DateTime.UtcNow);

        try
        {
            var expiredPolicies = await _policyRepository.FindAsync(p =>
                p.Status == PolicyStatus.Active &&
                p.EndDate < DateTime.UtcNow);

            var terminalStatuses = new[]
            {
                PolicyStatus.CoverageExhausted,
                PolicyStatus.Cancelled,
                PolicyStatus.Expired,
                PolicyStatus.Lapsed,
                PolicyStatus.Rejected
            };

            foreach (var policy in expiredPolicies)
            {
                if (terminalStatuses.Contains(policy.Status))
                {
                    continue;
                }

                policy.Status = PolicyStatus.Expired;

                _logger.LogInformation("Policy {PolicyId} expired", policy.Id);
            }

            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("PolicyExpiryJob completed. Processed {Count} policies", expiredPolicies.Count());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing PolicyExpiryJob");
            throw;
        }
    }
}