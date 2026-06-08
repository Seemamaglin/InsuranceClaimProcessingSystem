using InsuranceClaimSystem.Domain.Entities;
using InsuranceClaimSystem.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace InsuranceClaimSystem.Infrastructure.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext context)
    {
        // Seed only if tables are empty
        if (await context.Users.AnyAsync())
            return;

        // --- a) Admin User ---
        var adminUser = new User
        {
            Id = Guid.NewGuid(),
            FirstName = "System",
            LastName = "Admin",
            DateOfBirth = new DateTime(1990, 1, 1),
            Username = "admin",
            Email = "admin@insuranceclaimsystem.com",
            EmailVerifiedAt = DateTime.UtcNow,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123!"),
            PhoneNumber = "+91-9999999999",
            Role = UserRole.Admin,
            RegistrationStatus = RegistrationStatus.Approved,
            IsActive = true,
            IsFirstLogin = true,
            FailedLoginAttempts = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IsDeleted = false
        };

        await context.Users.AddAsync(adminUser);

        // --- b) PolicyTypes ---
        var healthPolicyType = new PolicyType
        {
            Id = Guid.NewGuid(),
            TypeName = "Health",
            Description = "Health insurance policy covering medical expenses",
            DefaultBenefitType = BenefitType.Reimbursement,
            AllowsNomineeClaim = false,
            AllowsThirdPartyClaim = false,
            DefaultCoverageAmount = 500000,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IsDeleted = false
        };

        var autoPolicyType = new PolicyType
        {
            Id = Guid.NewGuid(),
            TypeName = "Auto",
            Description = "Auto insurance policy covering vehicle damages",
            DefaultBenefitType = BenefitType.Reimbursement,
            AllowsNomineeClaim = false,
            AllowsThirdPartyClaim = true,
            DefaultCoverageAmount = 300000,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IsDeleted = false
        };

        var lifePolicyType = new PolicyType
        {
            Id = Guid.NewGuid(),
            TypeName = "Life",
            Description = "Life insurance policy providing financial security",
            DefaultBenefitType = BenefitType.FixedBenefit,
            AllowsNomineeClaim = true,
            AllowsThirdPartyClaim = false,
            DefaultCoverageAmount = 1000000,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IsDeleted = false
        };

        var propertyPolicyType = new PolicyType
        {
            Id = Guid.NewGuid(),
            TypeName = "Property",
            Description = "Property insurance policy covering damages to property",
            DefaultBenefitType = BenefitType.Reimbursement,
            AllowsNomineeClaim = false,
            AllowsThirdPartyClaim = false,
            DefaultCoverageAmount = 5000000,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IsDeleted = false
        };

        await context.PolicyTypes.AddRangeAsync(healthPolicyType, autoPolicyType, lifePolicyType, propertyPolicyType);

        // --- c) ClaimTypes ---
        var claimTypes = new List<ClaimType>
        {
            // Health
            new ClaimType
            {
                Id = Guid.NewGuid(),
                TypeName = "Hospitalization",
                IsMaturityClaim = false,
                IsFixedBenefit = false,
                PolicyTypeId = healthPolicyType.Id,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsDeleted = false
            },
            new ClaimType
            {
                Id = Guid.NewGuid(),
                TypeName = "DayCare",
                IsMaturityClaim = false,
                IsFixedBenefit = false,
                PolicyTypeId = healthPolicyType.Id,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsDeleted = false
            },
            // Auto
            new ClaimType
            {
                Id = Guid.NewGuid(),
                TypeName = "Accident",
                IsMaturityClaim = false,
                IsFixedBenefit = false,
                PolicyTypeId = autoPolicyType.Id,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsDeleted = false
            },
            new ClaimType
            {
                Id = Guid.NewGuid(),
                TypeName = "Theft",
                IsMaturityClaim = false,
                IsFixedBenefit = false,
                PolicyTypeId = autoPolicyType.Id,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsDeleted = false
            },
            // Life
            new ClaimType
            {
                Id = Guid.NewGuid(),
                TypeName = "Death",
                IsMaturityClaim = false,
                IsFixedBenefit = true,
                PolicyTypeId = lifePolicyType.Id,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsDeleted = false
            },
            new ClaimType
            {
                Id = Guid.NewGuid(),
                TypeName = "Maturity",
                IsMaturityClaim = true,
                IsFixedBenefit = true,
                PolicyTypeId = lifePolicyType.Id,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsDeleted = false
            },
            // Property
            new ClaimType
            {
                Id = Guid.NewGuid(),
                TypeName = "Fire",
                IsMaturityClaim = false,
                IsFixedBenefit = false,
                PolicyTypeId = propertyPolicyType.Id,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsDeleted = false
            },
            new ClaimType
            {
                Id = Guid.NewGuid(),
                TypeName = "Flood",
                IsMaturityClaim = false,
                IsFixedBenefit = false,
                PolicyTypeId = propertyPolicyType.Id,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsDeleted = false
            }
        };

        await context.ClaimTypes.AddRangeAsync(claimTypes);

        // --- d) PolicyBenefitRules ---
        var healthHospitalization = claimTypes.First(ct => ct.TypeName == "Hospitalization" && ct.PolicyTypeId == healthPolicyType.Id);
        var autoAccident = claimTypes.First(ct => ct.TypeName == "Accident" && ct.PolicyTypeId == autoPolicyType.Id);
        var lifeDeath = claimTypes.First(ct => ct.TypeName == "Death" && ct.PolicyTypeId == lifePolicyType.Id);
        var propertyFire = claimTypes.First(ct => ct.TypeName == "Fire" && ct.PolicyTypeId == propertyPolicyType.Id);

        var benefitRules = new List<PolicyBenefitRule>
        {
            // Health Hospitalization
            new PolicyBenefitRule
            {
                Id = Guid.NewGuid(),
                PolicyTypeId = healthPolicyType.Id,
                ClaimTypeId = healthHospitalization.Id,
                CoPayPercent = 10,
                MaxClaimablePercent = 80,
                SubLimitAmount = 100000,
                SubLimitDescription = "Room rent sub-limit",
                DeductibleAmount = 1000,
                WaitingPeriodDays = 30,
                IntimationDeadlineDays = 30,
                RequiresPoliceReport = false,
                RequiresMedicalCertificate = true,
                RequiresDeathCertificate = false,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsDeleted = false
            },
            // Auto Accident
            new PolicyBenefitRule
            {
                Id = Guid.NewGuid(),
                PolicyTypeId = autoPolicyType.Id,
                ClaimTypeId = autoAccident.Id,
                CoPayPercent = 0,
                MaxClaimablePercent = 100,
                SubLimitAmount = 0,
                SubLimitDescription = string.Empty,
                DeductibleAmount = 2500,
                WaitingPeriodDays = 0,
                IntimationDeadlineDays = 7,
                RequiresPoliceReport = true,
                RequiresMedicalCertificate = false,
                RequiresDeathCertificate = false,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsDeleted = false
            },
            // Life Death
            new PolicyBenefitRule
            {
                Id = Guid.NewGuid(),
                PolicyTypeId = lifePolicyType.Id,
                ClaimTypeId = lifeDeath.Id,
                CoPayPercent = 0,
                MaxClaimablePercent = 100,
                SubLimitAmount = 0,
                SubLimitDescription = string.Empty,
                DeductibleAmount = 0,
                WaitingPeriodDays = 365,
                IntimationDeadlineDays = 30,
                RequiresPoliceReport = false,
                RequiresMedicalCertificate = false,
                RequiresDeathCertificate = true,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsDeleted = false
            },
            // Property Fire
            new PolicyBenefitRule
            {
                Id = Guid.NewGuid(),
                PolicyTypeId = propertyPolicyType.Id,
                ClaimTypeId = propertyFire.Id,
                CoPayPercent = 5,
                MaxClaimablePercent = 90,
                SubLimitAmount = 0,
                SubLimitDescription = string.Empty,
                DeductibleAmount = 5000,
                WaitingPeriodDays = 15,
                IntimationDeadlineDays = 14,
                RequiresPoliceReport = true,
                RequiresMedicalCertificate = false,
                RequiresDeathCertificate = false,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsDeleted = false
            }
        };

        await context.PolicyBenefitRules.AddRangeAsync(benefitRules);

        await context.SaveChangesAsync();
    }
}