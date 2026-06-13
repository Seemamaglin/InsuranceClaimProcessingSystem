using Hangfire.Dashboard;

namespace InsuranceClaimSystem.API.Filters;

public class HangfireAdminAuthFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context) => true;
}